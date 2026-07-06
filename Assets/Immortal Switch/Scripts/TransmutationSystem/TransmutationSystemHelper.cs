using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Items;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

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
                StatModifierConstants.MODIFIER_HP,
                new ModifierStatMapping
                {
                    StatType = StatType.MaxHp,
                    Op = ModifierOp.Add
                }
            },
            {
                StatModifierConstants.MODIFIER_HP_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.MaxHp,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatModifierConstants.MODIFIER_ATK,
                new ModifierStatMapping
                {
                    StatType = StatType.Atk,
                    Op = ModifierOp.Add
                }
            },
            {
                StatModifierConstants.MODIFIER_ATK_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.Atk,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatModifierConstants.MODIFIER_DEF,
                new ModifierStatMapping
                {
                    StatType = StatType.Def,
                    Op = ModifierOp.Add
                }
            },
            {
                StatModifierConstants.MODIFIER_DEF_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.Def,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatModifierConstants.MODIFIER_SPD,
                new ModifierStatMapping
                {
                    StatType = StatType.AttackSpeed,
                    Op = ModifierOp.Add
                }
            },
            {
                StatModifierConstants.MODIFIER_SPD_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.AttackSpeed,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatModifierConstants.MODIFIER_CRIT_RATE,
                new ModifierStatMapping
                {
                    StatType = StatType.CritChance,
                    Op = ModifierOp.Add
                }
            },
            {
                StatModifierConstants.MODIFIER_CRIT_DMG,
                new ModifierStatMapping
                {
                    StatType = StatType.CritDamage,
                    Op = ModifierOp.Add
                }
            },
            {
                StatModifierConstants.MODIFIER_ACC,
                new ModifierStatMapping
                {
                    StatType = StatType.Accuracy,
                    Op = ModifierOp.Add
                }
            },
            {
                StatModifierConstants.MODIFIER_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.DamageToNormalMonster,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatModifierConstants.MODIFIER_BASIC_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.ClassSkillDamage,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatModifierConstants.MODIFIER_SKILL_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.ExclusiveSkillDamage,
                    Op = ModifierOp.Multiply
                }
            },
            {
                StatModifierConstants.MODIFIER_FINAL_DMG_BONUS,
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
            return ModifierToStatTypeMap.GetValueOrDefault(modifier.ToUpper());
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

        /// <summary>
        /// Convert modifiers trả về từ server (stat_type/op dạng string) sang StatModifier dùng ở client.
        /// </summary>
        public static List<StatModifier> ToModifiers(List<TransmutationModifierDto> dtos)
        {
            var result = new List<StatModifier>();

            if (dtos == null)
            {
                return result;
            }

            foreach (var dto in dtos)
            {
                var mapping = ToStatMapping(dto.StatType);

                if (mapping == null)
                {
                    Debug.LogError($"Transmutation: unknown stat_type from server: {dto.StatType}");
                    continue;
                }

                if (!Enum.TryParse<ModifierOp>(dto.Op, true, out var op))
                {
                    Debug.LogError($"Transmutation: unknown op from server: {dto.Op}");
                    continue;
                }

                result.Add(new StatModifier(mapping.StatType, op, (float)dto.Value, string.Empty, dto.IsUnique));
            }

            return result;
        }

        /// <summary>Dùng cho equips[]/pending trả về từ server — có item_type riêng.</summary>
        public static PlayerEquipItem ToPlayerEquipItem(TransmutationItemDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new PlayerEquipItem
            {
                CfgId = dto.CfgId,
                ItemType = dto.ItemType,
                Level = dto.Level,
                Tier = dto.Tier,
                Modifiers = ToModifiers(dto.Modifiers),
            };
        }

        /// <summary>Dùng cho current_equip/equipped/replaced trả về từ server — item_type lấy từ field ngoài (xem TransmutationItemBaseDto).</summary>
        public static PlayerEquipItem ToPlayerEquipItem(string itemType, TransmutationItemBaseDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new PlayerEquipItem
            {
                CfgId = dto.CfgId,
                ItemType = itemType,
                Level = dto.Level,
                Tier = dto.Tier,
                Modifiers = ToModifiers(dto.Modifiers),
            };
        }
    }
}