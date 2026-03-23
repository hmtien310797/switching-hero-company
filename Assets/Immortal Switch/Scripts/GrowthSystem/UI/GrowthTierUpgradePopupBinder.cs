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

            for (int i = 0; i < nextTierStats.Count; i++)
            {
                var stat = nextTierStats[i];

                if (!service.TryGetStatGrowthAtTier(nextTier, stat, out _))
                    continue;

                var ui = uiDb.Get(stat);

                bool hasCurrentTierVersion = service.TryGetStatGrowthAtTier(currentTier, stat, out _);
                bool hasAnyPreviousVersion = service.TryGetPreviousKnownStatGrowthBeforeTier(nextTier, stat, out int previousTier, out _);

                var row = new GrowthTierUpgradeRowData
                {
                    Stat = stat,
                    StatName = string.IsNullOrEmpty(ui.DisplayName) ? stat.ToString() : ui.DisplayName,
                    StatIcon = ui.Icon,
                    ShowArrow = false,
                    LeftValueText = string.Empty,
                    RightValueText = string.Empty
                };

                if (hasCurrentTierVersion)
                {
                    row.ShowArrow = true;

                    float left = GetTotalValueUpToTier(currentTier, stat);
                    float right = GetTotalValueUpToTier(nextTier, stat);

                    row.LeftValueText = FormatValue(stat, left);
                    row.RightValueText = FormatValue(stat, right);
                }
                else if (hasAnyPreviousVersion)
                {
                    row.ShowArrow = true;

                    float left = GetTotalValueUpToTier(previousTier, stat);
                    float right = GetTotalValueUpToTier(nextTier, stat);

                    row.LeftValueText = FormatValue(stat, left);
                    row.RightValueText = FormatValue(stat, right);
                }
                else
                {
                    row.ShowArrow = false;

                    float value = GetTotalValueUpToTier(nextTier, stat);
                    row.LeftValueText = FormatValue(stat, value);
                    row.RightValueText = string.Empty;
                }

                data.Rows.Add(row);
            }

            return data;
        }

        private string FormatValue(StatType stat, float rawValue)
        {
            float runtimeValue = service.ConvertRawValueToRuntime(stat, rawValue);
            return service.FormatForDisplay(stat, runtimeValue);
        }

        private float GetTotalValueUpToTier(int targetTier, StatType stat)
        {
            float totalRawValue = 0f;
            int previousEndStack = 0;

            for (int tier = 1; tier <= targetTier; tier++)
            {
                if (!service.TryGetStatGrowthAtTier(tier, stat, out var growth))
                    continue;

                var tierData = service.GetTierData(tier);
                if (tierData == null)
                    continue;

                int segmentStart = previousEndStack + 1;
                int segmentEnd = tierData.MaxStack;

                if (segmentEnd < segmentStart)
                    continue;

                int stackCountThisSegment = segmentEnd - segmentStart + 1;
                totalRawValue += stackCountThisSegment * growth.ValuePerLevel;

                previousEndStack = segmentEnd;
            }

            return totalRawValue;
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