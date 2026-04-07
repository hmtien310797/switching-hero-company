using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.GachaSystem;

namespace Immortal_Switch.Scripts.Skill
{
    public enum SkillSummonGrade
    {
        B,
        A,
        S,
        SS
    }

    [Serializable]
    public class SkillSummonResultEntry
    {
        public int RollIndex;
        public SkillDataSO SkillAsset;
        public int SkillId;
        public string SkillName;
        public SkillSummonGrade Grade;
        public bool IsNewSkill;
        public int ShardGained;
    }

    [Serializable]
    public class SkillSummonResult
    {
        public SummonPaymentType PaymentType;
        public int PaidAmount;

        public int OldTotalRoll;
        public int NewTotalRoll;
        public int OldSummonLevel;
        public int NewSummonLevel;

        public List<int> NewlyUnlockedRewardLevels = new();
        public List<SkillSummonResultEntry> Entries = new();
    }
}