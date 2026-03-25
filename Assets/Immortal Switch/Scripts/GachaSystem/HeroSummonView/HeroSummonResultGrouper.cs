using System.Collections.Generic;
using Immortal_Switch.Hero;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public static class HeroSummonResultGrouper
    {
        public static List<HeroSummonGroupedResultEntry> Group(HeroSummonResult result)
        {
            var groupedMap = new Dictionary<int, HeroSummonGroupedResultEntry>();
            var orderedResult = new List<HeroSummonGroupedResultEntry>();

            if (result == null || result.Entries == null)
                return orderedResult;

            for (int i = 0; i < result.Entries.Count; i++)
            {
                var entry = result.Entries[i];
                var hero = entry.HeroAsset as HeroDataSO;
                if (hero == null)
                    continue;

                if (!groupedMap.TryGetValue(hero.Id, out var grouped))
                {
                    grouped = new HeroSummonGroupedResultEntry
                    {
                        HeroAsset = entry.HeroAsset,
                        HeroName = entry.HeroName,
                        Count = 0,
                        IsNewHero = false,
                        TotalShardGained = 0,
                        HasPityHit = false,
                        Rarity = entry.Rarity
                    };

                    groupedMap.Add(hero.Id, grouped);
                    orderedResult.Add(grouped);
                }

                grouped.Count += 1;
                grouped.TotalShardGained += entry.ShardGained;

                if (entry.IsNewHero)
                    grouped.IsNewHero = true;

                if (entry.IsPityHit)
                    grouped.HasPityHit = true;
            }

            return orderedResult;
        }
    }
}