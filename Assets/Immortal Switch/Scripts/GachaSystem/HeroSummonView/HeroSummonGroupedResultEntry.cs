using Immortal_Switch.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonGroupedResultEntry
    {
        public Object HeroAsset;
        public string HeroName;
        public int Count;
        public bool IsNewHero;
        public int TotalShardGained;
        public bool HasPityHit;
        public SummonRarity Rarity;
    }
}