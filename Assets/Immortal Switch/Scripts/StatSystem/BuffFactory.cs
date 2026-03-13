using System.Collections.Generic;

namespace Immortal_Switch.Scripts.StatSystem
{
    public static class BuffFactory
    {
        public static BuffData CreatePoison()
        {
            return new BuffData
            {
                Id = "poison",
                Name = "Poison",
                Kind = BuffKind.Debuff,
                Duration = 5f,
                MaxStacks = 5,
                StackRule = BuffStackRule.Stack,
                PeriodicEffectType = PeriodicEffectType.DamageOverTime,
                TickInterval = 1f,
                PeriodicValue = 20f,
                PeriodicDamageType = DamageType.Poison
            };
        }

        public static BuffData CreateBurn()
        {
            return new BuffData
            {
                Id = "burn",
                Name = "Burn",
                Kind = BuffKind.Debuff,
                Duration = 4f,
                MaxStacks = 3,
                StackRule = BuffStackRule.Stack,
                PeriodicEffectType = PeriodicEffectType.DamageOverTime,
                TickInterval = 0.5f,
                PeriodicValue = 15f,
                PeriodicDamageType = DamageType.Burn,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(StatType.DEF, ModifierOp.Add, -10f)
                }
            };
        }

        public static BuffData CreateRegen()
        {
            return new BuffData
            {
                Id = "regen",
                Name = "Regen",
                Kind = BuffKind.Buff,
                Duration = 6f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                PeriodicEffectType = PeriodicEffectType.HealOverTime,
                TickInterval = 1f,
                PeriodicValue = 30f
            };
        }

        public static BuffData CreateStun()
        {
            return new BuffData
            {
                Id = "stun",
                Name = "Stun",
                Kind = BuffKind.Debuff,
                Duration = 2f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                StatusEffects = StatusEffectType.Stun
            };
        }

        public static BuffData CreateSilence()
        {
            return new BuffData
            {
                Id = "silence",
                Name = "Silence",
                Kind = BuffKind.Debuff,
                Duration = 3f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                StatusEffects = StatusEffectType.Silence
            };
        }

        public static BuffData CreateFreeze()
        {
            return new BuffData
            {
                Id = "freeze",
                Name = "Freeze",
                Kind = BuffKind.Debuff,
                Duration = 2.5f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                StatusEffects = StatusEffectType.Freeze,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(StatType.AttackSpeed, ModifierOp.Multiply, -0.5f)
                }
            };
        }
    }
}