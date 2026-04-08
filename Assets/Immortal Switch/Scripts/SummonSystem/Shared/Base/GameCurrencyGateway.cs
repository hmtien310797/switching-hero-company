using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.Base
{
    public class GameCurrencyGateway : IHeroSummonCurrencyGateway
    {
        public int GetHeroTicket()
        {
            return CurrencyManager.Instance.Get(CurrencyType.HeroTicket);
        }

        public int GetGem()
        {
            return CurrencyManager.Instance.Get(CurrencyType.Diamond);
        }

        public bool CanSpendHeroTicket(int amount)
        {
            return CurrencyManager.Instance.HasEnough(CurrencyType.HeroTicket, amount);
        }

        public bool CanSpendGem(int amount)
        {
            return CurrencyManager.Instance.HasEnough(CurrencyType.Diamond, amount);
        }

        public void SpendHeroTicket(int amount)
        {
            CurrencyManager.Instance.Spend(CurrencyType.HeroTicket, amount);
        }

        public void SpendGem(int amount)
        {
            CurrencyManager.Instance.Spend(CurrencyType.Diamond, amount);
        }
    }
}