using System;
using System.Numerics;

namespace Immortal_Switch.Scripts.Core
{
    public static class BigIntegerHelper
    {
        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

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
    }
}