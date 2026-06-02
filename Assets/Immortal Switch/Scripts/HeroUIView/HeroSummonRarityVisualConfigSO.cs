using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    [CreateAssetMenu(
        fileName = "HeroSummonRarityVisualConfig",
        menuName = "ScriptableObjects/Summon/HeroSummonRarityVisualConfig")]
    public class HeroSummonRarityVisualConfigSO : ScriptableObject
    {
        public List<HeroSummonRarityVisualEntry> Entries = new();

        public HeroSummonRarityVisualEntry Get(SummonRarity rarity)
        {
            return Entries.Find(x => x.Rarity == rarity);
        }

        public Sprite GetIcon(SummonRarity rarity)
        {
            var entry = Get(rarity);
            return entry != null ? entry.Icon : null;
        }

        public Color GetTopColor(SummonRarity rarity)
        {
            var entry = Get(rarity);
            return entry != null ? entry.TopColor : Color.white;
        }

        public Color GetBottomColor(SummonRarity rarity)
        {
            var entry = Get(rarity);
            return entry != null ? entry.BottomColor : Color.white;
        }
    }

    [Serializable]
    public class HeroSummonRarityVisualEntry
    {
        public SummonRarity Rarity;

        [Header("Icon")]
        [PreviewField] public Sprite Icon;

        [Header("Gradient")]
        public Color TopColor = Color.white;
        public Color BottomColor = Color.white;
    }
}