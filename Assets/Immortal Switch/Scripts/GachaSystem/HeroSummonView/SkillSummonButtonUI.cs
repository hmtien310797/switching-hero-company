using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.SkillSummon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private TMP_Text rollCountText;

        [Header("Icons")]
        [SerializeField] private Sprite skillTicketIcon;
        [SerializeField] private Sprite diamondIcon;

        [Header("Visual")]
        [SerializeField] private GameObject redDot;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color notEnoughColor = Color.red;
        [SerializeField] private CanvasGroup canvasGroup;

        private string optionId;
        private System.Action<string> clickAction;

        public void Init(string summonOptionId, System.Action<string> onClick)
        {
            optionId = summonOptionId;
            clickAction = onClick;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }

            Refresh();
        }

        public void Refresh()
        {
            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return;

            var option = SkillSummonManager.Instance.Service.GetOption(optionId);
            if (option == null)
                return;

            if (rollCountText != null)
                rollCountText.text = option.RollCount.ToString();

            int ticket = CurrencyManager.Instance.Get(CurrencyType.SkillTicket);
            int gem = CurrencyManager.Instance.Get(CurrencyType.Diamond);

            bool hasEnoughTicket = ticket >= option.TicketCost;
            bool hasEnoughGem = gem >= option.GemCost;

            if (hasEnoughTicket)
            {
                SetUI(option.TicketCost, skillTicketIcon, true, normalColor);
            }
            else if (hasEnoughGem)
            {
                SetUI(option.GemCost, diamondIcon, false, normalColor);
            }
            else
            {
                SetUI(option.GemCost, diamondIcon, false, notEnoughColor);
            }
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = value;
                canvasGroup.blocksRaycasts = value;
                canvasGroup.alpha = value ? 1f : 0.5f;
            }
        }

        private void SetUI(int amount, Sprite icon, bool showRedDot, Color color)
        {
            if (amountText != null)
            {
                amountText.text = amount.ToString();
                amountText.color = color;
            }

            if (currencyIcon != null)
                currencyIcon.sprite = icon;

            if (redDot != null)
                redDot.SetActive(showRedDot);
        }

        private void HandleClick()
        {
            clickAction?.Invoke(optionId);
        }
    }
}