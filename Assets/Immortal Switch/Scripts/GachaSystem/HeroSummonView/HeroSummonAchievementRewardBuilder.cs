using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public static class HeroSummonAchievementRewardBuilder
    {
        public static SummonAchievementRewardListData BuildHeroic(
            HeroSummonConfigSO config,
            HeroSummonSaveData saveData,
            HeroSummonRewardVisualConfigSO rewardVisualConfig)
        {
            var result = new SummonAchievementRewardListData
            {
                Tab = SummonAchievementTab.Heroic
            };

            if (config == null || config.LevelRewards == null)
                return result;

            var sortedRewards = config.LevelRewards
                .Where(x => x != null)
                .OrderBy(x => x.SummonLevel)
                .ToList();

            for (int i = 0; i < sortedRewards.Count; i++)
            {
                var entry = sortedRewards[i];
                if (entry.RewardItems == null || entry.RewardItems.Count == 0)
                    continue;

                var reward = entry.RewardItems[0];
                var visual = rewardVisualConfig != null ? rewardVisualConfig.Get(reward) : null;

                bool isClaimed = saveData != null && saveData.ClaimedRewardLevels.Contains(entry.SummonLevel);

                result.Items.Add(new SummonAchievementRewardItemData
                {
                    Level = entry.SummonLevel,
                    Title = $"Summon Level {entry.SummonLevel}",
                    RewardText = BuildRewardText(reward, visual),
                    RewardIcon = visual != null ? visual.Icon : null,
                    State = isClaimed
                        ? SummonAchievementRewardState.Claimed
                        : SummonAchievementRewardState.Normal
                });
            }

            return result;
        }

        private static string BuildRewardText(HeroSummonRewardItem reward, RewardVisualEntry visual)
        {
            if (reward == null)
                return string.Empty;

            switch (reward.RewardType)
            {
                case HeroSummonRewardType.Currency:
                {
                    string name = visual != null && !string.IsNullOrEmpty(visual.DisplayName)
                        ? visual.DisplayName
                        : reward.CurrencyType.ToString();

                    return $"{name} x{reward.Amount}";
                }

                case HeroSummonRewardType.RandomHero:
                {
                    string name = visual != null && !string.IsNullOrEmpty(visual.DisplayName)
                        ? visual.DisplayName
                        : $"Random {reward.HeroRarity} Hero";

                    return $"{name} x{reward.Amount}";
                }
            }

            return reward.Description;
        }
    }
}