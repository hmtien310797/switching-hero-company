using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public static class SkillCombatEventReporter
    {
        public static void ReportDamageDealt(HeroActor owner, ICombatUnit source, ICombatUnit target, SkillDataSO skill, DamageResult result)
        {
            if (source == null || target == null)
                return;

            SkillEventBus.Raise(new SkillEventContext
            {
                EventType = SkillTriggerEventType.OnHit,
                Owner = owner,
                Source = source,
                Target = target,
                Skill = skill,
                DamageResult = result,
                DamageAmount = result.Damage,
                IsCritical = result.DamageType == DamageType.Crit
            });

            SkillEventBus.Raise(new SkillEventContext
            {
                EventType = SkillTriggerEventType.OnDamageDealt,
                Owner = owner,
                Source = source,
                Target = target,
                Skill = skill,
                DamageResult = result,
                DamageAmount = result.Damage,
                IsCritical = result.DamageType == DamageType.Crit
            });

            if (result.DamageType == DamageType.Crit)
            {
                SkillEventBus.Raise(new SkillEventContext
                {
                    EventType = SkillTriggerEventType.OnCriticalHit,
                    Owner = owner,
                    Source = source,
                    Target = target,
                    Skill = skill,
                    DamageResult = result,
                    DamageAmount = result.Damage,
                    IsCritical = true
                });
            }
        }
    }
}
