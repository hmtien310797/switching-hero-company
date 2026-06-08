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
            return CurrencyManager.Instance.Get(CurrencyType.diamond) >= amount;
        }

        public void SpendWeaponTicket(int amount)
        {
            CurrencyManager.Instance.SpendLocalDemo(CurrencyType.WeaponTicket, amount);
        }

        public void SpendGem(int amount)
        {
            CurrencyManager.Instance.SpendLocalDemo(CurrencyType.diamond, amount);
        }
    }
}