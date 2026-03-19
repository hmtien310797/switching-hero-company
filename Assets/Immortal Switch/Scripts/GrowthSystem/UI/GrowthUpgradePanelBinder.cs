using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUpgradePanelBinder
    {
        private readonly GrowthSystemService growthService;
        private readonly GrowthStatUIViewDatabaseSO statUiDatabase;

        public GrowthUpgradePanelBinder(
            GrowthSystemService growthService,
            GrowthStatUIViewDatabaseSO statUiDatabase)
        {
            this.growthService = growthService;
            this.statUiDatabase = statUiDatabase;
        }

        public List<StatTierViewData> Build(int playerGold, int selectedUpgradeAmount)
        {
            var result = new List<StatTierViewData>();

            int currentTier = growthService.CurrentUnlockedTier;
            var stats = growthService.GetStatsUnlockedExactlyAtTier(currentTier);

            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                result.Add(BuildOne(stat, playerGold, selectedUpgradeAmount));
            }

            return result;
        }

        private StatTierViewData BuildOne(StatType stat, int playerGold, int selectedUpgradeAmount)
        {
            var uiData = statUiDatabase != null ? statUiDatabase.Get(stat) : default;

            int currentStack = growthService.GetCurrentStack(stat);
            int maxStack = growthService.GetMaxAvailableStack(stat);
            int affordableAmount = growthService.GetAffordableUpgradeAmount(stat, selectedUpgradeAmount, playerGold);
            int upgradeCost = growthService.GetUpgradeCost(stat, affordableAmount);
            bool isMax = growthService.IsMaxed(stat);

            return new StatTierViewData
            {
                Stat = stat,
                Icon = uiData.Icon,
                Name = string.IsNullOrEmpty(uiData.DisplayName) ? stat.ToString() : uiData.DisplayName,
                StatProgressPercent = maxStack > 0 ? (float)currentStack / maxStack : 0f,
                StatCurrentStack = currentStack,
                StatMaxStack = maxStack,
                ValuePay = isMax ? "MAX" : upgradeCost.ToString("N0"),
                IsMax = isMax
            };
        }
    }
}