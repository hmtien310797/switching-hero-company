using System;

namespace Immortal_Switch.Scripts.Equipment.Models
{
    [Serializable]
    public class StandardWeaponState
    {
        public int WeaponId;
        public bool IsUnlocked;
        public int Level = 1;
        public int LimitBreakStage = 0;
        public int CurrentShard = 0;
    }
}