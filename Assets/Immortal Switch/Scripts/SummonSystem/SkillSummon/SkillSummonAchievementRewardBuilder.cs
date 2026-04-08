using System.Linq;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
{
    public static class SkillSummonAchievementRewardBuilder
    {
        public static SummonAchievementRewardListData BuildSkill(
            SkillSummonConfigSO config,
            SkillSummonSaveData saveData,
            SummonRewardVisualConfigSO rewardVisualConfig)
        {
            var result = new SummonAchievementRewardListData
            {
                Tab = SummonAchievementTab.Skill
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

        private static string BuildRewardText(SummonRewardItem reward, RewardVisualEntry visual)
        {
            if (reward == null)
                return string.Empty;

            switch (reward.RewardType)
            {
                case SummonRewardType.Currency:
                {
                    string name = visual != null && !string.IsNullOrEmpty(visual.DisplayName)
                        ? visual.DisplayName
                        : reward.CurrencyType.ToString();

                    return $"{name} x{reward.Amount}";
                }

                case SummonRewardType.RandomSkill:
                {
                    string name = visual != null && !string.IsNullOrEmpty(visual.DisplayName)
                        ? visual.DisplayName
                        : $"Random {reward.RandomSkillGrade} Skill";

                    return $"{name} x{reward.Amount}";
                }

                case SummonRewardType.Skill:
                {
                    string name = visual != null && !string.IsNullOrEmpty(visual.DisplayName)
                        ? visual.DisplayName
                        : $"Skill {reward.SkillId}";

                    return $"{name} x{reward.Amount}";
                }
            }

            return reward.Description;
        }
    }
}