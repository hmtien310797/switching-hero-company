using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Skill
{
    [Serializable]
    public class SkillSummonSaveData
    {
        public int TotalRoll;
        public bool SkipGemFallbackConfirm;
        public List<int> ClaimedRewardLevels = new();
    }
}