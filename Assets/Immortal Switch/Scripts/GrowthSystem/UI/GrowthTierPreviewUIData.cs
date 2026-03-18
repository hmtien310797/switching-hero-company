using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public struct GrowthTierPreviewUIData
    {
        public int CurrentTier;
        public int NextTier;

        public Sprite CurrentTierIcon;
        public Sprite NextTierIcon;

        public string CurrentTierText;
        public string NextTierText;

        public bool CanUpgradeTier;
        public float CurrentTierProgressNormalized;
    }
}