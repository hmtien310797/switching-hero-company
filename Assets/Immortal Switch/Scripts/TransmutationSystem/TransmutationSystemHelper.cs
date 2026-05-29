using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    public static class TransmutationSystemHelper
    {
        private const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static readonly string[] BaseSuffixes =
        {
            "",
            "K",
            "M",
            "B",
            "T",
        };

        /// <summary>
        /// Mapping modifier -> stat.
        /// </summary>
        private static readonly Dictionary<string, ModifierStatMapping> ModifierToStatTypeMap = new()
        {
            {
                TransmutationSystemModifierConstants.MODIFIER_HP,
                new ModifierStatMapping
                {
                    StatType = StatType.MaxHp,
                    Op = ModifierOp.Add
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_HP_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.MaxHp,
                    Op = ModifierOp.Multiply
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_ATK,
                new ModifierStatMapping
                {
                    StatType = StatType.Atk,
                    Op = ModifierOp.Add
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_ATK_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.Atk,
                    Op = ModifierOp.Multiply
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_DEF,
                new ModifierStatMapping
                {
                    StatType = StatType.Def,
                    Op = ModifierOp.Add
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_DEF_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.Def,
                    Op = ModifierOp.Multiply
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_SPD,
                new ModifierStatMapping
                {
                    StatType = StatType.AttackSpeed,
                    Op = ModifierOp.Add
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_SPD_PCT,
                new ModifierStatMapping
                {
                    StatType = StatType.AttackSpeed,
                    Op = ModifierOp.Multiply
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_CRIT_RATE,
                new ModifierStatMapping
                {
                    StatType = StatType.CritChance,
                    Op = ModifierOp.Add
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_CRIT_DMG,
                new ModifierStatMapping
                {
                    StatType = StatType.CritDamage,
                    Op = ModifierOp.Add
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_ACC,
                new ModifierStatMapping
                {
                    StatType = StatType.Accuracy,
                    Op = ModifierOp.Add
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.DamageToNormalMonster,
                    Op = ModifierOp.Multiply
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_BASIC_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.ClassSkillDamage,
                    Op = ModifierOp.Multiply
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_SKILL_DMG_BONUS,
                new ModifierStatMapping
                {
                    StatType = StatType.ExclusiveSkillDamage,
                    Op = ModifierOp.Multiply
                }
            },
            {
                TransmutationSystemModifierConstants.MODIFIER_FINAL_DMG_BONUS,
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

        /// <summary>
        /// Format BigInteger theo kiểu idle game.
        /// Ví dụ:
        /// 999 -> 999
        /// 1_000 -> 1K
        /// 1_000_000 -> 1M
        /// 1_000_000_000 -> 1B
        /// 1_000_000_000_000 -> 1T
        /// 1_000_000_000_000_000 -> 1AA
        /// </summary>
        public static string Format(
            BigInteger value,
            int decimalPlaces = 2
        )
        {
            if (value < 0)
            {
                return "-" + Format(BigInteger.Abs(value), decimalPlaces);
            }

            if (value < 1000)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            var digits = value.ToString().Length;
            var groupIndex = (digits - 1) / 3;

            var suffix = GetSuffix(groupIndex);
            var divisor = BigInteger.Pow(1000, groupIndex);

            var integerPart = value / divisor;
            var remainder = value % divisor;

            var decimalText = BuildDecimalText(
                remainder,
                divisor,
                decimalPlaces
            );

            return integerPart + decimalText + suffix;
        }

        /// <summary>
        /// Parse idle game notation sang BigInteger.
        /// Ví dụ:
        /// 1K
        /// 1.5M
        /// 10AA
        /// </summary>
        public static BigInteger Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return BigInteger.Zero;
            }

            text = text.Trim().ToUpperInvariant();

            var match = Regex.Match(
                text,
                @"^([0-9]+(\.[0-9]+)?)([A-Z]*)$"
            );

            if (!match.Success)
            {
                throw new FormatException(
                    $"Invalid idle number format: {text}"
                );
            }

            var numberPart = match.Groups[1].Value;
            var suffixPart = match.Groups[3].Value;

            var decimalValue = decimal.Parse(
                numberPart,
                CultureInfo.InvariantCulture
            );

            var groupIndex = GetGroupIndex(suffixPart);
            var multiplier = BigInteger.Pow(1000, groupIndex);
            var scaled = decimalValue * (decimal)multiplier;
            return new BigInteger(scaled);
        }

        private static string BuildDecimalText(
            BigInteger remainder,
            BigInteger divisor,
            int decimalPlaces
        )
        {
            if (decimalPlaces <= 0 ||
                remainder == 0)
            {
                return string.Empty;
            }

            var scale = BigInteger.Pow(10, decimalPlaces);
            var decimalValue = remainder * scale / divisor;

            if (decimalValue == 0)
            {
                return string.Empty;
            }

            var text = decimalValue
                .ToString(CultureInfo.InvariantCulture)
                .PadLeft(decimalPlaces, '0');

            text = text.TrimEnd('0');

            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return "." + text;
        }

        private static string GetSuffix(int groupIndex)
        {
            if (groupIndex < BaseSuffixes.Length)
            {
                return BaseSuffixes[groupIndex];
            }

            // groupIndex:
            // 1 = K
            // 2 = M
            // 3 = B
            // 4 = T
            // 5 = AA
            // 6 = AB
            // 7 = AC
            var alphabetIndex = groupIndex - BaseSuffixes.Length;
            return ToAlphabetSuffix(alphabetIndex);
        }

        private static int GetGroupIndex(string suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return 0;
            }

            for (var i = 0; i < BaseSuffixes.Length; i++)
            {
                if (BaseSuffixes[i] == suffix)
                {
                    return i;
                }
            }

            return BaseSuffixes.Length + FromAlphabetSuffix(suffix);
        }

        private static string ToAlphabetSuffix(int index)
        {
            var first = index / 26;
            var second = index % 26;
            return $"{LETTERS[first]}{LETTERS[second]}";
        }

        private static int FromAlphabetSuffix(string suffix)
        {
            if (suffix.Length != 2)
            {
                throw new FormatException(
                    $"Invalid alphabet suffix: {suffix}"
                );
            }

            var first = LETTERS.IndexOf(suffix[0]);
            var second = LETTERS.IndexOf(suffix[1]);

            if (first < 0 ||
                second < 0)
            {
                throw new FormatException(
                    $"Invalid alphabet suffix: {suffix}"
                );
            }

            return first * 26 + second;
        }
    }
}