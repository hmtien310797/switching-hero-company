using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public abstract class BaseSummonButtonUI : MonoBehaviour
    {
        [SerializeField] protected Button button;
        [SerializeField] protected TMP_Text amountText;
        [SerializeField] protected Image currencyIcon;
        [SerializeField] protected TMP_Text rollCountText;

        [Header("Icons")]
        [SerializeField] protected Sprite ticketIcon;
        [SerializeField] protected Sprite diamondIcon;

        [Header("Visual")]
        [SerializeField] protected GameObject redDot;
        [SerializeField] protected Color normalColor = Color.white;
        [SerializeField] protected Color notEnoughColor = Color.red;
        [SerializeField] protected CanvasGroup canvasGroup;

        protected string optionId;
        protected System.Action<string> clickAction;
        private bool isBound;

        public virtual void Init(string summonOptionId, System.Action<string> onClick)
        {
            optionId = summonOptionId;
            clickAction = onClick;

            if (!isBound && button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
                isBound = true;
            }

            Refresh();
        }

        public abstract void Refresh();

        public virtual void SetInteractable(bool value)
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

        protected void SetRollCountText(int rollCount)
        {
            if (rollCountText != null)
                rollCountText.text = rollCount.ToString();
        }

        protected void SetUI(int amount, Sprite icon, bool showRedDot, Color color)
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