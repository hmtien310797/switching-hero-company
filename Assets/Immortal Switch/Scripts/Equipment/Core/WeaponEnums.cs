namespace Immortal_Switch.Scripts.Equipment.Core
{
    public enum WeaponCategory
    {
        Standard = 0,
        Exclusive = 1
    }

    public enum WeaponEquipSource
    {
        None = 0,
        Standard = 1,
        Exclusive = 2
    }

    public enum WeaponTier
    {
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SS = 5
    }

    public enum WeaponFuseMode
    {
        None = 0,
        ToNextStandard = 1,
        ToRandomExclusive = 2
    }

    public enum WeaponLimitBreakResult
    {
        Invalid = 0,
        Maxed = 1,
        NotEnoughCurrency = 2,
        RequiredLevelNotReached = 3,
        Success = 4,
        Failed = 5
    }
}