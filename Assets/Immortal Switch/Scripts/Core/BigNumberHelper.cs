using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Immortal_Switch.Scripts.Core
{
    public static class BigNumberHelper
    {
        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        /// <summary>
        /// Chia hai BigNumber và trả về kết quả dạng float.
        /// Thường dùng để tính progress UI.
        /// </summary>
        public static float DivideToFloat(BigNumber numerator, BigNumber denominator)
        {
            if (numerator.IsZero ||
                denominator.IsZero)
            {
                return 0f;
            }

            return (float)((numerator / denominator).Mantissa *
                           Math.Pow(1000.0, numerator.Tier - denominator.Tier));
        }

        public static float ClampProgress01(BigInteger current, BigInteger target)
        {
            if (target <= 0)
            {
                return 1f;
            }

            if (current <= 0)
            {
                return 0f;
            }

            if (current >= target)
            {
                return 1f;
            }

            var logCurrent = Log10(current);
            var logTarget = Log10(target);
            return Math.Clamp((float)Math.Pow(10, logCurrent - logTarget), 0f, 1f);
        }

        private static double Log10(BigInteger value)
        {
            var str = value.ToString();
            var digits = str.Length;
            var take = Math.Min(16, digits);
            var leading = double.Parse(str[..take]);
            return Math.Log10(leading) + (digits - take);
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
            if (value < 1000)
            {
                return value.ToString();
            }

            var tier = -1;
            decimal display = 0;
            var divisor = new BigInteger(1_000);

            while (value >= divisor)
            {
                tier++;
                display = (decimal)value / (decimal)divisor;
                divisor *= 1000;
            }

            var suffix = GetSuffix(tier);
            return display.ToString($"0.{new string('#', decimalPlaces)}") + suffix;
        }

        private static string GetSuffix(int index)
        {
            // 0 = A, 1 = B, ..., 25 = Z, 26 = AA
            var result = "";
            index++;

            while (index > 0)
            {
                index--;
                result = ALPHABET[index % 26] + result;
                index /= 26;
            }

            return result;
        }

        public static bool TryParse([CanBeNull] string text, out BigInteger result)
        {
            result = BigInteger.Zero;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim().ToUpperInvariant();

            var match = Regex.Match(text, @"^([0-9]+(?:\.[0-9]+)?)([A-Z]*)$");

            if (!match.Success)
            {
                return false;
            }

            if (!decimal.TryParse(
                    match.Groups[1].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                return false;
            }

            var suffix = match.Groups[2].Value;
            var multiplier = BigInteger.One;

            if (suffix.Length > 0)
            {
                var tier = GetSuffixIndex(suffix);
                multiplier = BigInteger.Pow(1000, tier + 1);
            }

            result = new BigInteger(number * (decimal)multiplier);
            return true;
        }

        public static BigInteger Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return BigInteger.Zero;
            }

            text = text.Trim().ToUpperInvariant();

            var match = Regex.Match(text, @"^([0-9]+(?:\.[0-9]+)?)([A-Z]*)$");

            if (!match.Success)
            {
                throw new FormatException($"Invalid BigInteger format: {text}");
            }

            var numberText = match.Groups[1].Value;
            var suffix = match.Groups[2].Value;

            if (!decimal.TryParse(
                    numberText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var number))
            {
                throw new FormatException($"Invalid number: {numberText}");
            }

            if (string.IsNullOrEmpty(suffix))
            {
                return new BigInteger(number);
            }

            var tier = GetSuffixIndex(suffix);
            var multiplier = BigInteger.Pow(1000, tier + 1);
            return new BigInteger(number * (decimal)multiplier);
        }

        private static int GetSuffixIndex(string suffix)
        {
            // A = 0, B = 1, ..., Z = 25, AA = 26
            var index = suffix.Aggregate(0, (current, c) => current * 26 + (c - 'A' + 1));
            return index - 1;
        }
    }
}