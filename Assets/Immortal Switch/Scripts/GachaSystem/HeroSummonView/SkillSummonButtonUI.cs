using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.SkillSummon;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonButtonUI : BaseSummonButtonUI
    {
        public override void Refresh()
        {
            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return;

            var option = SkillSummonManager.Instance.Service.GetOption(optionId);
            if (option == null)
                return;

            SetRollCountText(option.RollCount);

            int ticket = CurrencyManager.Instance.Get(CurrencyType.SkillTicket);
            int gem = CurrencyManager.Instance.Get(CurrencyType.Diamond);

            bool hasEnoughTicket = ticket >= option.TicketCost;
            bool hasEnoughGem = gem >= option.GemCost;

            if (hasEnoughTicket)
            {
                SetUI(
                    amount: option.TicketCost,
                    icon: ticketIcon,
                    showRedDot: true,
                    color: normalColor
                );
            }
            else if (hasEnoughGem)
            {
                SetUI(
                    amount: option.GemCost,
                    icon: diamondIcon,
                    showRedDot: false,
                    color: normalColor
                );
            }
            else
            {
                SetUI(
                    amount: option.GemCost,
                    icon: diamondIcon,
                    showRedDot: false,
                    color: notEnoughColor
                );
            }
        }
    }
}