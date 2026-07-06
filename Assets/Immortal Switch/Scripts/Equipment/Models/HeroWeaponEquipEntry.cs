using System;

namespace Immortal_Switch.Scripts.Equipment.Models
{
    [Serializable]
    public class HeroWeaponEquipEntry
    {
        public int HeroId;
        public int EquippedStandardWeaponId;
        public int EquippedExclusiveWeaponId;
        public bool UseExclusive;
    }
}