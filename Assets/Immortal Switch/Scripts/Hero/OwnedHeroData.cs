using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Hero
{
    [Serializable]
    public class OwnedHeroData
    {
        public int HeroId;
        public bool IsUnlocked;
        public HeroProgressTier CurrentTier;
        public int CurrentStarInTier;
        public int CurrentShard;
    }

    [Serializable]
    public class HeroCollectionSaveData
    {
        public List<OwnedHeroData> OwnedHeroes = new();
    }
}