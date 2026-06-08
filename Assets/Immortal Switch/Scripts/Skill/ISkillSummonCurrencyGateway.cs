using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;

namespace Immortal_Switch.Scripts.Skill
{
    public interface ISkillSummonCurrencyGateway
    {
        BigNumber GetSkillTicket();
        BigNumber GetGem();

        bool CanSpendSkillTicket(BigNumber amount);
        bool CanSpendGem(BigNumber amount);

        void SpendSkillTicket(BigNumber amount);
        void SpendGem(BigNumber amount);

        void AddCurrency(CurrencyType currencyType, BigNumber amount);
    }

    public class GameSkillSummonCurrencyGateway : ISkillSummonCurrencyGateway
    {
        public BigNumber GetSkillTicket()
        {
            return CurrencyManager.Instance.Get(CurrencyType.SkillTicket);
        }

        public BigNumber GetGem()
        {
            return CurrencyManager.Instance.Get(CurrencyType.diamond);
        }

        public bool CanSpendSkillTicket(BigNumber amount)
        {
            return CurrencyManager.Instance.HasEnough(CurrencyType.SkillTicket, amount);
        }

        public bool CanSpendGem(BigNumber amount)
        {
            return CurrencyManager.Instance.HasEnough(CurrencyType.diamond, amount);
        }

        public void SpendSkillTicket(BigNumber amount)
        {
            CurrencyManager.Instance.SpendLocalDemo(CurrencyType.SkillTicket, amount);
        }

        public void SpendGem(BigNumber amount)
        {
            CurrencyManager.Instance.SpendLocalDemo(CurrencyType.diamond, amount);
        }

        public void AddCurrency(CurrencyType currencyType, BigNumber amount)
        {
            CurrencyManager.Instance.AddLocalDemo(currencyType, amount);
        }
    }
}