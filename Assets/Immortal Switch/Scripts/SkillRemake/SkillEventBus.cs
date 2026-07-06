using System;
using Immortal_Switch.Scripts.Combat;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public static class SkillEventBus
    {
        public static event Action<SkillEventContext> EventRaised;

        public static void Raise(SkillEventContext context)
        {
            if (context == null)
                return;

            EventRaised?.Invoke(context);
        }
    }

    public sealed class SkillEventContext
    {
        public SkillTriggerEventType EventType;
        public HeroActor Owner;
        public ICombatUnit Source;
        public ICombatUnit Target;
        public SkillDataSO Skill;
        public DamageResult DamageResult;
        public float DamageAmount;
        public bool IsCritical;
    }
}
