using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Shared.Database;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    public enum WeaponSummonPaymentType
    {
        Ticket,
        Gem
    }

    [Serializable]
    public class WeaponSummonResultEntry
    {
        public int RollIndex;
        public StandardWeaponDefinitionSO Weapon;
        public int WeaponId;
        public string WeaponName;
        public Sprite Icon;
        public WeaponTier Tier;
        public int Star;
        public bool IsNewWeapon;
        public int ShardGained;
        public int TotalShardAfter;

        // thong tin tier,
        public ItemTierEntry TierInfo;
    }

    [Serializable]
    public class WeaponSummonResult
    {
        public WeaponSummonPaymentType PaymentType;
        public int PaidAmount;

        public int OldTotalRoll;
        public int NewTotalRoll;
        public int OldSummonLevel;
        public int NewSummonLevel;

        public List<int> NewlyUnlockedRewardLevels = new();
        public List<WeaponSummonResultEntry> Entries = new();
    }
}