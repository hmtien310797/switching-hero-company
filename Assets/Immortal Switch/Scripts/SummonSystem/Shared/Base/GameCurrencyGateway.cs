using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.Base
{
    public class GameCurrencyGateway : IHeroSummonCurrencyGateway
    {
        public BigNumber GetHeroTicket()
        {
            return CurrencyManager.Instance.Get(CurrencyType.HeroTicket);
        }

        public BigNumber GetGem()
        {
            return CurrencyManager.Instance.Get(CurrencyType.diamond);
        }

        public bool CanSpendHeroTicket(BigNumber amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.HeroTicket, amount);
        }

        public bool CanSpendGem(int amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.diamond, amount);
        }

        public void SpendHeroTicket(BigNumber amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.HeroTicket, amount, CurrencyTransactionReason.SummonHero);
        }

        public void SpendGem(int amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.diamond, amount, CurrencyTransactionReason.SummonHero);
        }
    }
}