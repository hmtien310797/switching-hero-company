using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    [Serializable]
    public class WeaponSummonSaveData
    {
        public int TotalRoll;
        public bool SkipGemFallbackConfirm;
        public List<int> ClaimedRewardLevels = new();
    }
}