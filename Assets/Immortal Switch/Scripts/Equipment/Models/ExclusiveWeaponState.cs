using System;

namespace Immortal_Switch.Scripts.Equipment.Models
{
    [Serializable]
    public class ExclusiveWeaponState
    {
        public int ExclusiveWeaponId;
        public int HeroId;
        public bool IsUnlocked;
        public int Level = 1;
        public int LimitBreakStage = 0;
        public int CurrentShard = 0;

        public int CurrentStar = 1;

        // Phase sau
        public int TranscendLevel = 0;
    }
}