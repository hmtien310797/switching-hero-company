using System.Numerics;

namespace Immortal_Switch.Scripts.Core
{
    public static class BigIntegerHelper
    {
        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

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