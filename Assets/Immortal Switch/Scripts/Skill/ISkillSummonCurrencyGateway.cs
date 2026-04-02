using Immortal_Switch.Scripts.Currency;

namespace Immortal_Switch.Scripts.SkillSummon
{
    public interface ISkillSummonCurrencyGateway
    {
        int GetSkillTicket();
        int GetGem();

        bool CanSpendSkillTicket(int amount);
        bool CanSpendGem(int amount);

        void SpendSkillTicket(int amount);
        void SpendGem(int amount);

        void AddCurrency(CurrencyType currencyType, int amount);
    }

    public class GameSkillSummonCurrencyGateway : ISkillSummonCurrencyGateway
    {
        public int GetSkillTicket()
        {
            return CurrencyManager.Instance.Get(CurrencyType.SkillTicket);
        }

        public int GetGem()
        {
            return CurrencyManager.Instance.Get(CurrencyType.Diamond);
        }

        public bool CanSpendSkillTicket(int amount)
        {
            return CurrencyManager.Instance.HasEnough(CurrencyType.SkillTicket, amount);
        }

        public bool CanSpendGem(int amount)
        {
            return CurrencyManager.Instance.HasEnough(CurrencyType.Diamond, amount);
        }

        public void SpendSkillTicket(int amount)
        {
            CurrencyManager.Instance.Spend(CurrencyType.SkillTicket, amount);
        }

        public void SpendGem(int amount)
        {
            CurrencyManager.Instance.Spend(CurrencyType.Diamond, amount);
        }

        public void AddCurrency(CurrencyType currencyType, int amount)
        {
            CurrencyManager.Instance.Add(currencyType, amount);
        }
    }
}