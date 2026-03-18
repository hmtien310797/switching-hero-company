using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUIBinder
    {
        private readonly GrowthSystemService growthService;
        private readonly GrowthTierVisualDatabaseSO tierVisualDb;
        private readonly GrowthStatVisualDatabaseSO statVisualDb;

        public GrowthUIBinder(
            GrowthSystemService growthService,
            GrowthTierVisualDatabaseSO tierVisualDb,
            GrowthStatVisualDatabaseSO statVisualDb)
        {
            this.growthService = growthService;
            this.tierVisualDb = tierVisualDb;
            this.statVisualDb = statVisualDb;
        }

        public GrowthTierPreviewUIData BuildTierPreviewData()
        {
            int currentTier = growthService.CurrentUnlockedTier;
            int nextTier = currentTier + 1;

            return new GrowthTierPreviewUIData
            {
                CurrentTier = currentTier,
                NextTier = nextTier,
                CurrentTierIcon = tierVisualDb != null ? tierVisualDb.GetIconByTier(currentTier) : null,
                NextTierIcon = tierVisualDb != null ? tierVisualDb.GetIconByTier(nextTier) : null,
                CurrentTierText = $"Tier {currentTier}",
                NextTierText = $"Tier {nextTier}",
                CanUpgradeTier = true,
                CurrentTierProgressNormalized = 0f
            };
        }

        public GrowthUpgradePanelUIData BuildUpgradePanelData(int playerGold, int selectedUpgradeAmount)
        {
            int currentTier = growthService.CurrentUnlockedTier;
            var rows = new List<GrowthStatRowUIData>();

            var statsInTier = growthService.GetStatsUnlockedExactlyAtTier(currentTier);
            for (int i = 0; i < statsInTier.Count; i++)
            {
                var stat = statsInTier[i];
                rows.Add(BuildStatRowData(stat, playerGold, selectedUpgradeAmount));
            }

            return new GrowthUpgradePanelUIData
            {
                CurrentTier = currentTier,
                TierIcon = tierVisualDb != null ? tierVisualDb.GetIconByTier(currentTier) : null,
                TierText = $"Tier {currentTier}",
                PlayerGold = playerGold,
                PlayerGoldText = playerGold.ToString("N0"),
                SelectedUpgradeAmount = selectedUpgradeAmount,
                TierProgressNormalized = GetTierProgressNormalized(currentTier),
                TierProgressText = GetTierProgressText(currentTier),
                ShowMaxButton = true,
                Rows = rows
            };
        }

        public GrowthStatRowUIData BuildStatRowData(StatType stat, int playerGold, int selectedUpgradeAmount)
        {
            var visual = statVisualDb != null ? statVisualDb.GetEntry(stat) : default;

            int currentStack = growthService.GetCurrentStack(stat);
            int maxStack = growthService.GetMaxAvailableStack(stat);

            float currentValue = growthService.GetTotalGrowthValue(stat);
            float previewValue = growthService.GetNextLevelValuePreview(stat, selectedUpgradeAmount);

            int affordableAmount = growthService.GetAffordableUpgradeAmount(stat, selectedUpgradeAmount, playerGold);
            int realCost = growthService.GetUpgradeCost(stat, affordableAmount);

            bool isMaxed = growthService.IsMaxed(stat);

            return new GrowthStatRowUIData
            {
                Stat = stat,
                StatIcon = visual.Icon,
                StatName = string.IsNullOrEmpty(visual.DisplayName) ? stat.ToString() : visual.DisplayName,

                CurrentStack = currentStack,
                MaxStack = maxStack,
                StackText = $"{currentStack} / {maxStack}",

                ProgressNormalized = maxStack > 0 ? (float)currentStack / maxStack : 0f,

                CurrentValue = currentValue,
                CurrentValueText = GrowthHelper.GetStatDisplayValue(stat, currentValue),

                PreviewValue = previewValue,
                PreviewValueText = GrowthHelper.GetStatDisplayValue(stat, previewValue),

                UpgradeAmount = affordableAmount,
                UpgradeCost = realCost,

                CanUpgrade = affordableAmount > 0 && realCost > 0 && !isMaxed,
                IsMaxed = isMaxed
            };
        }

        private float GetTierProgressNormalized(int tier)
        {
            var stats = growthService.GetStatsUnlockedExactlyAtTier(tier);
            if (stats.Count == 0)
                return 0f;

            float total = 0f;

            for (int i = 0; i < stats.Count; i++)
            {
                int current = growthService.GetCurrentStack(stats[i]);
                int max = growthService.GetMaxAvailableStack(stats[i]);
                if (max <= 0) continue;

                total += (float)current / max;
            }

            return total / stats.Count;
        }

        private string GetTierProgressText(int tier)
        {
            var stats = growthService.GetStatsUnlockedExactlyAtTier(tier);
            if (stats.Count == 0)
                return "0 / 0";

            int currentSum = 0;
            int maxSum = 0;

            for (int i = 0; i < stats.Count; i++)
            {
                currentSum += growthService.GetCurrentStack(stats[i]);
                maxSum += growthService.GetMaxAvailableStack(stats[i]);
            }

            return $"{currentSum} / {maxSum}";
        }
    }
}