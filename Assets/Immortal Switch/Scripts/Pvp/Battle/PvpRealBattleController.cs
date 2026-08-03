using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.Pvp.Views;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Real-time 2v2 PvP battle controller (DOCX §12-16). Spawn 4 <see cref="HeroActor"/> từ
    /// <see cref="HeroVsHeroBattleSnapshot"/>, reuse live <c>SkillExecutor</c> + auto-skill AI
    /// (HeroAutoSkillController) + per-side targeting (<see cref="PvpBattleContext"/>/IAllyProvider)
    /// + <c>DamageCalculator</c>. Win/lose khi 1 team all-dead (§15). Cleanup despawn + clear.
    /// <para><b>FLAGGED:</b> real-time dùng UnityEngine.Random cho crit/chance (live engine) —
    /// deterministic seeded combat = <see cref="HeroVsHeroBattleController.Simulate"/> (M5, cho
    /// Trust-but-Verify/replay). Per-hero DamageDealt/counts = 0 (cần instrument HeroActor; M5 sim
    /// có đầy đủ cho verification). Basic-attack/skill crit không inject seeded RNG (deferred).</para>
    /// </summary>
    public class PvpRealBattleController : Singleton<PvpRealBattleController>
    {
        [SerializeField] private bool enableLog = true;
        [SerializeField] private Vector3 attackerSpawnCenter = new Vector3(0f, 0f, 5f);
        [SerializeField] private Vector3 defenderSpawnCenter = new Vector3(0f, 0f, -5f);
        [SerializeField] private float heroSpacing = 1.5f;

        private PvpBattleTeam _attacker;
        private PvpBattleTeam _defender;
        private HeroVsHeroBattleSnapshot _snapshot;
        private SeededPvPBattleRandomService _rng;
        private Dictionary<HeroActor, PvpBattleTeam> _heroTeam;
        private CancellationTokenSource _cts;
        private float _battleStartReal;
        private bool _running;
        private bool _ended;
        private bool _openUi = true;
        private float _debugLogTimer;

        public bool IsRunning => _running && !_ended;

        /// <summary>Fire khi battle kết thúc (request đã build). No-UI flow quan sát qua event này
        /// thay vì Result view. Truyền request cho caller (rank/reward đã submit trong EndBattle).</summary>
        public event Action<PvPBattleResultRequest> OnBattleEnded;

        public override UniTask InitializeAsync() => UniTask.CompletedTask;

        // ── Public API ─────────────────────────────────────────────────────────────

        public async UniTask RunAsync(HeroVsHeroBattleSnapshot snapshot, CancellationToken token, bool openUi = true)
        {
            if (snapshot == null) throw new InvalidOperationException("Cannot run battle — snapshot null.");
            if (_running) { Debug.LogWarning("[PvP] Real battle already running."); return; }
            _openUi = openUi;

            _snapshot = snapshot;
            _rng = new SeededPvPBattleRandomService(snapshot.RandomSeed);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _battleStartReal = Time.realtimeSinceStartup;
            _ended = false;
            _heroTeam = new Dictionary<HeroActor, PvpBattleTeam>();

            _attacker = new PvpBattleTeam();
            _defender = new PvpBattleTeam();
            // Mỗi team dùng registry CỦA CHÍNH NÓ làm TargetRegistry. Sau cross-register bên dưới:
            //   _attacker.Registry ← defender heroes (= enemies của attacker)
            //   _defender.Registry ← attacker heroes  (= enemies của defender)
            // (Trước đây trỏ chéo sang registry đối phương → mỗi team target chính đồng đội → không đánh nhau.)
            _attacker.Context = new PvpBattleContext(_attacker.Heroes, _attacker.Registry, _rng);
            _defender.Context = new PvpBattleContext(_defender.Heroes, _defender.Registry, _rng);

            try
            {
                // Clear PvE chapter stage (creep/boss + live hero). Snapshot đã build từ live hero stats
                // trong matchmaking (trước RunAsync), nên despawn live hero bây giờ không mất dữ liệu attacker.
                PvpStageTransition.ClearPveStage();

                await SpawnTeamAsync(_attacker, snapshot.Attacker, true);
                await SpawnTeamAsync(_defender, snapshot.Defender, false);

                // Cross-register hostiles: A's heroes are hostiles to B, and vice versa.
                RegisterHostiles(_attacker, _defender);
                RegisterHostiles(_defender, _attacker);

                // Heroes spawned + registered → battle now running. (Trước đây set _running=true quá
                // sớm ở đầu RunAsync → Update check AllDead khi team rỗng (count==0 → true) → EndBattle
                // ngay TRƯỚC khi hero spawn xong → battle kết thúc sớm, hero đứng im.)
                _running = true;

                if (enableLog)
                {
                    Debug.Log($"[PvP] Real battle started: {snapshot.BattleId} (seed {snapshot.RandomSeed}).");
                    Debug.Log($"[PvP] Post-spawn: attacker.Registry.count={_attacker.Registry.HostileTargets.Count} defender.Registry.count={_defender.Registry.HostileTargets.Count}");
                    LogTeamStates(_attacker, "A");
                    LogTeamStates(_defender, "D");
                }

                // Open Battle HUD (screen 07) nếu openUi. No-UI flow: bỏ qua HUD, quan sát qua OnBattleEnded.
                if (_openUi)
                    UIManager.Instance?.OpenPopupAsync<PvpBattleHudView>(snapshot).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvP] Real battle setup failed: {e}");
                Cleanup();
                _running = false;
                _ended = true;
                throw;
            }
        }

        /// <summary>Force-end now with current alive/dead state (HUD RESOLVE).</summary>
        public void ForceResolve()
        {
            if (!_running || _ended) return;
            bool aDead = _attacker != null && _attacker.AllDead;
            bool bDead = _defender != null && _defender.AllDead;
            var outcome = (aDead && bDead) ? PvPBattleResult.Draw
                : (bDead ? PvPBattleResult.Victory
                : (aDead ? PvPBattleResult.Defeat : PvPBattleResult.Draw));
            EndBattle(outcome);
        }

        /// <summary>Surrender = Defeat, ticket không hoàn (DOCX §2/§15).</summary>
        public void Surrender()
        {
            if (!_running || _ended) return;
            EndBattle(PvPBattleResult.Surrender);
        }

        // ── Win/lose detection ───────────────────────────────────────────────────────

        private void Update()
        {
            if (!_running || _ended) return;
            if (_attacker == null || _defender == null) return;

            // [DEBUG] log state mỗi 1s để trace tại sao hero không đánh.
            _debugLogTimer += Time.deltaTime;
            if (_debugLogTimer >= 1f)
            {
                _debugLogTimer = 0f;
                LogTeamStates(_attacker, "A");
                LogTeamStates(_defender, "D");
            }

            bool aDead = _attacker.AllDead;
            bool bDead = _defender.AllDead;
            if (!aDead && !bDead) return;

            var outcome = (aDead && bDead) ? PvPBattleResult.Draw
                : (bDead ? PvPBattleResult.Victory : PvPBattleResult.Defeat);
            EndBattle(outcome);
        }

        private static void LogTeamStates(PvpBattleTeam team, string label)
        {
            if (team?.Actors == null) return;
            foreach (var h in team.Actors)
            {
                if (h == null) continue;
                Debug.Log($"[PvP] {label} hero {h.GetHeroId()}: state={h.StateMachine.CurrentStateId} hasTarget={h.CurrentTarget != null} locked={h.IsActionLocked} dead={h.IsDead} pos={h.Position}");
            }
        }

        private async void EndBattle(PvPBattleResult outcome)
        {
            if (_ended) return;
            _ended = true;
            _running = false;

            PvPBattleResultRequest request;
            try { request = BuildResult(outcome); }
            catch (Exception e)
            {
                Debug.LogError($"[PvP] BuildResult failed: {e}");
                request = new PvPBattleResultRequest { BattleId = _snapshot?.BattleId, Result = outcome };
            }

            Cleanup();

            try
            {
                var resultService = PvpManager.Instance?.Facade?.BattleResult;
                if (resultService != null)
                    await resultService.SubmitResultAsync(request, _cts?.Token ?? CancellationToken.None);
            }
            catch (Exception e) { Debug.LogError($"[PvP] SubmitResult failed: {e}"); }

            OnBattleEnded?.Invoke(request);
            if (_openUi)
            {
                UIManager.Instance?.Close<PvpBattleHudView>();
                UIManager.Instance.OpenPopupAsync<PvpBattleResultView>(request).Forget();
            }
            GameEventManager.Trigger(GameEvents.ON_PVP_BATTLE_END);
        }

        // ── Spawn ───────────────────────────────────────────────────────────────────

        private async UniTask SpawnTeamAsync(PvpBattleTeam team, TeamBattleSnapshot teamSnap, bool isAttacker)
        {
            if (teamSnap == null) return;
            Vector3 center = isAttacker ? attackerSpawnCenter : defenderSpawnCenter;
            Vector3 frontPos = center + Vector3.left * heroSpacing;
            Vector3 backPos = center + Vector3.right * heroSpacing;
            await SpawnHeroAsync(team, teamSnap.FrontHero, frontPos);
            await SpawnHeroAsync(team, teamSnap.BackHero, backPos);
        }

        private async UniTask SpawnHeroAsync(PvpBattleTeam team, HeroBattleSnapshot heroSnap, Vector3 pos)
        {
            if (heroSnap == null || heroSnap.HeroId <= 0) return;

            HeroDataSO data = DatabaseManager.Instance.GetHeroDataById(heroSnap.HeroId);
            if (data == null)
            {
                Debug.LogWarning($"[PvP] HeroDataSO not found for heroId={heroSnap.HeroId} — skipping.");
                return;
            }

            // Spawn Addressable hero prefab + Init (both teams AUTO, DOCX §07; teamController null —
            // MoveTowards null-safe after the shared edit).
            HeroActor hero = await AddressableSpawnService.SpawnAsync<HeroActor>(
                string.Empty, data.HeroAddressKey, pos, Quaternion.identity);

            if (hero == null)
            {
                Debug.LogWarning($"[PvP] Spawn failed for hero {heroSnap.HeroId}.");
                return;
            }

            hero.gameObject.SetActive(true);
            await hero.Init(data, team.Context, null, true, true);
            hero.OnDead += OnHeroDead;

            // Override base stats from snapshot FinalStats (bypass progression/equipment bridges —
            // bridges non-fatal for enemy ids, then overwritten here).
            ApplySnapshotStats(hero, heroSnap.FinalStats);

            // HeroActor.Init → Spawn state set IsActionLocked(true) (spawn anim 2s) nhưng Spawn.Exit
            // KHÔNG reset → MoveTowards (Run state) return sớm ở "if (IsDead || IsActionLocked || ...)"
            // → hero đứng im, không lao vào đánh. Unlock ở đây để hero hành động sau khi Spawn→Idle.
            hero.SetActionLocked(false);

            _heroTeam[hero] = team;
            team.AddHero(hero);
        }

        private static void ApplySnapshotStats(HeroActor hero, RuntimeStatSnapshot stats)
        {
            if (hero == null || hero.Stats == null) return;

            if (stats == null)
            {
                hero.HealthBarController?.ResetHealth();
                return;
            }

            var bs = new BaseStat
            {
                Health = stats.Get(StatType.MaxHp),
                Attack = stats.Get(StatType.Atk),
                Defense = stats.Get(StatType.Def),
                AttackRange = stats.Get(StatType.AttackRange),
                AttackSpeed = stats.Get(StatType.AttackSpeed),
                CritChance = stats.Get(StatType.CritChance),
                CritDamage = stats.Get(StatType.CritDamage),
                Accuracy = stats.Get(StatType.Accuracy),
                MoveSpeed = stats.Get(StatType.MoveSpeed)
            };
            hero.Stats.Initialize(bs);   // recreate modules; HP = MaxHp.

            // Extra stats not in BaseStat (Penetration, LifeSteal, ShieldPower, CooldownReduction, ...).
            var sm = hero.Stats.StatModule;
            if (stats.Values != null)
            {
                foreach (var kv in stats.Values)
                {
                    switch (kv.Key)
                    {
                        case StatType.MaxHp:
                        case StatType.Atk:
                        case StatType.Def:
                        case StatType.AttackRange:
                        case StatType.AttackSpeed:
                        case StatType.CritChance:
                        case StatType.CritDamage:
                        case StatType.Accuracy:
                        case StatType.MoveSpeed:
                            continue;
                        default:
                            sm.SetBaseStat(kv.Key, kv.Value);
                            break;
                    }
                }
            }

            // Ensure MoveSpeed > 0 để hero di chuyển được (MoveTowards fallback khi teamController null).
            // Hero prefab thường KHÔNG set MoveSpeed base stat (PvE dùng HeroTeamController.TeamMoveSpeed)
            // → snapshot MoveSpeed = 0 → hero đứng im, không tới tầm → không đánh.
            if (sm.GetFinalStat(StatType.MoveSpeed) < 5f)
                sm.SetBaseStat(StatType.MoveSpeed, 5f);

            hero.HealthBarController?.ResetHealth();
        }

        // ── Hostile registration + death ─────────────────────────────────────────────

        private static void RegisterHostiles(PvpBattleTeam from, PvpBattleTeam into)
        {
            if (from == null || into == null) return;
            for (int i = 0; i < from.Actors.Count; i++)
                into.Registry.RegisterHostile(from.Actors[i]);
        }

        private void OnHeroDead(HeroActor dead)
        {
            if (dead == null || _heroTeam == null) return;
            if (!_heroTeam.TryGetValue(dead, out var ownerTeam) || ownerTeam == null) return;
            var enemyTeam = ownerTeam == _attacker ? _defender : _attacker;
            enemyTeam?.Registry.UnregisterHostile(dead);
        }

        // ── Result + Cleanup (§16) ────────────────────────────────────────────────────

        private PvPBattleResultRequest BuildResult(PvPBattleResult outcome)
        {
            return new PvPBattleResultRequest
            {
                BattleId = _snapshot.BattleId,
                Result = outcome,
                DurationMs = (long)((Time.realtimeSinceStartup - _battleStartReal) * 1000f),
                AttackerHeroes = BuildSummaries(_attacker),
                DefenderHeroes = BuildSummaries(_defender),
                TotalDamageDealt = 0,   // FLAGGED: real-time counters chưa instrument (M5 sim có đầy đủ).
                TotalHealingDone = 0,
                CombatLogHash = PvpCombatLogHasher.Hash(_snapshot.BattleId + ":" + outcome),
                SnapshotVersion = _snapshot.SnapshotVersion,
                BattleRulesVersion = _snapshot.BattleRulesVersion
            };
        }

        private static HeroBattleResultSummary[] BuildSummaries(PvpBattleTeam team)
        {
            if (team == null || team.Actors.Count == 0) return Array.Empty<HeroBattleResultSummary>();
            var arr = new HeroBattleResultSummary[team.Actors.Count];
            for (int i = 0; i < team.Actors.Count; i++)
            {
                var h = team.Actors[i];
                arr[i] = new HeroBattleResultSummary
                {
                    HeroId = h != null ? h.GetHeroId() : 0,
                    AssignedSlot = (FormationSlot)i,   // 0 = Front, 1 = Back (spawn order)
                    FinalHp = h != null ? (long)Math.Max(0, h.CurrentHp) : 0,
                    IsDead = h == null || h.IsDead
                };
            }
            return arr;
        }

        private void Cleanup()
        {
            DespawnTeam(_attacker);
            DespawnTeam(_defender);
            _attacker?.Clear();
            _defender?.Clear();
            _heroTeam?.Clear();
            try { _cts?.Cancel(); _cts?.Dispose(); } catch { }
            _cts = null;
        }

        private void DespawnTeam(PvpBattleTeam team)
        {
            if (team == null) return;
            for (int i = 0; i < team.Actors.Count; i++)
            {
                var h = team.Actors[i];
                if (h == null) continue;
                try { h.OnDead -= OnHeroDead; } catch { }
                try { AddressableSpawnService.ReleaseInstance(h); } catch { }
            }
        }
    }
}
