using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.Models;

namespace Immortal_Switch.Scripts.Equipment.Core
{
    [Serializable]
    public class WeaponSaveData
    {
        public List<StandardWeaponState> StandardWeapons = new();
        public List<ExclusiveWeaponState> ExclusiveWeapons = new();
        public List<HeroWeaponEquipEntry> HeroEquips = new();
    }
}