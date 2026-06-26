using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Hero;

namespace Immortal_Switch.Scripts.Equipment.Core
{
    public static class WeaponCurrencyHelper
    {
        public static CurrencyType GetClassStoneCurrency(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Archer:
                    return CurrencyType.ArcherWeaponTranscendenceStone;
                case HeroClass.Mage:
                    return CurrencyType.MageWeaponTranscendenceStone;
                case HeroClass.Warrior:
                    return CurrencyType.WarriorWeaponTranscendenceStone;
                case HeroClass.Assassin:
                    return CurrencyType.AssassinWeaponTranscendenceStone;
                default:
                    return CurrencyType.gold;
            }
        }
    }
}