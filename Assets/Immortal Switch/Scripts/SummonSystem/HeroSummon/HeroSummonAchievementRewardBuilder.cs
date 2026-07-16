using System.Linq;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public static class HeroSummonAchievementRewardBuilder
    {
        public static SummonAchievementRewardListData BuildHeroic(
            HeroSummonConfigSO config,
            HeroSummonSaveData saveData)
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

                bool isClaimed = saveData != null && saveData.ClaimedRewardLevels.Contains(entry.SummonLevel);

                result.Items.Add(new SummonAchievementRewardItemData
                {
                    Level = entry.SummonLevel,
                    Title = $"Summon Level {entry.SummonLevel}",
                    RewardText = BuildRewardText(reward),
                    RewardIcon = DatabaseManager.Instance.ItemDb.LoadIconByItemId(reward.ItemId),
                    State = isClaimed
                        ? SummonAchievementRewardState.Claimed
                        : SummonAchievementRewardState.Normal
                });
            }

            return result;
        }

        private static string BuildRewardText(SummonRewardItem reward)
        {
            if (reward == null)
                return string.Empty;

            return "chưa có key";
        }
    }
}