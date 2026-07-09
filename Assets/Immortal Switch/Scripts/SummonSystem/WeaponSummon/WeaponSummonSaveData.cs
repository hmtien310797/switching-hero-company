using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    [Serializable]
    public class WeaponSummonSaveData
    {
        public int TotalRoll;
        public int SummonLevel = 1;
        public bool SkipGemFallbackConfirm;
        public List<int> ClaimedRewardLevels = new();
    }
}