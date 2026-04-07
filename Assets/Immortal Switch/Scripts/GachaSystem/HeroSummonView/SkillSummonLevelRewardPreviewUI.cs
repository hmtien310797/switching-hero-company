using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SkillSummon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonLevelRewardPreviewUI : MonoBehaviour
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

        [Header("Claim")]
        [SerializeField] private Button claimButton;
        [SerializeField] private MonoBehaviour rewardReceiverBehaviour;

        [Header("Visual Config")]
        [SerializeField] private SummonRewardVisualConfigSO rewardVisualConfig;

        [Header("Alpha")]
        [SerializeField] private float claimableAlpha = 1f;
        [SerializeField] private float claimedAlpha = 0.45f;

        private ISkillSummonRewardReceiver rewardReceiver;
        private SummonRewardPreviewData currentPreviewData;

        private void Awake()
        {
            rewardReceiver = rewardReceiverBehaviour as ISkillSummonRewardReceiver;

            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(HandleClaim);
            }
        }

        public void Refresh()
        {
            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return;

            currentPreviewData = SkillSummonManager.Instance.Service.GetRewardPreviewData();
            Bind(currentPreviewData);
        }

        private void Bind(SummonRewardPreviewData data)
        {
            int currentLevel = SkillSummonManager.Instance != null
                ? SkillSummonManager.Instance.GetCurrentSummonLevel()
                : 1;

            if (levelText != null)
                levelText.text = $"Lv.{currentLevel}";

            if (data == null || data.RewardItem == null)
            {
                if (amountText != null)
                    amountText.text = string.Empty;

                if (rewardIcon != null)
                {
                    rewardIcon.sprite = null;
                    rewardIcon.enabled = false;
                }

                ApplyState(false, false);
                return;
            }

            // amount
            if (amountText != null)
                amountText.text = data.RewardItem.Amount.ToString();
            
            if (rewardIcon != null)
            {
                var visual = rewardVisualConfig != null
                    ? rewardVisualConfig.Get(data.RewardItem)
                    : null;

                rewardIcon.sprite = visual != null ? visual.Icon : null;
                rewardIcon.color = visual != null ? visual.Tint : Color.white;
                rewardIcon.enabled = rewardIcon.sprite != null;
            }

            ApplyState(data.IsClaimable, data.IsClaimed);
        }

        private void ApplyState(bool isClaimable, bool isClaimed)
        {
            bool canClaim = isClaimable && !isClaimed;

            if (canvasGroup != null)
                canvasGroup.alpha = canClaim ? claimableAlpha : claimedAlpha;

            if (claimableHighlight != null)
                claimableHighlight.SetActive(canClaim);

            if (redDot != null)
                redDot.SetActive(canClaim);

            if (claimButton != null)
                claimButton.interactable = canClaim;
        }

        private void HandleClaim()
        {
            if (currentPreviewData == null || !currentPreviewData.IsClaimable)
                return;

            if (rewardReceiver == null)
            {
                Debug.LogWarning("SkillSummonLevelRewardPreviewUI: rewardReceiver is null");
                return;
            }

            if (SkillSummonManager.Instance == null)
                return;

            bool claimed = SkillSummonManager.Instance.ClaimReward(
                currentPreviewData.SummonLevel,
                rewardReceiver);

            if (!claimed)
                return;

            Refresh();
        }
    }
}