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
    }

    public class GameSkillSummonCurrencyGateway : ISkillSummonCurrencyGateway
    {
        public BigNumber GetSkillTicket()
        {
            return CurrencyLedgerService.Instance.GetDisplayBalance(CurrencyType.summon_ticket_skill);
        }

        public BigNumber GetGem()
        {
            return CurrencyLedgerService.Instance.GetDisplayBalance(CurrencyType.diamond);
        }

        public bool CanSpendSkillTicket(BigNumber amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.summon_ticket_skill, amount);
        }

        public bool CanSpendGem(BigNumber amount)
        {
            return CurrencyLedgerService.Instance.HasEnoughDisplayBalance(CurrencyType.diamond, amount);
        }

        public void SpendSkillTicket(BigNumber amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.summon_ticket_skill, amount, CurrencyTransactionReason.SummonSkill);
        }

        public void SpendGem(BigNumber amount)
        {
            CurrencyLedgerService.Instance.TrySpend(CurrencyType.diamond, amount, CurrencyTransactionReason.SummonHero);
        }
    }
}