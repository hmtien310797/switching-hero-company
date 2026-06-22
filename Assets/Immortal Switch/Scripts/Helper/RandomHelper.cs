using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Immortal_Switch.Scripts.Helper
{
    public static class RandomHelper
    {
        private static readonly Random Rnd = new();

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