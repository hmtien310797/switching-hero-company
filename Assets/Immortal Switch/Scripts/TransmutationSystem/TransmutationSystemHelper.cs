using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.ItemSystem;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    public static class TransmutationSystemHelper
    {
        /// <summary>
        /// Mapping modifier -> stat.
        /// </summary>
        private static readonly Dictionary<string, ModifierStatMapping> ModifierToStatTypeMap = new()
        {
            {
                StatSystemModifierConstants.MODIFIER_HP,
                new ModifierStatMapping
                {
                    StatType = StatType.MaxHp,
                    Op = ModifierOp.Add
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_HP_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.MaxHp,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_ATK,
                new ModifierStatMapping
                {
                    StatType = StatType.Atk,
                    Op = ModifierOp.Add
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_ATK_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.Atk,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_DEF,
                new ModifierStatMapping
                {
                    StatType = StatType.Def,
                    Op = ModifierOp.Add
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_DEF_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.Def,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_SPD,
                new ModifierStatMapping
                {
                    StatType = StatType.AttackSpeed,
                    Op = ModifierOp.Add
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_SPD_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.AttackSpeed,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_CRIT_RATE,
                new ModifierStatMapping
                {
                    StatType = StatType.CritChance,
                    Op = ModifierOp.Add
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_CRIT_DMG,
                new ModifierStatMapping
                {
                    StatType = StatType.CritDamage,
                    Op = ModifierOp.Add
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_ACC,
                new ModifierStatMapping
                {
                    StatType = StatType.Accuracy,
                    Op = ModifierOp.Add
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.DamageToNormalMonster,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_BASIC_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.ClassSkillDamage,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_SKILL_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.ExclusiveSkillDamage,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatSystemModifierConstants.MODIFIER_FINAL_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.SwitchSkillDamage,
                    Op = ModifierOp.Multiply
                }
            },
        };

        /// <summary>
        /// Mapping stat type + op -> modifier.
        /// </summary>
        private static readonly Dictionary<(StatType, ModifierOp), string> StatTypeToModifierMap =
            ModifierToStatTypeMap.ToDictionary(
                x => (x.Value.StatType, x.Value.Op),
                x => x.Key
            );

        /// <summary>
        /// Lấy mapping info từ modifier key.
        /// </summary>
        public static ModifierStatMapping ToStatMapping(string modifier)
        {
            return ModifierToStatTypeMap.TryGetValue(modifier, out var mapping)
                ? mapping
                : throw new Exception($"Unsupported modifier: {modifier}");
        }

        /// <summary>
        /// Lấy modifier key từ stat type + operation.
        /// </summary>
        public static string ToModifier(
            StatType statType,
            ModifierOp op
        )
        {
            return StatTypeToModifierMap.TryGetValue((statType, op), out var modifier)
                ? modifier
                : throw new Exception(
                    $"Unsupported stat type: {statType}, op: {op}"
                );
        }

        /// <summary>
        /// Kiểm tra modifier có tồn tại hay không.
        /// </summary>
        public static bool IsValidModifier(string modifier)
        {
            return ModifierToStatTypeMap.ContainsKey(modifier);
        }
    }
}