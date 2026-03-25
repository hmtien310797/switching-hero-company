using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.GachaSystem
{
    [Serializable]
    public class HeroSummonSaveData
    {
        public int TotalRoll;
        public int PityMissCounter;
        public bool SkipGemFallbackConfirm;
        public List<int> ClaimedRewardLevels = new();
    }
}