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

            if (config == null || config.SummonLevels == null)
                return result;

            // Reward source is each level's own ItemId/ItemQuantity (Hero_Levels), not the
            // older LevelRewards (Hero_Rewards) table — the two described the same milestone
            // payout and Hero_Levels is now the authoritative one, matching the server.
            var sortedLevels = config.SummonLevels
                .Where(x => x != null)
                .OrderBy(x => x.SummonLevel)
                .ToList();

            for (int i = 0; i < sortedLevels.Count; i++)
            {
                var entry = sortedLevels[i];
                if (entry.ItemId <= 0 || entry.ItemQuantity <= 0)
                    continue;

                bool isClaimed = saveData != null && saveData.ClaimedRewardLevels.Contains(entry.SummonLevel);

                result.Items.Add(new SummonAchievementRewardItemData
                {
                    Level = entry.SummonLevel,
                    Title = $"Summon Level {entry.SummonLevel}",
                    RewardText = BuildRewardText(entry.ItemId, entry.ItemQuantity),
                    RewardIcon = DatabaseManager.Instance.ItemDb.LoadIconByItemId(entry.ItemId),
                    State = isClaimed
                        ? SummonAchievementRewardState.Claimed
                        : SummonAchievementRewardState.Normal
                });
            }

            return result;
        }

        private static string BuildRewardText(int itemId, int quantity)
        {
            var item = DatabaseManager.Instance.ItemDb.FindItem(itemId);
            var name = item != null ? item.itemName : string.Empty;
            return $"{name} x{quantity}";
        }
    }
}