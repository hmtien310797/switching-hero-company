using System.Collections.Generic;
using System.Text;
using Immortal_Switch.Scripts.Pvp.Data;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Data-driven Formation Buff runtime (DOCX §18, §38). Đánh giá trigger + apply effects trong
    /// simulation. Reuses StatType/ModifierOp (§19). Trigger: BattleStarted (apply at start),
    /// BasicAttackHit / AfterDamageReceived / HpBelowThreshold (per action). Effect: StatModifier /
    /// Damage / Heal / Shield. Internal cooldown / once-per-battle / max-count (§18 Runtime).
    /// <b>FLAGGED:</b> Phase-1 subset của triggers/effects; ApplyBuff/Cooldown/Stack/PreventDeath
    /// chưa full (M-future). Shield absorption cũng ở controller.Act (consistent).
    /// </summary>
    public static class FormationBuffRuntime
    {
        public static void OnBattleStart(List<PvpSimHero> team, List<PvpSimHero> enemy,
            IFormationBuffCatalogService catalog, IPvPBattleRandomService rng, StringBuilder log)
        {
            if (team == null) return;
            foreach (var h in team)
                ApplyTriggeredEffects(h, enemy, FormationBuffTriggerType.BattleStarted, catalog, rng, log);
        }

        public static void OnBasicAttackHit(PvpSimHero attacker, PvpSimHero target, List<PvpSimHero> attackerEnemyTeam,
            IFormationBuffCatalogService catalog, IPvPBattleRandomService rng, StringBuilder log)
        {
            if (attacker == null) return;
            ApplyTriggeredEffects(attacker, attackerEnemyTeam, FormationBuffTriggerType.BasicAttackHit,
                catalog, rng, log, explicitTarget: target);
            if (target != null)
                ApplyTriggeredEffects(target, attackerEnemyTeam, FormationBuffTriggerType.AfterDamageReceived,
                    catalog, rng, log);
        }

        public static void OnHpBelowThreshold(PvpSimHero hero, List<PvpSimHero> heroEnemyTeam,
            IFormationBuffCatalogService catalog, IPvPBattleRandomService rng, StringBuilder log)
        {
            if (hero == null || hero.IsDead || hero.MaxHp <= 0) return;
            ApplyTriggeredEffects(hero, heroEnemyTeam, FormationBuffTriggerType.HpBelowThreshold, catalog, rng, log);
        }

        public static void TickCooldowns(PvpSimHero hero)
        {
            if (hero?.BuffStates == null) return;
            foreach (var kv in hero.BuffStates)
                if (kv.Value.CooldownRemaining > 0f) kv.Value.CooldownRemaining -= 1f;
        }

        private static void ApplyTriggeredEffects(PvpSimHero hero, List<PvpSimHero> enemyTeam,
            FormationBuffTriggerType triggerType, IFormationBuffCatalogService catalog,
            IPvPBattleRandomService rng, StringBuilder log, PvpSimHero explicitTarget = null)
        {
            if (hero?.FormationBuffs == null || catalog == null) return;

            foreach (var b in hero.FormationBuffs)
            {
                if (b == null || !catalog.TryGet(b.BuffId, out var def)) continue;
                if (def.Trigger == null || def.Trigger.TriggerType != triggerType || def.Effects == null) continue;

                // HpBelowThreshold condition.
                if (triggerType == FormationBuffTriggerType.HpBelowThreshold && hero.MaxHp > 0)
                {
                    float pct = hero.CurrentHp / hero.MaxHp * 100f;
                    if (pct >= def.Trigger.HpThresholdPercent) continue;
                }

                if (!CanTrigger(hero, b.BuffId, def.Trigger, rng)) continue;

                foreach (var eff in def.Effects)
                    ApplyEffect(eff, hero, explicitTarget, enemyTeam, b.Level, log);

                MarkTriggered(hero, b.BuffId, def.Trigger);
            }
        }

        private static bool CanTrigger(PvpSimHero hero, string buffId, FormationBuffTriggerData trig, IPvPBattleRandomService rng)
        {
            if (trig == null) return true;
            var st = GetState(hero, buffId);
            if (trig.OncePerBattle && st.TriggeredOnceThisBattle) return false;
            if (trig.MaximumCount > 0 && st.TriggerCount >= trig.MaximumCount) return false;
            if (st.CooldownRemaining > 0f) return false;
            if (trig.ChancePercent < 100f && !rng.Roll(trig.ChancePercent / 100f)) return false;
            return true;
        }

        private static void MarkTriggered(PvpSimHero hero, string buffId, FormationBuffTriggerData trig)
        {
            var st = GetState(hero, buffId);
            st.TriggerCount++;
            st.CooldownRemaining = trig != null ? trig.InternalCooldown : 0f;
            st.TriggeredOnceThisBattle = true;

            int count = hero.BuffTriggerCounts.TryGetValue(buffId, out var c) ? c : 0;
            hero.BuffTriggerCounts[buffId] = count + 1;
        }

        private static BuffTriggerState GetState(PvpSimHero hero, string buffId)
        {
            if (!hero.BuffStates.TryGetValue(buffId, out var st))
            {
                st = new BuffTriggerState();
                hero.BuffStates[buffId] = st;
            }
            return st;
        }

        private static void ApplyEffect(FormationBuffEffectData eff, PvpSimHero owner, PvpSimHero explicitTarget,
            List<PvpSimHero> enemyTeam, int level, StringBuilder log)
        {
            if (eff == null) return;
            PvpSimHero target = ResolveTarget(eff.Target, owner, explicitTarget, enemyTeam);

            switch (eff.EffectType)
            {
                case FormationBuffEffectType.Shield:
                    float shield = eff.GetAmount(level);
                    if (owner != null) { owner.Shield += shield; owner.ShieldGenerated += (long)shield; }
                    break;
                case FormationBuffEffectType.Heal:
                    if (target != null && !target.IsDead)
                    {
                        float heal = eff.GetAmount(level);
                        target.CurrentHp = Mathf.Min(target.MaxHp, target.CurrentHp + heal);
                        target.HealingDone += (long)heal;
                    }
                    break;
                case FormationBuffEffectType.Damage:
                    if (target != null && !target.IsDead)
                    {
                        float dmg = Mathf.Max(1f, eff.GetAmount(level));
                        ApplyDamage(target, dmg, log, owner != null ? owner.HeroId : 0);
                    }
                    break;
                case FormationBuffEffectType.StatModifier:
                    if (owner != null) ApplyStatEffect(owner, eff, level);
                    break;
                // ApplyBuff/Cooldown/Stack/PreventDeath: FLAGGED — M-future.
            }
        }

        private static void ApplyStatEffect(PvpSimHero h, FormationBuffEffectData eff, int level)
        {
            float v = eff.GetStatValue(level);
            if (eff.Operation == ModifierOp.Add)
            {
                switch (eff.StatType)
                {
                    case StatType.MaxHp: h.MaxHp += v; break;
                    case StatType.Atk: h.Atk += v; break;
                    case StatType.Def: h.Def += v; break;
                }
            }
            else
            {
                switch (eff.StatType)
                {
                    case StatType.MaxHp: h.MaxHp *= 1f + v; break;
                    case StatType.Atk: h.Atk *= 1f + v; break;
                    case StatType.Def: h.Def *= 1f + v; break;
                }
            }
        }

        private static PvpSimHero ResolveTarget(FormationBuffTargetType t, PvpSimHero owner,
            PvpSimHero explicitTarget, List<PvpSimHero> enemyTeam)
        {
            switch (t)
            {
                case FormationBuffTargetType.Owner: return owner;
                case FormationBuffTargetType.EnemyFront:
                    return FindAliveSlot(enemyTeam, FormationSlot.Front) ?? FindAlive(enemyTeam);
                case FormationBuffTargetType.EnemyBack:
                    return FindAliveSlot(enemyTeam, FormationSlot.Back) ?? FindAlive(enemyTeam);
                case FormationBuffTargetType.LowestHpEnemy: return FindLowestHp(enemyTeam);
                default: return explicitTarget ?? owner;
            }
        }

        private static PvpSimHero FindAliveSlot(List<PvpSimHero> team, FormationSlot slot)
        {
            if (team == null) return null;
            for (int i = 0; i < team.Count; i++)
                if (team[i] != null && !team[i].IsDead && team[i].Slot == slot) return team[i];
            return null;
        }

        private static PvpSimHero FindAlive(List<PvpSimHero> team)
        {
            if (team == null) return null;
            for (int i = 0; i < team.Count; i++)
                if (team[i] != null && !team[i].IsDead) return team[i];
            return null;
        }

        private static PvpSimHero FindLowestHp(List<PvpSimHero> team)
        {
            PvpSimHero best = null;
            float bestPct = 2f;
            if (team != null)
            {
                for (int i = 0; i < team.Count; i++)
                {
                    var h = team[i];
                    if (h == null || h.IsDead || h.MaxHp <= 0) continue;
                    float p = h.CurrentHp / h.MaxHp;
                    if (p < bestPct) { bestPct = p; best = h; }
                }
            }
            return best;
        }

        /// <summary>Apply damage với shield absorption (shared với controller). §18 Shield effect.</summary>
        public static void ApplyDamage(PvpSimHero target, float dmg, StringBuilder log, int sourceHeroId)
        {
            if (target == null) return;
            if (target.Shield > 0f)
            {
                float absorbed = Mathf.Min(target.Shield, dmg);
                target.Shield -= absorbed;
                dmg -= absorbed;
            }
            target.CurrentHp -= dmg;
            target.DamageReceived += (long)dmg;
            if (target.CurrentHp <= 0f) { target.CurrentHp = 0f; target.IsDead = true; }
            log.Append(sourceHeroId).Append(":buff->").Append(target.HeroId).Append(' ').Append((long)dmg).Append(';');
        }
    }
}
