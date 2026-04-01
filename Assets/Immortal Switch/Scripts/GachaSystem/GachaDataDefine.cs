using System;
using System.Collections.Generic;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.Currency;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem
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

    public enum HeroSummonRewardType
    {
        Currency,
        Hero,
        RandomHero
    }

    [System.Serializable]
    public class HeroSummonRewardItem
    {
        [HorizontalGroup("Row", 60)]
        [HideLabel]
        public HeroSummonRewardType RewardType;

        // ===== Currency =====
        [ShowIf(nameof(IsCurrency))]
        [LabelWidth(80)]
        public CurrencyType CurrencyType;

        // ===== Hero =====
        [ShowIf(nameof(IsHero))]
        [LabelWidth(80)]
        public string HeroId;

        [ShowIf(nameof(IsHero))]
        public SummonRarity HeroRarity;

        // ===== Random Hero =====
        [ShowIf(nameof(IsRandomHero))]
        [LabelText("Pool Rarity")]
        public SummonRarity RandomHeroRarity;

        [ShowIf(nameof(IsRandomHero))]
        [LabelText("Pool Id (optional)")]
        public string PoolId;

        // ===== Common =====
        [LabelWidth(80)]
        public int Amount;

        // ===== Condition =====
        private bool IsCurrency() => RewardType == HeroSummonRewardType.Currency;
        private bool IsHero() => RewardType == HeroSummonRewardType.Hero;
        private bool IsRandomHero() => RewardType == HeroSummonRewardType.RandomHero;
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
    public class HeroSummonRewardPreviewData
    {
        public int SummonLevel;
        public HeroSummonRewardItem RewardItem;
        public bool IsClaimable;
        public bool IsClaimed;
    }
}