using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Boss
{
    public static class BossBuffFactory
    {
        public static BuffData CreateHellCurseDebuff()
        {
            return new BuffData
            {
                Id = "boss_3001_hell_curse",
                Name = "Hell Curse",
                Kind = BuffKind.Debuff,
                Duration = 5f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                Modifiers = new List<StatModifier>
                {
                    new (StatType.DEF, ModifierOp.Multiply, -0.2f)
                }
            };
        }

        public static BuffData CreateAtkDown25_5s()
        {
            return new BuffData
            {
                Id = "boss_atk_down_25_5s",
                Name = "ATK Down",
                Kind = BuffKind.Debuff,
                Duration = 5f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(StatType.ATK, ModifierOp.Multiply, -0.25f)
                }
            };
        }

        public static BuffData CreateDamageReduction40_10s()
        {
            return new BuffData
            {
                Id = "boss_self_damage_reduction_40_10s",
                Name = "Lava Skin",
                Kind = BuffKind.Buff,
                Duration = 10f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(StatType.DamageReduction, ModifierOp.Add, 0.4f)
                }
            };
        }

        public static BuffData CreateAttackSpeedDown20_5s()
        {
            return new BuffData
            {
                Id = "boss_attack_speed_down_20_5s",
                Name = "Attack Speed Down",
                Kind = BuffKind.Debuff,
                Duration = 5f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(StatType.AttackSpeed, ModifierOp.Multiply, -0.2f)
                }
            };
        }

        public static BuffData CreateSelfAtkUp25_5s()
        {
            return new BuffData
            {
                Id = "boss_self_atk_up_25_5s",
                Name = "Flame Body",
                Kind = BuffKind.Buff,
                Duration = 5f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                Modifiers = new List<StatModifier>
                {
                    new StatModifier(StatType.ATK, ModifierOp.Multiply, 0.25f)
                }
            };
        }

        public static BuffData CreateFreeze_1_5s()
        {
            return new BuffData
            {
                Id = "boss_freeze_1_5s",
                Name = "Freeze",
                Kind = BuffKind.Debuff,
                Duration = 1.5f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                StatusEffects = StatusEffectType.Freeze
            };
        }

        public static BuffData CreateStun_1_5s()
        {
            return new BuffData
            {
                Id = "boss_stun_1_5s",
                Name = "Stun",
                Kind = BuffKind.Debuff,
                Duration = 1.5f,
                MaxStacks = 1,
                StackRule = BuffStackRule.Refresh,
                StatusEffects = StatusEffectType.Stun
            };
        }
    }
}