using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    [Serializable]
    public class HeroSummonSaveData
    {
        public int TotalRoll;
        public int SummonLevel = 1;
        public int PityMissCounter;
        public bool SkipGemFallbackConfirm;
        public List<int> ClaimedRewardLevels = new();
    }
}