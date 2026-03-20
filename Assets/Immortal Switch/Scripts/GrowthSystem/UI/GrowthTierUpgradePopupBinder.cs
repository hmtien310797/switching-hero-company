using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthTierUpgradePopupBinder
    {
        private readonly GrowthSystemService service;
        private readonly GrowthStatUIViewDatabaseSO uiDb;

        public GrowthTierUpgradePopupBinder(
            GrowthSystemService service,
            GrowthStatUIViewDatabaseSO uiDb)
        {
            this.service = service;
            this.uiDb = uiDb;
        }

        public GrowthTierUpgradePopupData Build(int currentTier, int nextTier, Sprite currentTierIcon, Sprite nextTierIcon)
        {
            var data = new GrowthTierUpgradePopupData
            {
                CurrentTier = currentTier,
                NextTier = nextTier,
                CurrentTierIcon = currentTierIcon,
                NextTierIcon = nextTierIcon,
                Rows = new List<GrowthTierUpgradeRowData>()
            };

            var nextTierStats = service.GetStatsUnlockedExactlyAtTier(nextTier);
            var currentTierStats = service.GetStatsUnlockedExactlyAtTier(currentTier);

            for (int i = 0; i < nextTierStats.Count; i++)
            {
                var stat = nextTierStats[i];

                if (!service.TryGetStatGrowthAtTier(nextTier, stat, out var nextGrowth))
                    continue;

                var ui = uiDb.Get(stat);

                bool hasCurrentTierVersion = service.TryGetStatGrowthAtTier(currentTier, stat, out var currentTierGrowth);
                bool hasAnyPreviousVersion = service.TryGetPreviousKnownStatGrowthBeforeTier(nextTier, stat, out int previousTier, out var previousGrowth);

                var row = new GrowthTierUpgradeRowData
                {
                    Stat = stat,
                    StatName = string.IsNullOrEmpty(ui.DisplayName) ? stat.ToString() : ui.DisplayName,
                    StatIcon = ui.Icon,
                    ShowArrow = false,
                    LeftValueText = string.Empty,
                    RightValueText = string.Empty
                };

                // Rule 1
                if (hasCurrentTierVersion)
                {
                    row.ShowArrow = true;

                    float left = GetTotalValueOfTier(currentTier, stat);
                    float right = GetTotalValueOfTier(nextTier, stat);

                    row.LeftValueText = FormatValue(left);
                    row.RightValueText = FormatValue(right);
                }
// Rule 4
                else if (hasAnyPreviousVersion)
                {
                    row.ShowArrow = true;

                    float left = GetTotalValueOfTier(previousTier, stat);
                    float right = GetTotalValueOfTier(nextTier, stat);

                    row.LeftValueText = FormatValue(left);
                    row.RightValueText = FormatValue(right);
                }
// Rule 2
                else
                {
                    row.ShowArrow = false;

                    float value = GetTotalValueOfTier(nextTier, stat);
                    row.LeftValueText = FormatValue(value);
                    row.RightValueText = string.Empty;
                }

                data.Rows.Add(row);
            }

            // Rule 3 tự nhiên được xử lý:
            // stat chỉ có ở tier cũ mà không có ở tier mới sẽ không loop vào nextTierStats nên không hiện.

            return data;
        }

        private static string FormatValue(float value)
        {
            return $"{value:0.##}%";
        }
        
        private float GetTotalValueOfTier(int tier, StatType stat)
        {
            if (!service.TryGetStatGrowthAtTier(tier, stat, out var growth))
                return 0f;

            // Lấy max stack của tier
            var tierData = service.GetTierData(tier);
            if (tierData == null)
                return 0f;

            return growth.ValuePerLevel * tierData.MaxStack;
        }
    }

    public class GrowthTierUpgradePopupData
    {
        public int CurrentTier;
        public int NextTier;
        public Sprite CurrentTierIcon;
        public Sprite NextTierIcon;
        public List<GrowthTierUpgradeRowData> Rows;
    }

    public struct GrowthTierUpgradeRowData
    {
        public StatType Stat;
        public string StatName;
        public Sprite StatIcon;
        public bool ShowArrow;
        public string LeftValueText;
        public string RightValueText;
    }
}