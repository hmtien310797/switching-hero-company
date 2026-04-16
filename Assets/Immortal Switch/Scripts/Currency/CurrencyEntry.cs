using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Currency
{
    public enum CurrencyType
    {
        Gold,
        Diamond,
        HeroTicket,
        WeaponTicket,
        SkillTicket,
        WeaponEnhancementStone,
        WeaponBreakThroughStone,
        ArcherWeaponTranscendenceStone,
        MageWeaponTranscendenceStone,
        WarriorWeaponTranscendenceStone,
        AssassinWeaponTranscendenceStone,
    }
    
    [Serializable]
    public class CurrencyEntry
    {
        public CurrencyType CurrencyType;
        public int Amount;
    }

    [Serializable]
    public class CurrencySaveData
    {
        public List<CurrencyEntry> Entries = new();
    }
    
    public class CurrencyChangedArgs
    {
        public CurrencyType CurrencyType;
        public int OldAmount;
        public int NewAmount;
        public int Delta;
    }
}