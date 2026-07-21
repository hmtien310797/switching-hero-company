using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
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

        public List<StatTierViewData> Build(BigNumber gold, int amount)
        {
            var result = new List<StatTierViewData>();

            var stats = service.GetStatsUnlockedExactlyAtTier(service.CurrentUnlockedTier);

            foreach (var stat in stats)
            {
                result.Add(BuildOne(stat, gold, amount));
            }

            return result;
        }

        private StatTierViewData BuildOne(StatType stat, BigNumber gold, int amount)
        {
            var ui = uiDb.Get(stat);

            int cur = service.GetCurrentStack(stat);
            int max = service.GetMaxAvailableStack(stat);
            int cost = service.GetUpgradeCost(stat, amount);
            int afford = service.GetAffordableUpgradeAmount(stat, amount, gold);

            bool isMax = service.IsMaxed(stat);
            bool can = gold >= cost && !isMax;

            return new StatTierViewData
            {
                Stat = stat,
                Icon = ui.Icon,
                Name = ui.DisplayName,
                StatProgressPercent = max > 0 ? (float)cur / max : 0,
                StatCurrentStack = cur,
                StatMaxStack = max,
                ValuePay = isMax ? "MAX" : cost.ToString("N0"),
                IsMax = isMax,
                CanUpgrade = can
            };
        }
    }
}