using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public struct GrowthStatRowUIData
    {
        public StatType Stat;
        public Sprite StatIcon;
        public string StatName;

        public int CurrentStack;
        public int MaxStack;
        public string StackText;

        public float ProgressNormalized;

        public float CurrentValue;
        public string CurrentValueText;

        public float PreviewValue;
        public string PreviewValueText;

        public int UpgradeAmount;
        public int UpgradeCost;

        public bool CanUpgrade;
        public bool IsMaxed;
    }
}