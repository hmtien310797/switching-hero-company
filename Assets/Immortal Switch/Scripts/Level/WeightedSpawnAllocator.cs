using System;
using System.Collections.Generic;
using System.Linq;

namespace Immortal_Switch.Scripts.Level
{
    public static class WeightedSpawnAllocator
    {
        public static Dictionary<int, int> AllocateCounts(
            List<int> a,
            List<List<float>> spawnRateSets,
            int X,
            Random rng = null)
        {
            if (a == null || a.Count < 2 || a.Count > 4)
                throw new ArgumentException("a.Count must be in [2..4]");
            if (spawnRateSets == null || spawnRateSets.Count == 0)
                throw new ArgumentException("spawnRateSets is empty");

            rng ??= new Random();

            int n = a.Count;

            // 1) Pick a random rate set that matches n
            var candidates = spawnRateSets.Where(s => s != null && s.Count == n).ToList();
            if (candidates.Count == 0)
                throw new ArgumentException($"No spawnRate set with length = {n}");

            var rates = candidates[rng.Next(candidates.Count)];

            // validate rates
            double sumW = 0;
            for (int i = 0; i < n; i++)
            {
                if (rates[i] < 0) throw new ArgumentException("Rates must be >= 0");
                sumW += rates[i];
            }
            if (sumW <= 0) throw new ArgumentException("Sum of rates must be > 0");

            // 2) total = half of X (integer), remainder of X handled later
            int total = X / 2;
            int remX = X % 2; // nếu X lẻ: 1

            // 3) Compute base counts by floor(expected)
            int[] counts = new int[n];
            int assigned = 0;

            for (int i = 0; i < n; i++)
            {
                double expected = total * (rates[i] / sumW);
                int c = (int)Math.Floor(expected);
                counts[i] = c;
                assigned += c;
            }

            // 4) Distribute rounding remainder randomly to any element in a
            int remaining = total - assigned;
            for (int k = 0; k < remaining; k++)
            {
                int idx = rng.Next(n);
                counts[idx]++;
            }

            // 5) If X was odd, distribute that extra 1 randomly too (as you requested)
            for (int k = 0; k < remX; k++)
            {
                int idx = rng.Next(n);
                counts[idx]++;
            }

            // 6) Build result map: id -> count
            var result = new Dictionary<int, int>(n);
            for (int i = 0; i < n; i++)
                result[a[i]] = counts[i];

            return result;
        }
    }
}