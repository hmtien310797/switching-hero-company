using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Currency;

namespace Immortal_Switch.Scripts.Skill
{
    public enum SkillSummonPaymentType
    {
        Ticket,
        Gem
    }

    public enum SkillSummonGrade
    {
        B,
        A,
        S,
        SS
    }

    public enum SkillSummonRewardType
    {
        Currency,
        RandomSkill
    }

    [Serializable]
    public class SkillSummonRewardItem
    {
        public SkillSummonRewardType RewardType;

        public CurrencyType CurrencyType;
        public SkillSummonGrade RandomSkillGrade;

        public int Amount;
        public string Description;
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
        public SkillSummonPaymentType PaymentType;
        public int PaidAmount;

        public int OldTotalRoll;
        public int NewTotalRoll;
        public int OldSummonLevel;
        public int NewSummonLevel;

        public List<int> NewlyUnlockedRewardLevels = new();
        public List<SkillSummonResultEntry> Entries = new();
    }

    [Serializable]
    public class SkillSummonRewardPreviewData
    {
        public int SummonLevel;
        public SkillSummonRewardItem RewardItem;
        public bool IsClaimable;
        public bool IsClaimed;
    }
}