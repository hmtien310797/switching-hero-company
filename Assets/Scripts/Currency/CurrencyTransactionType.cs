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

        ShopPurchase = 100,
        Upgrade = 101,
        Summon = 102,
        WeaponUpgrade = 103,
        SkillUpgrade = 104,
        GrowthUpgrade = 105
    }
}