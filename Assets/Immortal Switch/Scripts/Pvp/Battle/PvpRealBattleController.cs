using System;
using System.Collections.Generic;
using System.Threading;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Pvp;
using Immortal_Switch.Scripts.Pvp.DevTools;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.Pvp.Views;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using NUnit.Framework;
using Unity.Cinemachine;
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
        private bool _ending;
        private float _endDelayTimer;
        private PvPBattleResult _pendingOutcome;
        [SerializeField] private float endBattleDelay = 2f;

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
            _ending = false;
            _endDelayTimer = 0f;
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
                await Transitioner.Instance.TransitionOutWithoutChangingScene(token);
                PvpStageTransition.ClearPveStage();
                GameCameraController.Instance.ResetCamera();
                Transitioner.Instance.TransitionInWithoutChangingScene();
                await UniTask.Delay(1000, cancellationToken: token);
                await SpawnTeamAsync(_attacker, snapshot.Attacker, true);
                if (_attacker.Actors.Count >= 2)
                {
                    UserDataCache.Instance.SetBattleLineup(new[]
                        { _attacker.Actors[0].HeroData.Id, _attacker.Actors[1].HeroData.Id });
                    UserDataCache.Instance.TrySetInBattleHeroActor(0, _attacker.Actors[0]);
                    UserDataCache.Instance.TrySetInBattleHeroActor(1, _attacker.Actors[1]);
                    HeroTeamController.Instance?.SetHeroes(_attacker.Actors[0], _attacker.Actors[1]);
                    BattleHeroSessionController.Instance.SelectControlledHeroSlotForPvp();
                    TopMainView.Instance.ResetHeroIconPositions();
                    GameEventManager.Trigger(GameEvents.OnActiveLineupChanged);
                    foreach (var h in _attacker.Actors)
                    {
                        h?.SetAutoClassSkill(UserDataCache.Instance != null && UserDataCache.Instance.AutoClassSkill);
                        h?.SetAutoUltimateSkill(UserDataCache.Instance != null && UserDataCache.Instance.AutoUltimateSkill);
                    }
                }
                await SpawnTeamAsync(_defender, snapshot.Defender, false);
                List<CinemachineTargetGroup.Target> targets = new List<CinemachineTargetGroup.Target>();
                for (int i = 0; i < _attacker.Actors.Count; i++)
                {
                    targets.Add(new CinemachineTargetGroup.Target{Object = _attacker.Actors[i].transform});
                }
                
                for (int i = 0; i < _defender.Actors.Count; i++)
                {
                    targets.Add(new CinemachineTargetGroup.Target{Object = _defender.Actors[i].transform});
                }
                
                await UniTask.Delay(200, cancellationToken: token);
                GameCameraController.Instance.SetFollowPvpHero(targets);
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
                {
                    //UIManager.Instance?.OpenPopupAsync<PvpBattleHudView>(snapshot).Forget();
                }
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

            if (_ending)
            {
                _endDelayTimer -= Time.deltaTime;
                if (_endDelayTimer <= 0f)
                    EndBattle(_pendingOutcome);
                return;
            }

            bool aDead = _attacker.AllDead;
            bool bDead = _defender.AllDead;
            if (!aDead && !bDead) return;

            _pendingOutcome = (aDead && bDead) ? PvPBattleResult.Draw
                : (bDead ? PvPBattleResult.Victory : PvPBattleResult.Defeat);
            _ending = true;
            _endDelayTimer = endBattleDelay;
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
            _ending = false;

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
                // UIManager.Instance?.Close<PvpBattleHudView>();
                // UIManager.Instance.OpenPopupAsync<PvpBattleResultView>(request).Forget();
            }
            GameEventManager.Trigger(GameEvents.ON_PVP_BATTLE_END);
            BattleFlowController.Instance.PlayNormalChapter().Forget();
        }

        // ── Spawn ───────────────────────────────────────────────────────────────────

        private async UniTask SpawnTeamAsync(PvpBattleTeam team, TeamBattleSnapshot teamSnap, bool isAttacker)
        {
            if (teamSnap == null) return;
            Vector3 center = isAttacker ? attackerSpawnCenter : defenderSpawnCenter;
            Vector3 frontPos = center + Vector3.left * heroSpacing;
            Vector3 backPos = center + Vector3.right * heroSpacing;
            HeroTeamController teamCtrl = isAttacker ? HeroTeamController.Instance : null;
            await SpawnHeroAsync(team, teamSnap.FrontHero, frontPos, teamCtrl);
            await SpawnHeroAsync(team, teamSnap.BackHero, backPos, teamCtrl);
        }

        private async UniTask SpawnHeroAsync(PvpBattleTeam team, HeroBattleSnapshot heroSnap, Vector3 pos, HeroTeamController teamCtrl)
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
            await hero.Init(data, team.Context, teamCtrl, true, true);
            hero.OnDead += OnHeroDead;

            // Override base stats from snapshot FinalStats (bypass progression/equipment bridges —
            // bridges non-fatal for enemy ids, then overwritten here).
            if (teamCtrl == null)
            {
                // Defender (AI, fake data) - base = progression node; weapon = modifier riêng có sourceId
                // (để StatsController liệt kê nguồn như attacker). Bỏ qua growth/transmutation/powerup.
                ApplySnapshotBaseAndWeapon(hero, heroSnap);

                // Áp skill loadout từ config + prewarm runtime assets (skill object/đạn) cho defender
                // VÀO TRƯỚC khi trận bắt đầu (Init đã prewarm skill của player, không phải skill config).
                await ApplySnapshotSkills(hero, heroSnap.Skills);
                hero.BindDeathEvent();
            }
            // Attacker (player) - KEEP Init stats (PowerUp + progression + equipment + growth +
            // transmutation bridges applied in ResetData). Do NOT call ApplySnapshotStats (Initialize
            // would wipe bridges + lose mechanics: equipment procs, growth passives, transmutation).

            hero.SetActionLocked(false);

            _heroTeam[hero] = team;
            team.AddHero(hero);
        }

        /// <summary>
        /// Áp stat cho defender: base = progression node + HeroDataSO (không weapon), weapon = StatModifier
        /// riêng có sourceId (WeaponRuntimeIds) — để StatsController liệt kê nguồn giống attacker. Bỏ qua
        /// growth/transmutation/powerup (config test chỉ có tier/star/skill/equipment).
        /// </summary>
        private static void ApplySnapshotBaseAndWeapon(HeroActor hero, HeroBattleSnapshot heroSnap)
        {
            if (hero?.Stats == null || heroSnap == null) return;

            var heroData = DatabaseManager.Instance?.GetHeroDataById(heroSnap.HeroId);
            if (heroData == null)
            {
                hero.HealthBarController?.ResetHealth();
                return;
            }

            // Base stats (progression node + HeroDataSO), không weapon.
            var baseStats = DefenderTestStatsBuilder.BuildBaseStats(
                new DefenderSlotConfig { HeroId = heroSnap.HeroId, Tier = heroSnap.Tier, Star = heroSnap.Star },
                heroData);

            var bs = new BaseStat
            {
                Health = baseStats.Get(StatType.MaxHp),
                Attack = baseStats.Get(StatType.Atk),
                Defense = baseStats.Get(StatType.Def),
                AttackRange = baseStats.Get(StatType.AttackRange),
                AttackSpeed = baseStats.Get(StatType.AttackSpeed),
                CritChance = baseStats.Get(StatType.CritChance),
                CritDamage = baseStats.Get(StatType.CritDamage),
                Accuracy = baseStats.Get(StatType.Accuracy),
                MoveSpeed = 0f
            };
            hero.Stats.Initialize(bs);   // recreate modules; HP = MaxHp.

            // Weapon modifiers riêng (có sourceId) → hiện breakdown nguồn như attacker.
            var sm = hero.Stats.StatModule;
            if (heroSnap.Equipment != null)
            {
                var mods = DefenderTestStatsBuilder.BuildWeaponModifiers(heroSnap.HeroId, heroSnap.Equipment);
                for (int i = 0; i < mods.Count; i++)
                    sm.AddModifier(mods[i]);
            }

            // Ensure MoveSpeed > 0 để hero di chuyển được (MoveTowards fallback khi teamController null).
            if (sm.GetFinalStat(StatType.MoveSpeed) < 5f)
                sm.SetBaseStat(StatType.MoveSpeed, 5f);

            hero.HealthBarController?.ResetHealth();

            // [DEBUG] stat thực tế sau apply (so với FinalStats config).
            Debug.Log($"[PvP][DefenderTest] applied hero={hero.GetHeroId()} " +
                $"Atk={sm.GetFinalStat(StatType.Atk):0} " +
                $"MaxHp={sm.GetFinalStat(StatType.MaxHp):0} " +
                $"Def={sm.GetFinalStat(StatType.Def):0}");
        }

        /// <summary>
        /// Áp loadout skill cho defender từ snapshot (giả lập một user khác). Resolve SkillId → SkillDataSO,
        /// chỉ lấy class skill (ultimate/passive hero-bound không đổi), rồi set + inject level provider
        /// trả level đã config (clamp MaxLevel). Sau khi set, prewarm runtime assets (skill object/đạn các
        /// class skill) để chúng được pool trước khi trận bắt đầu. Nếu snapshot không mang skill thì giữ
        /// nguyên mặc định (không prewarm lại).
        /// </summary>
        private static async UniTask ApplySnapshotSkills(HeroActor hero, SkillProgressionSnapshot skills)
        {
            if (hero?.HeroSkillController == null || skills?.Equipped == null) return;
            if (DatabaseManager.Instance == null) return;

            var resolved = new List<SkillDataSO>();
            var seen = new HashSet<int>();
            for (int i = 0; i < skills.Equipped.Count && resolved.Count < HeroSkillController.ClassSkillSlotCount; i++)
            {
                var slot = skills.Equipped[i];
                if (slot == null || slot.SkillId <= 0) continue;
                if (!seen.Add(slot.SkillId)) continue;

                var data = DatabaseManager.Instance.GetSkillDataById(slot.SkillId);
                if (data == null || data.OwnerType != SkillOwnerType.ClassSkill) continue;
                resolved.Add(data);
            }

            if (resolved.Count == 0) return;
            hero.HeroSkillController.SetClassSkills(resolved);
            hero.HeroSkillController.SetSkillLevelProvider(new DefenderSkillLevelProvider(skills));

            // Prewarm skill object + đạn của các class skill vừa config (idempotent) để không bị
            // spawn-on-demand khi bước vào trận.
            await hero.HeroSkillController.InitializeUltimateSkillDataAndClassSkillData();
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
                // Despawn skill object + đạn (ultimate + class skills) của hero — giống PvE
                // (BattleHeroSessionController.DespawnAllHeroes). Tránh pool skill rơi rớt sau trận.
                try { h.HeroSkillController?.DespawnAllInstanceOfUltimateSkillAndClassSkill(); } catch { }
                try { AddressableSpawnService.ReleaseInstance(h); } catch { }
            }
        }
    }
}

