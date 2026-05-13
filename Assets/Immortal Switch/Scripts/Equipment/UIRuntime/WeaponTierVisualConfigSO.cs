using System;
using System.Collections.Generic;
using UnityEngine;
using Battle;
using Immortal_Switch.Scripts.Equipment.Core;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    [CreateAssetMenu(fileName = "WeaponTierVisualConfig", menuName = "ScriptableObjects/Equipment/WeaponTierVisualConfig")]
    public class WeaponTierVisualConfigSO : ScriptableObject
    {
        public List<WeaponTierVisualEntry> Entries = new();

        public WeaponTierVisualEntry Get(WeaponTier tier)
        {
            return Entries.Find(x => x.Tier == tier);
        }
    }

    [Serializable]
    public class WeaponTierVisualEntry
    {
        public WeaponTier Tier;
        public Color GlowColor;
        public Sprite TierLabelSprite;
        public Sprite TierBackgroundSprite;
    }
}