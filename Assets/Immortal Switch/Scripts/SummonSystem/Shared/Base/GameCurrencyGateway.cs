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
            return CurrencyManager.Instance.HasEnough(CurrencyType.HeroTicket, amount);
        }

        public bool CanSpendGem(int amount)
        {
            return CurrencyManager.Instance.HasEnough(CurrencyType.diamond, amount);
        }

        public void SpendHeroTicket(BigNumber amount)
        {
            CurrencyManager.Instance.SpendLocalDemo(CurrencyType.HeroTicket, amount);
        }

        public void SpendGem(int amount)
        {
            CurrencyManager.Instance.SpendLocalDemo(CurrencyType.diamond, amount);
        }
    }
}