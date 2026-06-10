using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;

namespace Immortal_Switch.Scripts.Currency
{
    public enum CurrencyType
    {
        none = 0,
        gold,
        diamond,
        energy,
        arena_token,
        guild_coin,
        event_token,
        crystal,
        
        HeroTicket,
        WeaponTicket,
        SkillTicket,
        WeaponEnhancementStone,
        WeaponBreakThroughStone,
        ArcherWeaponTranscendenceStone,
        MageWeaponTranscendenceStone,
        WarriorWeaponTranscendenceStone,
        AssassinWeaponTranscendenceStone,
        exp
    }
    
    [Serializable]
    public class CurrencyEntry
    {
        public CurrencyType CurrencyType;
        public BigNumber Amount;
    }

    [Serializable]
    public class CurrencySaveData
    {
        public List<CurrencyEntry> Entries = new();
    }
    
    public class CurrencyChangedArgs
    {
        public CurrencyType CurrencyType;
        public BigNumber OldAmount;
        public BigNumber NewAmount;
        public BigNumber Delta;
    }
}