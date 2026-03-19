using System.Collections.Generic;
using UnityEngine;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthDebugMonitor : MonoBehaviour
    {
        [SerializeField] private GrowthManager growthManager;

        [Header("Snapshot")]
        [SerializeField] private int currentTier;
        [SerializeField] private int gold;
        [SerializeField] private List<GrowthDebugEntry> entries = new();

        private void Awake()
        {
            if (growthManager == null)
                growthManager = GrowthManager.Instance;
        }

        private void OnEnable()
        {
            if (growthManager == null)
                growthManager = GrowthManager.Instance;

            if (growthManager != null)
            {
                growthManager.OnGrowthChanged += Refresh;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (growthManager != null)
                growthManager.OnGrowthChanged -= Refresh;
        }

        [ContextMenu("Refresh Debug Snapshot")]
        public void Refresh()
        {
            if (growthManager == null || growthManager.Service == null)
                return;

            currentTier = growthManager.SaveData.CurrentUnlockedTier;
            gold = growthManager.PlayerGold;

            entries.Clear();

            var stats = growthManager.Service.GetAllUnlockedStatsUpToCurrentTier();

            foreach (var stat in stats)
            {
                entries.Add(new GrowthDebugEntry
                {
                    Stat = stat,
                    CurrentStack = growthManager.Service.GetCurrentStack(stat),
                    MaxStack = growthManager.Service.GetMaxAvailableStack(stat),

                    CostX1 = growthManager.Service.GetUpgradeCost(stat, 1),
                    CostX10 = growthManager.Service.GetUpgradeCost(stat, 10),
                    CostX100 = growthManager.Service.GetUpgradeCost(stat, 100),

                    CanBuyX1 = growthManager.Service.GetAffordableUpgradeAmount(stat, 1, gold),
                    CanBuyX10 = growthManager.Service.GetAffordableUpgradeAmount(stat, 10, gold),
                    CanBuyX100 = growthManager.Service.GetAffordableUpgradeAmount(stat, 100, gold),

                    IsMaxed = growthManager.Service.IsMaxed(stat)
                });
            }
        }
    }
}