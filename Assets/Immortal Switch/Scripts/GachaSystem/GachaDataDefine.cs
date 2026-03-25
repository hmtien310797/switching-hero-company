using System;
using System.Collections.Generic;
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
        HeroTicket,
        RandomEpicHero,
        RandomLegendaryHero,
        LegendaryShard,
        SelectLegendaryHero
    }

    [Serializable]
    public class HeroSummonRewardItem
    {
        public HeroSummonRewardType RewardType;
        [Min(0)] public int Amount;
        public string Description;
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
}