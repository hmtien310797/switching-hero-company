using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUpgradePanelBinder
    {
        private readonly GrowthSystemService service;
        private readonly GrowthStatUIViewDatabaseSO uiDb;

        public GrowthUpgradePanelBinder(
            GrowthSystemService service,
            GrowthStatUIViewDatabaseSO uiDb)
        {
            this.service = service;
            this.uiDb = uiDb;
        }

        public GrowthUpgradePanelData Build(int gold, int amount)
        {
            var result = new GrowthUpgradePanelData
            {
                Rows = new List<StatTierViewData>()
            };

            int tier = service.CurrentUnlockedTier;
            var stats = service.GetStatsUnlockedExactlyAtTier(tier);

            foreach (var stat in stats)
            {
                result.Rows.Add(BuildOne(stat, gold, amount));
            }

            result.CurrentTier = tier;
            result.CompletedStatCount = service.GetTierCompletedStatCount(tier);
            result.TotalStatCount = service.GetTierTotalStatCount(tier);
            result.TierProgressPercent = service.GetTierCompletionPercent(tier);

            return result;
        }

        private StatTierViewData BuildOne(StatType stat, int gold, int amount)
        {
            var ui = uiDb.Get(stat);

            int cur = service.GetCurrentStack(stat);
            int max = service.GetMaxAvailableStack(stat);
            int afford = service.GetAffordableUpgradeAmount(stat, amount, gold);
            int cost = service.GetUpgradeCost(stat, afford);

            bool isMax = service.IsMaxed(stat);
            bool can = afford > 0 && !isMax;

            return new StatTierViewData
            {
                Stat = stat,
                Icon = ui.Icon,
                Name = ui.DisplayName,
                StatProgressPercent = max > 0 ? (float)cur / max : 0f,
                StatCurrentStack = cur,
                StatMaxStack = max,
                ValuePay = isMax ? "MAX" : cost.ToString("N0"),
                IsMax = isMax,
                CanUpgrade = can
            };
        }
    }

    public struct GrowthUpgradePanelData
    {
        public int CurrentTier;
        public int CompletedStatCount;
        public int TotalStatCount;
        public float TierProgressPercent;
        public List<StatTierViewData> Rows;
    }
}