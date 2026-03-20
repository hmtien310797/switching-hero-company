using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.StatSystem
{
    [Serializable]
    public class BuffData
    {
        public string Id;
        public string Name;
        public BuffKind Kind;
        public float Duration;
        public int MaxStacks = 1;
        public BuffStackRule StackRule = BuffStackRule.Refresh;
        public List<StatModifier> Modifiers = new();

        // Periodic
        public PeriodicEffectType PeriodicEffectType = PeriodicEffectType.None;
        public float TickInterval = 0f;
        public float PeriodicValue = 0f;
        public DamageType PeriodicDamageType = DamageType.Normal;

        // Status
        public StatusEffectType StatusEffects = StatusEffectType.None;

        public BuffData() { }

        public BuffData(string id, string name, BuffKind kind, float duration, int maxStacks, BuffStackRule stackRule)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Duration = duration;
            MaxStacks = maxStacks;
            StackRule = stackRule;
        }
    }
}