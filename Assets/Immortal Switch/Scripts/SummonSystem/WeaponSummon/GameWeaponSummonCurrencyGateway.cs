using Immortal_Switch.Scripts.Currency;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    public class GameWeaponSummonCurrencyGateway : IWeaponSummonCurrencyGateway
    {
        public bool CanSpendWeaponTicket(int amount)
        {
            return CurrencyManager.Instance.Get(CurrencyType.WeaponTicket) >= amount;
        }

        public bool CanSpendGem(int amount)
        {
            return CurrencyManager.Instance.Get(CurrencyType.Diamond) >= amount;
        }

        public void SpendWeaponTicket(int amount)
        {
            CurrencyManager.Instance.Spend(CurrencyType.WeaponTicket, amount);
        }

        public void SpendGem(int amount)
        {
            CurrencyManager.Instance.Spend(CurrencyType.Diamond, amount);
        }
    }
}