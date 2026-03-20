using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Combat
{
    public class DotModule
    {
        public event Action<DotInstance> OnDotApplied;
        public event Action<DotInstance> OnDotRemoved;
        public event Action<DotInstance> OnDotRefreshed;
        public event Action<DotInstance> OnDotReplaced;

        private readonly List<DotInstance> activeDots = new();

        public IReadOnlyList<DotInstance> ActiveDots => activeDots;

        public void Update(float deltaTime)
        {
            for (int i = activeDots.Count - 1; i >= 0; i--)
            {
                var dot = activeDots[i];

                if (dot == null || !dot.IsValid)
                {
                    activeDots.RemoveAt(i);
                    continue;
                }

                dot.Update(deltaTime);

                if (dot.IsExpired)
                {
                    OnDotRemoved?.Invoke(dot);
                    activeDots.RemoveAt(i);
                }
            }
        }

        public void ApplyDotSnapshot(
            string effectId,
            ICombatUnit source,
            ICombatUnit target,
            float tickDamage,
            float tickInterval,
            float duration,
            DamageType damageType,
            DotStackRule stackRule)
        {
            if (target == null || target.Stats.HealthModule == null)
                return;

            switch (stackRule)
            {
                case DotStackRule.StackIndependent:
                {
                    var newDot = new DotInstance(
                        effectId, source, target, tickDamage, tickInterval, duration, damageType, stackRule
                    );
                    activeDots.Add(newDot);
                    OnDotApplied?.Invoke(newDot);
                    break;
                }

                case DotStackRule.Refresh:
                {
                    var existing = FindDot(effectId, target);
                    if (existing == null)
                    {
                        var newDot = new DotInstance(
                            effectId, source, target, tickDamage, tickInterval, duration, damageType, stackRule
                        );
                        activeDots.Add(newDot);
                        OnDotApplied?.Invoke(newDot);
                    }
                    else
                    {
                        existing.RefreshDuration(duration);
                        OnDotRefreshed?.Invoke(existing);
                    }
                    break;
                }

                case DotStackRule.Replace:
                {
                    var existing = FindDot(effectId, target);
                    if (existing == null)
                    {
                        var newDot = new DotInstance(
                            effectId, source, target, tickDamage, tickInterval, duration, damageType, stackRule
                        );
                        activeDots.Add(newDot);
                        OnDotApplied?.Invoke(newDot);
                    }
                    else
                    {
                        existing.ReplaceSnapshot(tickDamage, tickInterval, duration, damageType);
                        OnDotReplaced?.Invoke(existing);
                    }
                    break;
                }
            }
        }

        private DotInstance FindDot(string effectId, ICombatUnit target)
        {
            for (int i = 0; i < activeDots.Count; i++)
            {
                var dot = activeDots[i];
                if (dot.Target == target && dot.EffectId == effectId)
                    return dot;
            }

            return null;
        }
    }
}