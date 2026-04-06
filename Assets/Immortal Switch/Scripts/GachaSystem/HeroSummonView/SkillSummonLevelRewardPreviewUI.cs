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

        [Header("Reward Icons")]
        [SerializeField] private Sprite skillTicketIcon;
        [SerializeField] private Sprite ssSkillIcon;

        [Header("Alpha")]
        [SerializeField] private float claimableAlpha = 1f;
        [SerializeField] private float claimedAlpha = 0.45f;

        private ISkillSummonRewardReceiver rewardReceiver;
        private SkillSummonRewardPreviewData currentPreviewData;

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

        private void Bind(SkillSummonRewardPreviewData data)
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

            if (amountText != null)
                amountText.text = BuildAmountText(data.RewardItem);

            if (rewardIcon != null)
            {
                rewardIcon.sprite = GetRewardIcon(data.RewardItem);
                rewardIcon.color = Color.white;
                rewardIcon.enabled = rewardIcon.sprite != null;
            }

            ApplyState(data.IsClaimable, data.IsClaimed);
        }

        private void ApplyState(bool isClaimable, bool isClaimed)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = isClaimed ? claimedAlpha : (isClaimable ? claimableAlpha : claimedAlpha);

            if (claimableHighlight != null)
                claimableHighlight.SetActive(isClaimable && !isClaimed);

            if (redDot != null)
                redDot.SetActive(isClaimable && !isClaimed);

            if (claimButton != null)
                claimButton.interactable = isClaimable && !isClaimed;
        }

        private string BuildAmountText(SkillSummonRewardItem reward)
        {
            if (reward == null)
                return string.Empty;

            switch (reward.RewardType)
            {
                case SkillSummonRewardType.Currency:
                    return reward.Amount.ToString();
                case SkillSummonRewardType.RandomSkill:
                    return $"x{reward.Amount}";
                default:
                    return reward.Amount.ToString();
            }
        }

        private Sprite GetRewardIcon(SkillSummonRewardItem reward)
        {
            if (reward == null)
                return null;

            switch (reward.RewardType)
            {
                case SkillSummonRewardType.Currency:
                    return skillTicketIcon;
                case SkillSummonRewardType.RandomSkill:
                    return ssSkillIcon;
                default:
                    return null;
            }
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

            bool claimed = SkillSummonManager.Instance.ClaimReward(currentPreviewData.SummonLevel, rewardReceiver);
            if (!claimed)
                return;

            Refresh();
        }
    }
}