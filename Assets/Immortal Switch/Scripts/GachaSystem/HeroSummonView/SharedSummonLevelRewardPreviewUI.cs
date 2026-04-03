using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SharedSummonLevelRewardPreviewUI : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text amountText;

        [Header("Icon")]
        [SerializeField] private Image rewardIcon;

        [Header("State")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject claimableHighlight;
        [SerializeField] private GameObject redDot;
        [SerializeField] private Button claimButton;

        [Header("Alpha")]
        [SerializeField] private float claimableAlpha = 1f;
        [SerializeField] private float claimedAlpha = 0.45f;

        private Action claimAction;
        private SharedSummonRewardPreviewData currentData;

        private void Awake()
        {
            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(HandleClaim);
            }
        }

        public void Bind(SharedSummonRewardPreviewData data, Action onClaim)
        {
            currentData = data;
            claimAction = onClaim;

            if (levelText != null)
                levelText.text = data != null ? $"Lv.{data.SummonLevel}" : string.Empty;

            if (amountText != null)
                amountText.text = data != null ? data.AmountText : string.Empty;

            if (rewardIcon != null)
            {
                rewardIcon.sprite = data != null ? data.RewardIcon : null;
                rewardIcon.enabled = rewardIcon.sprite != null;
            }

            bool isClaimable = data != null && data.IsClaimable;
            bool isClaimed = data != null && data.IsClaimed;

            if (canvasGroup != null)
                canvasGroup.alpha = isClaimable ? claimableAlpha : claimedAlpha;

            if (claimableHighlight != null)
                claimableHighlight.SetActive(isClaimable && !isClaimed);

            if (redDot != null)
                redDot.SetActive(isClaimable && !isClaimed);

            if (claimButton != null)
                claimButton.interactable = isClaimable && !isClaimed;
        }

        private void HandleClaim()
        {
            if (currentData == null || !currentData.IsClaimable || currentData.IsClaimed)
                return;

            claimAction?.Invoke();
        }
    }
}