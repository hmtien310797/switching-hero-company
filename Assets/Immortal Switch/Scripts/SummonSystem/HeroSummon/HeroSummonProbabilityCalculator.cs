using System.Collections.Generic;
using System.Linq;
using Common;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public static class HeroSummonProbabilityCalculator
    {
        public static HeroSummonProbabilityViewData Build(HeroSummonConfigSO config, int summonLevel)
        {
            var result = new HeroSummonProbabilityViewData
            {
                SummonLevel = summonLevel
            };

            if (config == null)
                return result;

            var levelEntry = config.GetLevelEntry(summonLevel);
            if (levelEntry == null)
                return result;

            var rarityRates = BuildRarityRateMap(levelEntry);

            foreach (var pair in rarityRates)
            {
                var rarity = pair.Key;
                float rarityRate = pair.Value;

                if (rarityRate <= 0f)
                    continue;

                var heroes = MasterDataCache.Instance.GetAllHeroData()
                    .Where(x => x != null && x.IsAvailableInSummon && x.SummonRarity == rarity)
                    .ToList();

                if (heroes.Count == 0)
                    continue;

                int totalWeight = 0;
                for (int i = 0; i < heroes.Count; i++)
                    totalWeight += Mathf.Max(1, heroes[i].SummonWeight);

                if (totalWeight <= 0)
                    continue;

                var section = new HeroSummonProbabilitySectionData
                {
                    Rarity = rarity,
                    RarityLabel = GetRarityLabel(rarity),
                    TotalRatePercent = rarityRate
                };

                for (int i = 0; i < heroes.Count; i++)
                {
                    var hero = heroes[i];
                    int heroWeight = Mathf.Max(1, hero.SummonWeight);
                    float heroRate = rarityRate * heroWeight / totalWeight;

                    section.Heroes.Add(new HeroSummonProbabilityHeroData
                    {
                        Hero = hero,
                        ProbabilityPercent = heroRate
                    });
                }

                result.Sections.Add(section);
            }

            result.Sections = result.Sections
                .OrderBy(x => GetRarityOrder(x.Rarity))
                .ToList();

            return result;
        }

        private static Dictionary<SummonRarity, float> BuildRarityRateMap(HeroSummonLevelEntry levelEntry)
        {
            return new Dictionary<SummonRarity, float>
            {
                { SummonRarity.Mythic, levelEntry.MythicRate },
                { SummonRarity.Legendary, levelEntry.LegendaryRate },
                { SummonRarity.Epic, levelEntry.EpicRate },
                { SummonRarity.Rare, levelEntry.RareRate },
                { SummonRarity.UnCommon, levelEntry.UnCommonRate },
                { SummonRarity.Common, levelEntry.CommonRate }
            };
        }

        private static int GetRarityOrder(SummonRarity rarity)
        {
            switch (rarity)
            {
                case SummonRarity.Mythic: return 0;
                case SummonRarity.Legendary: return 1;
                case SummonRarity.Epic: return 2;
                case SummonRarity.Rare: return 3;
                case SummonRarity.UnCommon: return 4;
                case SummonRarity.Common: return 5;
                default: return 999;
            }
        }

        private static string GetRarityLabel(SummonRarity rarity)
        {
            switch (rarity)
            {
                case SummonRarity.Mythic: return "Mythic";
                case SummonRarity.Legendary: return "Legendary";
                case SummonRarity.Epic: return "Epic";
                case SummonRarity.Rare: return "Rare";
                case SummonRarity.UnCommon: return "UnCommon";
                case SummonRarity.Common: return "Common";
                default: return rarity.ToString();
            }
        }
    }
}