using System.Linq;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public static class WeaponSummonAchievementRewardBuilder
    {
        public static SummonAchievementRewardListData BuildWeapon(
            WeaponSummonConfigSO config,
            WeaponSummonSaveData saveData,
            SummonRewardVisualConfigSO rewardVisualConfig)
        {
            var result = new SummonAchievementRewardListData
            {
                Tab = SummonAchievementTab.Weapon
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

                bool isClaimed = saveData != null &&
                                 saveData.ClaimedRewardLevels != null &&
                                 saveData.ClaimedRewardLevels.Contains(entry.SummonLevel);

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

        private static string BuildRewardText(SummonRewardItem reward, RewardVisualEntry visual)
        {
            if (reward == null)
                return string.Empty;

            string name = visual != null && !string.IsNullOrEmpty(visual.DisplayName)
                ? visual.DisplayName
                : reward.RewardType.ToString();

            return $"{name} x{reward.Amount}";
        }
    }
}