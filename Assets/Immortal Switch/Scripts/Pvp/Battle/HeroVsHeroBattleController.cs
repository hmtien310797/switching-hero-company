using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Hero vs Hero 2v2 battle controller (DOCX §12–16, §34, §35, §38). Phase-1: deterministic SEEDED
    /// SIMULATION từ BattleSnapshot FinalStats (không spawn HeroActor — real-time engine cần PvP
    /// scene/prefab, FLAGGED). Honors: §12 front-first targeting, §13 mock AI (basic/class/ult),
    /// §14 seeded RNG (no UnityEngine.Random), §15 completion/timeout/double-KO/tie-break,
    /// §16 cleanup (state thuần local, GC-able), §18 data-driven buff runtime (M6), §34/§35 result +
    /// per-hero summaries + CombatLogHash, §36 local verifier mirror. Combat RNG tách gacha RNG.
    /// </summary>
    public sealed class HeroVsHeroBattleController
    {
        private readonly IPvPBattleResultService _resultService;
        private readonly IFormationBuffCatalogService _catalog;

        public HeroVsHeroBattleController(IPvPBattleResultService resultService, IFormationBuffCatalogService catalog)
        {
            _resultService = resultService;
            _catalog = catalog;
        }

        public async UniTask<PvPBattleResultRequest> RunAsync(HeroVsHeroBattleSnapshot snapshot, CancellationToken token)
        {
            if (snapshot == null)
                throw new InvalidOperationException("Cannot run battle — snapshot is null.");

            var request = Simulate(snapshot);
            await _resultService.SubmitResultAsync(request, token);
            return request;
        }

        /// <summary>Surrender: rời sau BattleId → Defeat, không hoàn ticket (DOCX §2/§15).</summary>
        public async UniTask<PvPBattleResultRequest> SurrenderAsync(string battleId, CancellationToken token)
        {
            var request = new PvPBattleResultRequest
            {
                BattleId = battleId,
                Result = PvPBattleResult.Surrender,
                DurationMs = 0,
                CombatLogHash = PvpCombatLogHasher.Hash("surrender:" + battleId),
                BattleRulesVersion = 1,
                SnapshotVersion = 1,
                AttackerHeroes = Array.Empty<HeroBattleResultSummary>(),
                DefenderHeroes = Array.Empty<HeroBattleResultSummary>()
            };
            await _resultService.SubmitResultAsync(request, token);
            return request;
        }

        /// <summary>Pure deterministic simulation (no side effects). Produce PvPBattleResultRequest.</summary>
        public PvPBattleResultRequest Simulate(HeroVsHeroBattleSnapshot snapshot)
        {
            var rng = new SeededPvPBattleRandomService(snapshot.RandomSeed);
            var log = new StringBuilder();

            var attacker = BuildTeam(snapshot.Attacker);
            var defender = BuildTeam(snapshot.Defender);
            var teams = new[] { attacker, defender };

            // §18 BattleStarted passive triggers (shield/heal/stat at battle start).
            FormationBuffRuntime.OnBattleStart(attacker, defender, _catalog, rng, log);
            FormationBuffRuntime.OnBattleStart(defender, attacker, _catalog, rng, log);
            // Heroes full HP after battle-start effects (MaxHp có thể thay do stat buffs).
            foreach (var team in teams) foreach (var h in team) h.CurrentHp = Mathf.Min(h.MaxHp, h.CurrentHp > 0 ? h.CurrentHp : h.MaxHp);

            const float tickSeconds = PvpDefaults.TickDurationSeconds;
            PvPBattleResult result = PvPBattleResult.None;
            int tick;

            for (tick = 0; tick < PvpDefaults.MaxBattleDurationTicks; tick++)
            {
                for (int teamIndex = 0; teamIndex < 2; teamIndex++)
                {
                    var team = teams[teamIndex];
                    var enemy = teams[1 - teamIndex];
                    for (int i = 0; i < team.Count; i++)
                    {
                        var hero = team[i];
                        if (hero.IsDead) continue;

                        hero.ActionCharge += hero.AttackSpeed * tickSeconds;
                        if (hero.ActionCharge < 1f) continue;
                        hero.ActionCharge -= 1f;

                        var target = SelectTarget(enemy);   // §12 front-first
                        if (target == null) continue;
                        Act(hero, target, team, enemy, rng, log);   // §13 AI + §14 RNG + §18 buff triggers
                    }
                }

                bool atkDead = TeamAllDead(attacker);
                bool defDead = TeamAllDead(defender);
                if (atkDead && defDead) { result = ResolveTieBreak(attacker, defender, log); break; } // double-KO
                if (defDead) { result = PvPBattleResult.Victory; break; }
                if (atkDead) { result = PvPBattleResult.Defeat; break; }
            }

            if (result == PvPBattleResult.None)
                result = ResolveTieBreak(attacker, defender, log);   // timeout tie-break (§15)

            long durationMs = (long)(Math.Max(1, tick) * tickSeconds * 1000f);

            return new PvPBattleResultRequest
            {
                BattleId = snapshot.BattleId,
                Result = result,
                DurationMs = durationMs,
                AttackerHeroes = BuildSummaries(attacker),
                DefenderHeroes = BuildSummaries(defender),
                TotalDamageDealt = SumDamage(attacker) + SumDamage(defender),
                TotalHealingDone = SumHealing(attacker) + SumHealing(defender),
                CombatLogHash = PvpCombatLogHasher.Hash(log.ToString()),
                SnapshotVersion = snapshot.SnapshotVersion,
                BattleRulesVersion = snapshot.BattleRulesVersion
            };
        }

        private List<PvpSimHero> BuildTeam(TeamBattleSnapshot team)
        {
            var list = new List<PvpSimHero>(2);
            if (team == null) return list;
            AddHero(list, team.FrontHero, FormationSlot.Front);
            AddHero(list, team.BackHero, FormationSlot.Back);
            return list;
        }

        private void AddHero(List<PvpSimHero> list, HeroBattleSnapshot snap, FormationSlot slot)
        {
            if (snap == null) return;
            var fs = snap.FinalStats;
            var hero = new PvpSimHero
            {
                HeroId = snap.HeroId,
                Slot = slot,
                MaxHp = fs?.Get(StatType.MaxHp) ?? 1f,
                Atk = fs?.Get(StatType.Atk) ?? 0f,
                Def = fs?.Get(StatType.Def) ?? 0f,
                CritChance = fs?.Get(StatType.CritChance) ?? 0f,
                CritDamage = fs?.Get(StatType.CritDamage) ?? 1f,
                AttackSpeed = Mathf.Max(0.1f, fs?.Get(StatType.AttackSpeed) ?? 1f),
                FormationBuffs = snap.FormationBuffs ?? new List<FormationBuffSnapshot>()
            };
            ApplyBuffStatModifiers(hero);   // passive stat modifiers (§19) — M6 runtime trigger effects apply during sim.
            hero.CurrentHp = hero.MaxHp;
            list.Add(hero);
        }

        // §19 passive stat-modifier buffs (StatEffects). Triggered effects handled by FormationBuffRuntime.
        private void ApplyBuffStatModifiers(PvpSimHero hero)
        {
            if (hero.FormationBuffs == null || _catalog == null) return;
            foreach (var b in hero.FormationBuffs)
            {
                if (b == null || !_catalog.TryGet(b.BuffId, out var def) || def.StatEffects == null) continue;
                foreach (var eff in def.StatEffects)
                {
                    if (eff == null) continue;
                    float v = eff.GetValue(b.Level);
                    if (eff.Operation == ModifierOp.Add) AddToStat(hero, eff.StatType, v);
                    else MulStat(hero, eff.StatType, 1f + v);
                }
            }
        }

        private static void AddToStat(PvpSimHero h, StatType t, float v)
        {
            switch (t)
            {
                case StatType.MaxHp: h.MaxHp += v; break;
                case StatType.Atk: h.Atk += v; break;
                case StatType.Def: h.Def += v; break;
                case StatType.CritChance: h.CritChance += v; break;
                case StatType.CritDamage: h.CritDamage += v; break;
                case StatType.AttackSpeed: h.AttackSpeed += v; break;
            }
        }

        private static void MulStat(PvpSimHero h, StatType t, float m)
        {
            switch (t)
            {
                case StatType.MaxHp: h.MaxHp *= m; break;
                case StatType.Atk: h.Atk *= m; break;
                case StatType.Def: h.Def *= m; break;
                case StatType.CritChance: h.CritChance *= m; break;
                case StatType.CritDamage: h.CritDamage *= m; break;
                case StatType.AttackSpeed: h.AttackSpeed *= m; break;
            }
        }

        // §12 front-first targeting; Back remains Back when Front dead (§8).
        private static PvpSimHero SelectTarget(List<PvpSimHero> enemy)
        {
            if (enemy == null) return null;
            for (int i = 0; i < enemy.Count; i++)
                if (enemy[i].Slot == FormationSlot.Front && !enemy[i].IsDead) return enemy[i];
            for (int i = 0; i < enemy.Count; i++)
                if (!enemy[i].IsDead) return enemy[i];
            return null;
        }

        // §13 mock AI: ult when ready, else class skill, else basic. §14 seeded crit. §18 shield + triggers.
        private void Act(PvpSimHero hero, PvpSimHero target, List<PvpSimHero> heroTeam, List<PvpSimHero> enemyTeam,
            IPvPBattleRandomService rng, StringBuilder log)
        {
            string action;
            float skillCoef;

            if (hero.UltimateCd <= 0) { action = "ult"; skillCoef = 200f; hero.UltimateCd = 8f; hero.UltimateCount++; }
            else if (hero.ClassSkillCd <= 0) { action = "skill"; skillCoef = 80f; hero.ClassSkillCd = 4f; hero.ClassSkillCount++; }
            else { action = "basic"; skillCoef = 0f; hero.BasicAttackCount++; }

            hero.ClassSkillCd -= 1f;
            hero.UltimateCd -= 1f;
            FormationBuffRuntime.TickCooldowns(hero);   // §18 internal cooldown tick per action.

            bool crit = rng.Roll(hero.CritChance);   // §14 — seeded RNG, không UnityEngine.Random
            float critMul = crit ? hero.CritDamage : 1f;
            float defMul = 100f / (100f + target.Def);
            float dmg = Mathf.Max(1f, hero.Atk * (1f + skillCoef / 100f) * defMul * critMul);

            // §18 Shield absorption.
            if (target.Shield > 0f)
            {
                float absorbed = Mathf.Min(target.Shield, dmg);
                target.Shield -= absorbed;
                dmg -= absorbed;
            }
            target.CurrentHp -= dmg;
            target.DamageReceived += (long)dmg;
            hero.DamageDealt += (long)dmg;
            if (target.CurrentHp <= 0f) { target.CurrentHp = 0f; target.IsDead = true; }

            log.Append(hero.HeroId).Append(':').Append(action).Append("->").Append(target.HeroId)
               .Append(' ').Append((long)dmg).Append(crit ? "crit" : "").Append(';');

            // §18 buff triggers after the attack.
            FormationBuffRuntime.OnBasicAttackHit(hero, target, enemyTeam, _catalog, rng, log);
            FormationBuffRuntime.OnHpBelowThreshold(target, heroTeam, _catalog, rng, log);   // target's enemy = attacker team
        }

        private static bool TeamAllDead(List<PvpSimHero> team)
        {
            if (team == null || team.Count == 0) return true;
            for (int i = 0; i < team.Count; i++) if (!team[i].IsDead) return false;
            return true;
        }

        // §15 tie-break: HP% → abs HP → damage dealt. Same-tick double-KO dùng cùng sequence.
        private static PvPBattleResult ResolveTieBreak(List<PvpSimHero> attacker, List<PvpSimHero> defender, StringBuilder log)
        {
            float atkHpPct = TeamHpPercent(attacker);
            float defHpPct = TeamHpPercent(defender);
            if (atkHpPct > defHpPct) return PvPBattleResult.Victory;
            if (atkHpPct < defHpPct) return PvPBattleResult.Defeat;

            float atkHp = TeamHp(attacker), defHp = TeamHp(defender);
            if (atkHp > defHp) return PvPBattleResult.Victory;
            if (atkHp < defHp) return PvPBattleResult.Defeat;

            long atkDmg = SumDamage(attacker), defDmg = SumDamage(defender);
            if (atkDmg > defDmg) return PvPBattleResult.Victory;
            if (atkDmg < defDmg) return PvPBattleResult.Defeat;

            log.Append("draw;");
            return PvPBattleResult.Draw;
        }

        private static float TeamHpPercent(List<PvpSimHero> team)
        {
            float hp = 0, max = 0;
            if (team != null) for (int i = 0; i < team.Count; i++) { hp += team[i].CurrentHp; max += team[i].MaxHp; }
            return max > 0 ? hp / max : 0f;
        }

        private static float TeamHp(List<PvpSimHero> team)
        {
            float hp = 0; if (team != null) for (int i = 0; i < team.Count; i++) hp += team[i].CurrentHp; return hp;
        }

        private static long SumDamage(List<PvpSimHero> team)
        {
            long d = 0; if (team != null) for (int i = 0; i < team.Count; i++) d += team[i].DamageDealt; return d;
        }

        private static long SumHealing(List<PvpSimHero> team)
        {
            long d = 0; if (team != null) for (int i = 0; i < team.Count; i++) d += team[i].HealingDone; return d;
        }

        private static HeroBattleResultSummary[] BuildSummaries(List<PvpSimHero> team)
        {
            if (team == null || team.Count == 0) return Array.Empty<HeroBattleResultSummary>();
            var arr = new HeroBattleResultSummary[team.Count];
            for (int i = 0; i < team.Count; i++)
            {
                var h = team[i];
                arr[i] = new HeroBattleResultSummary
                {
                    HeroId = h.HeroId,
                    AssignedSlot = h.Slot,
                    FinalHp = (long)h.CurrentHp,
                    IsDead = h.IsDead,
                    DamageDealt = h.DamageDealt,
                    DamageReceived = h.DamageReceived,
                    HealingDone = h.HealingDone,
                    ShieldGenerated = h.ShieldGenerated,
                    BasicAttackCount = h.BasicAttackCount,
                    ClassSkillCastCount = h.ClassSkillCount,
                    UltimateCastCount = h.UltimateCount,
                    BuffTriggerCounts = h.BuffTriggerCounts
                };
            }
            return arr;
        }
    }
}
