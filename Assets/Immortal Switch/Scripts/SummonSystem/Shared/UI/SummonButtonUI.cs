using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.UI
{
    public class SummonButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private TMP_Text rollCountText;

        [Header("Icons")]
        [PreviewField][SerializeField] private Sprite heroTicketIcon;
        [PreviewField][SerializeField] private Sprite skillTicketIcon;
        [PreviewField][SerializeField] private Sprite weaponTicketIcon;
        [PreviewField][SerializeField] private Sprite diamondIcon;

        [Header("Visual")]
        [SerializeField] private GameObject redDot;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color notEnoughColor = Color.red;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Category")]
        [SerializeField] private SummonCategory summonCategory = SummonCategory.Hero;

        private string optionId;
        private System.Action<string> clickAction;

        public void Init(string summonOptionId, System.Action<string> onClick, SummonCategory category)
        {
            summonCategory = category;
            Init(summonOptionId, onClick);
        }

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
            if (summonCategory == SummonCategory.Skill)
                RefreshSkill();
            else if (summonCategory == SummonCategory.Weapon)
                RefreshWeapon();
            else
                RefreshHero();
        }

        private void RefreshHero()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            var option = HeroSummonManager.Instance.Service.GetOption(optionId);
            if (option == null)
                return;

            if (rollCountText != null)
                rollCountText.text = $"{option.RollCount} lần";

            BigNumber ticket = CurrencyManager.Instance.Get(CurrencyType.HeroTicket);
            BigNumber gem    = CurrencyManager.Instance.Get(CurrencyType.diamond);

            bool hasEnoughTicket = ticket >= option.TicketCost;
            bool hasEnoughGem    = gem    >= option.GemCost;

            if (hasEnoughTicket)
                SetUI(option.TicketCost, heroTicketIcon, true, normalColor);
            else if (hasEnoughGem)
                SetUI(option.GemCost, diamondIcon, false, normalColor);
            else
                SetUI(option.GemCost, diamondIcon, false, notEnoughColor);
        }

        private void RefreshWeapon()
        {
            if (WeaponSummonManager.Instance == null || WeaponSummonManager.Instance.Service == null)
                return;

            var option = WeaponSummonManager.Instance.Service.GetOption(optionId);
            if (option == null)
                return;

            if (rollCountText != null)
                rollCountText.text = $"{option.RollCount} lần";

            BigNumber ticket = CurrencyManager.Instance.Get(CurrencyType.WeaponTicket);
            BigNumber gem    = CurrencyManager.Instance.Get(CurrencyType.diamond);

            bool hasEnoughTicket = ticket >= option.TicketCost;
            bool hasEnoughGem    = gem    >= option.GemCost;

            if (hasEnoughTicket)
                SetUI(option.TicketCost, weaponTicketIcon, true, normalColor);
            else if (hasEnoughGem)
                SetUI(option.GemCost, diamondIcon, false, normalColor);
            else
                SetUI(option.GemCost, diamondIcon, false, notEnoughColor);
        }

        private void RefreshSkill()
        {
            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return;

            var option = SkillSummonManager.Instance.Service.GetOption(optionId);
            if (option == null)
                return;

            if (rollCountText != null)
                rollCountText.text = $"{option.RollCount} lần";

            BigNumber ticket = CurrencyManager.Instance.Get(CurrencyType.SkillTicket);
            BigNumber gem    = CurrencyManager.Instance.Get(CurrencyType.diamond);

            bool hasEnoughTicket = ticket >= option.TicketCost;
            bool hasEnoughGem    = gem    >= option.GemCost;

            if (hasEnoughTicket)
                SetUI(option.TicketCost, skillTicketIcon, true, normalColor);
            else if (hasEnoughGem)
                SetUI(option.GemCost, diamondIcon, false, normalColor);
            else
                SetUI(option.GemCost, diamondIcon, false, notEnoughColor);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;

            if (canvasGroup != null)
            {
                canvasGroup.interactable  = value;
                canvasGroup.blocksRaycasts = value;
                canvasGroup.alpha          = value ? 1f : 0.5f;
            }
        }

        private void SetUI(int amount, Sprite icon, bool showRedDot, Color color)
        {
            if (amountText != null)
            {
                amountText.text  = amount.ToString();
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
