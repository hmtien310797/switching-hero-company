namespace Immortal_Switch.Scripts.Currency
{
    public enum CurrencyTransactionType
    {
        Income = 0,
        Spend = 1
    }

    public enum CurrencyTransactionReason
    {
        Unknown = 0,

        OnlineFarming = 10,
        ClearStageReward = 11,
        OfflineAfkReward = 12,
        Gem,
        Diamond,
        Summon,
        ShopPurchase = 100,
        Upgrade = 101,

        WeaponUpgrade = 103,
        SkillUpgrade = 104,
        GrowthUpgrade = 105,
        FuseWeapon,
        SummonSkill,
        SummonHero,
        SummonWeapon,
        LevelUpStandardWeapon,
        LevelUpExclusiveWeapon,
        LimitBreakStandardWeapon,
        LimitBreakExclusiveWeapon
    }
}