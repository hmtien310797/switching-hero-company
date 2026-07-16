using Immortal_Switch.Scripts.Currency;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    public class GameWeaponSummonCurrencyGateway : IWeaponSummonCurrencyGateway
    {
        public bool CanSpendWeaponTicket(int amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.summon_ticket_weapon, amount);
        }

        public bool CanSpendGem(int amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.diamond, amount);
        }

        public void SpendWeaponTicket(int amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.summon_ticket_weapon, amount, CurrencyTransactionReason.SummonWeapon);
        }

        public void SpendGem(int amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.diamond, amount, CurrencyTransactionReason.SummonWeapon);
        }
    }
}