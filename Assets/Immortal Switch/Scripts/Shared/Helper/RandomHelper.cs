using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Immortal_Switch.Scripts.Shared.Helper
{
    public static class RandomHelper
    {
        private static readonly Random Rnd = new();

        /// <summary>
        /// Random index của một phần tử theo trọng số.
        /// Weight nhỏ hơn hoặc bằng 0 sẽ bị bỏ qua.
        /// Trả về -1 nếu không tìm thấy phần tử hợp lệ.
        /// </summary>
        public static int RandomIndexByWeight<T>(
            IReadOnlyList<T> items,
            Func<T, double> weightSelector)
        {
            if (items == null ||
                items.Count == 0 ||
                weightSelector == null)
            {
                return -1;
            }

            var totalWeight = items.Select(weightSelector).Where(weight => weight > 0d).Sum();

            if (totalWeight <= 0d)
            {
                return -1;
            }

            var random = Rnd.NextDouble() * totalWeight;
            var lastValidIndex = -1;

            for (var i = 0; i < items.Count; i++)
            {
                var weight = weightSelector(items[i]);

                if (weight <= 0d)
                {
                    continue;
                }

                lastValidIndex = i;
                random -= weight;

                if (random <= 0d)
                {
                    return i;
                }
            }

            // Tránh sai số floating point và không trả về item có weight bằng 0.
            return lastValidIndex;
        }

        /// <summary>
        /// Random 1 phần tử theo trọng số.
        /// Weight <= 0 sẽ bị bỏ qua.
        /// </summary>
        [CanBeNull]
        public static T RandomByWeight<T>(
            IReadOnlyList<T> items,
            Func<T, double> weightSelector)
        {
            if (items.Count == 0)
            {
                return default;
            }

            var totalWeight = items.Select(weightSelector).Where(weight => weight > 0).Sum();

            if (totalWeight <= 0)
            {
                return default;
            }

            var random = Rnd.NextDouble() * totalWeight;

            foreach (var item in items)
            {
                var weight = weightSelector(item);

                if (weight <= 0)
                {
                    continue;
                }

                random -= weight;

                if (random <= 0)
                {
                    return item;
                }
            }

            // Tránh lỗi do floating point
            return items[^1];
        }
    }
}