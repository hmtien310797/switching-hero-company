namespace Immortal_Switch.Scripts.Equipment.Models
{
    public class WeaponFuseResult
    {
        public bool Success;

        public int SourceWeaponId;

        public bool UnlockedNewStandard;
        public bool AddedShardToExistingStandard;
        public int TargetStandardWeaponId;

        public bool UnlockedNewExclusive;
        public bool AddedShardToExistingExclusive;
        public int TargetExclusiveWeaponId;
        public int TargetHeroId;
    }
}