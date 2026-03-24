using System;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    [CreateAssetMenu(fileName = "HeroRarityVisualConfig", menuName = "ScriptableObjects/UI/HeroRarityVisualConfig")]
    public class HeroRarityVisualConfigSO : ScriptableObject
    {
        public List<HeroRarityVisualEntry> Entries = new();

        public Sprite GetIcon(HeroProgressTier tier)
        {
            var entry = Entries.Find(x => x.Tier == tier);
            return entry != null ? entry.Icon : null;
        }
    }

    [Serializable]
    public class HeroRarityVisualEntry
    {
        public HeroProgressTier Tier;
        public Sprite Icon;
    }
}