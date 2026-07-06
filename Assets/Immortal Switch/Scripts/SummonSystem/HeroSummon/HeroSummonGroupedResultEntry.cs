using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
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