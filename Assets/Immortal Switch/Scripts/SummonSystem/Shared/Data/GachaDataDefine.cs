using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.Data
{
    public enum SummonPaymentType
    {
        Ticket,
        Gem
    }

    public enum HeroSummonPityMode
    {
        None,
        Soft,
        Hard,
        SoftHard
    }

    public enum SummonRewardType
    {
        Currency,
        Hero,
        Skill,
        RandomHero,
        RandomSkill
    }

    [Serializable]
    public class SummonRewardItem
    {
        [HorizontalGroup("Row", 60)]
        [HideLabel]
        public SummonRewardType RewardType;

        // ===== Currency =====
        [ShowIf(nameof(IsCurrency))]
        [LabelWidth(100)]
        public CurrencyType CurrencyType;

        // ===== Hero =====
        [ShowIf(nameof(IsHero))]
        [LabelWidth(100)]
        public string HeroId;

        [ShowIf(nameof(IsHero))]
        [LabelWidth(100)]
        public SummonRarity HeroRarity;

        // ===== Skill =====
        [ShowIf(nameof(IsSkill))]
        [LabelWidth(100)]
        public string SkillId;

        [ShowIf(nameof(IsSkill))]
        [LabelWidth(100)]
        public SkillSummonGrade SkillGrade;

        // ===== Random Hero =====
        [ShowIf(nameof(IsRandomHero))]
        [LabelText("Pool Rarity")]
        public SummonRarity RandomHeroRarity;

        [ShowIf(nameof(IsRandomHero))]
        [LabelText("Pool Id (optional)")]
        public string PoolId;

        // ===== Random Skill =====
        [ShowIf(nameof(IsRandomSkill))]
        [LabelText("Pool Grade")]
        public SkillSummonGrade RandomSkillGrade;

        [ShowIf(nameof(IsRandomSkill))]
        [LabelText("Pool Id (optional)")]
        public string SkillPoolId;

        // ===== Common =====
        [LabelWidth(100)]
        public int Amount;

        public string Description;

        private bool IsCurrency() => RewardType == SummonRewardType.Currency;
        private bool IsHero() => RewardType == SummonRewardType.Hero;
        private bool IsSkill() => RewardType == SummonRewardType.Skill;
        private bool IsRandomHero() => RewardType == SummonRewardType.RandomHero;
        private bool IsRandomSkill() => RewardType == SummonRewardType.RandomSkill;
    }

    [Serializable]
    public class HeroSummonResultEntry
    {
        public int RollIndex;
        public UnityEngine.Object HeroAsset;
        public string HeroName;
        public bool IsNewHero;
        public int ShardGained;
        public bool IsPityHit;
        public SummonRarity Rarity;
    }

    [Serializable]
    public class HeroSummonResult
    {
        public SummonPaymentType PaymentType;
        public int PaidAmount;

        public int OldTotalRoll;
        public int NewTotalRoll;
        public int OldSummonLevel;
        public int NewSummonLevel;

        public List<int> NewlyUnlockedRewardLevels = new();
        public List<HeroSummonResultEntry> Entries = new();
    }

    [Serializable]
    public class SummonRewardPreviewData
    {
        public int SummonLevel;
        public SummonRewardItem RewardItem;
        public bool IsClaimable;
        public bool IsClaimed;
    }
}