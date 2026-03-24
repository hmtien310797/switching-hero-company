using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts;

namespace Immortal_Switch.Hero
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