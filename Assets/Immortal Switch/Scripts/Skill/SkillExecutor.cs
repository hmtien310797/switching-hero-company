using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.Boss;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public static class SkillExecutor
    {
        public static void ExecutePhase(PlayerHeroController caster, SkillPhaseData phase, Vector3 targetPosition, float range)
        {
            if (caster == null || phase == null || phase.Effects == null || phase.Effects.Count == 0)
                return;

            // var targets = PvEBattleController.Instance?.GetNearestEnemiesInRange(targetPosition, range);
            //
            // if (targets == null || targets.Count == 0)
            //     return;
            //
            // for (int i = 0; i < phase.Effects.Count; i++)
            // {
            //     ExecuteEffect(caster, targets, phase.Effects[i]);
            // }
        }

        public static void ExecuteEffect(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
        {
            if (caster == null || effect == null || targets == null || targets.Count == 0)
                return;

            if (!RollChance(effect.ChancePercent))
                return;

            switch (effect.EffectType)
            {
                case SkillEffectType.Damage:
                    ExecuteDamage(caster, targets, effect);
                    break;

                case SkillEffectType.ModifyStatPercent:
                    ExecuteModifyStatPercent(caster, targets, effect);
                    break;

                case SkillEffectType.ApplyStatus:
                    ExecuteApplyStatus(caster, targets, effect);
                    break;

                case SkillEffectType.HealPercentMaxHp:
                    ExecuteHealPercentMaxHp(caster, effect);
                    break;

                case SkillEffectType.ShieldPercentMaxHp:
                    Debug.LogWarning("SkillExecutor: ShieldPercentMaxHp chưa implement.");
                    break;

                case SkillEffectType.ReflectDamagePercent:
                    Debug.LogWarning("SkillExecutor: ReflectDamagePercent chưa implement.");
                    break;

                case SkillEffectType.DamageReductionPercent:
                    Debug.LogWarning("SkillExecutor: DamageReductionPercent chưa implement.");
                    break;

                case SkillEffectType.AddMark:
                    Debug.LogWarning("SkillExecutor: AddMark chưa implement.");
                    break;

                case SkillEffectType.TeleportToTarget:
                    Debug.LogWarning("SkillExecutor: TeleportToTarget chưa implement.");
                    break;

                default:
                    Debug.LogWarning($"SkillExecutor: Chưa xử lý EffectType {effect.EffectType}");
                    break;
            }
        }

        private static void ExecuteDamage(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
        {
            float factor = effect.DamageMultiplier / 100f;
            if (factor <= 0f) return;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null || target.IsDead) continue;

                target.OnReceiveDamage(factor, null, caster);
            }
        }

        private static void ExecuteModifyStatPercent(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
        {
            Debug.LogWarning("SkillExecutor: ModifyStatPercent mới bridge tạm, chưa nối BuffData runtime.");
        }

        private static void ExecuteApplyStatus(PlayerHeroController caster, List<MonsterScrepController> targets, SkillEffectData effect)
        {
            Debug.LogWarning("SkillExecutor: ApplyStatus mới bridge tạm, chưa nối Buff/Status runtime.");
        }

        private static void ExecuteHealPercentMaxHp(PlayerHeroController caster, SkillEffectData effect)
        {
            if (caster == null) return;

            float healValue = caster.MaxHp * (effect.Value / 100f);
            if (healValue <= 0f) return;

            caster.Heal(healValue);
        }

        private static bool RollChance(float chancePercent)
        {
            if (chancePercent >= 100f) return true;
            if (chancePercent <= 0f) return false;

            return UnityEngine.Random.value * 100f <= chancePercent;
        }
    }
}