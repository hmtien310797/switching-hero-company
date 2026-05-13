using System.Collections.Generic;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public static class WeaponSummonResultGrouper
    {
        public static List<WeaponSummonGroupedResultEntry> Group(WeaponSummonResult result)
        {
            var map = new Dictionary<string, WeaponSummonGroupedResultEntry>();
            var ordered = new List<WeaponSummonGroupedResultEntry>();

            if (result == null || result.Entries == null)
                return ordered;

            for (int i = 0; i < result.Entries.Count; i++)
            {
                var entry = result.Entries[i];
                if (entry == null || entry.Weapon == null)
                    continue;

                string key = BuildKey(entry);

                if (!map.TryGetValue(key, out var grouped))
                {
                    grouped = new WeaponSummonGroupedResultEntry
                    {
                        Weapon = entry.Weapon,
                        WeaponId = entry.WeaponId,
                        WeaponName = entry.WeaponName,
                        Icon = entry.Icon,
                        Tier = entry.Tier,
                        Star = entry.Star,
                        Count = 0,
                        TotalShardGained = 0,
                        TotalShardAfter = entry.TotalShardAfter,
                        IsNewWeapon = false
                    };

                    map.Add(key, grouped);
                    ordered.Add(grouped);
                }

                grouped.Count += 1;
                grouped.TotalShardGained += entry.ShardGained;
                grouped.TotalShardAfter = entry.TotalShardAfter;

                if (entry.IsNewWeapon)
                    grouped.IsNewWeapon = true;
            }

            return ordered;
        }

        private static string BuildKey(WeaponSummonResultEntry entry)
        {
            return $"{entry.WeaponId}_{entry.Tier}_{entry.Star}";
        }
    }
}