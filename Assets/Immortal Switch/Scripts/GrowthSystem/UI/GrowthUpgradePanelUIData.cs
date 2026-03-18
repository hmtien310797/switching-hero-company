using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public struct GrowthUpgradePanelUIData
    {
        public int CurrentTier;
        public Sprite TierIcon;
        public string TierText;

        public int PlayerGold;
        public string PlayerGoldText;

        public int SelectedUpgradeAmount;

        public float TierProgressNormalized;
        public string TierProgressText;

        public bool ShowMaxButton;

        public List<GrowthStatRowUIData> Rows;
    }
}