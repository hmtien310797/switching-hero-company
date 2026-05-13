namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    public interface IWeaponSummonCurrencyGateway
    {
        bool CanSpendWeaponTicket(int amount);
        bool CanSpendGem(int amount);
        void SpendWeaponTicket(int amount);
        void SpendGem(int amount);
    }
}