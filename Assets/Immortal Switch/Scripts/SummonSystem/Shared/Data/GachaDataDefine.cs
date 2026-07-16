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
        Weapon,
        RandomHero,
        RandomSkill
    }

    [Serializable]
    public class SummonRewardItem
    {
        public int ItemId;
        public int Amount;
    }

    [Serializable]
    public class HeroSummonResultEntry
    {
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
        public int Quantity;
        public int ItemId;
        public bool IsClaimable;
        public bool IsClaimed;
    }
}