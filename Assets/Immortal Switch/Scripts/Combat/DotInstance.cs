using System;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Combat
{
    [Serializable]
    public class DotInstance
    {
        public string EffectId { get; private set; }
        public ICombatUnit Source { get; private set; }
        public ICombatUnit Target { get; private set; }

        public DamageType DamageType { get; private set; }
        public DotStackRule StackRule { get; private set; }

        public float TickDamage { get; private set; }
        public float TickInterval { get; private set; }
        public float RemainingDuration { get; private set; }

        private float tickTimer;

        public bool IsExpired => RemainingDuration <= 0f;
        public bool IsValid => Target != null && Target.Stats.HealthModule != null;

        public DotInstance(
            string effectId,
            ICombatUnit source,
            ICombatUnit target,
            float tickDamage,
            float tickInterval,
            float duration,
            DamageType damageType,
            DotStackRule stackRule)
        {
            EffectId = effectId;
            Source = source;
            Target = target;
            TickDamage = tickDamage;
            TickInterval = tickInterval;
            RemainingDuration = duration;
            DamageType = damageType;
            StackRule = stackRule;
            tickTimer = tickInterval;
        }

        public void RefreshDuration(float newDuration)
        {
            RemainingDuration = newDuration;
        }

        public void ReplaceSnapshot(float newTickDamage, float newTickInterval, float newDuration, DamageType newDamageType)
        {
            TickDamage = newTickDamage;
            TickInterval = newTickInterval;
            RemainingDuration = newDuration;
            DamageType = newDamageType;
            tickTimer = newTickInterval;
        }

        public void Update(float deltaTime)
        {
            if (!IsValid || IsExpired)
                return;

            RemainingDuration -= deltaTime;
            tickTimer -= deltaTime;

            while (tickTimer <= 0f && RemainingDuration > 0f)
            {
                Target.TakeDamage(Source);
                tickTimer += TickInterval;
            }
        }
    }
}