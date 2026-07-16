using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.Base
{
    public class GameCurrencyGateway : IHeroSummonCurrencyGateway
    {
        public BigNumber GetHeroTicket()
        {
            return CurrencyLedgerService.Instance.GetDisplayBalance(CurrencyType.summon_ticket_hero);
        }

        public BigNumber GetGem()
        {
            return CurrencyLedgerService.Instance.GetDisplayBalance(CurrencyType.diamond);
        }

        public bool CanSpendHeroTicket(BigNumber amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.summon_ticket_hero, amount);
        }

        public bool CanSpendGem(int amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.diamond, amount);
        }

        public void SpendHeroTicket(BigNumber amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.summon_ticket_hero, amount, CurrencyTransactionReason.SummonHero);
        }

        public void SpendGem(int amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.diamond, amount, CurrencyTransactionReason.SummonHero);
        }
    }
}