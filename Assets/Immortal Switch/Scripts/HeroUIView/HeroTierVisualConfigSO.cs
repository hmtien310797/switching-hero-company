using System;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    [CreateAssetMenu(fileName = "HeroTierVisualConfig", menuName = "ScriptableObjects/UI/HeroTierVisualConfig")]
    public class HeroTierVisualConfigSO : ScriptableObject
    {
        public TierVisualMode Mode;

        public List<TierVisualEntry> Entries = new();

        public TierVisualEntry Get(HeroProgressTier tier)
        {
            return Entries.Find(x => x.Tier == tier);
        }
    }

    [Serializable]
    public class TierVisualEntry
    {
        public HeroProgressTier Tier;

        [Header("Gradient")]
        public Color TopColor;
        public Color BottomColor;

        [Header("Sprite")]
        public Sprite BackgroundSprite;
    }
    
    public enum TierVisualMode
    {
        Gradient,
        Sprite
    }
}