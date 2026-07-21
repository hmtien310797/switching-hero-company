using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.UI
{
    public class SummonLevelRewardPreviewUI : MonoBehaviour
    {
        [Header("Category")]
        [SerializeField] private SummonCategory category;
        
        [Header("Texts")]
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

        [Header("Alpha")]
        [SerializeField] private float claimableAlpha = 1f;
        [SerializeField] private float claimedAlpha = 0.45f;

        private ISummonRewardReceiver rewardReceiver;
        private SummonRewardPreviewData currentPreviewData;

        private void Awake()
        {
            rewardReceiver = rewardReceiverBehaviour as ISummonRewardReceiver;

            if (claimButton != null)
            {
                claimButton.onClick.RemoveAllListeners();
                claimButton.onClick.AddListener(HandleClaim);
            }
        }

        public void Refresh()
        {
            rewardReceiver ??= rewardReceiverBehaviour as ISummonRewardReceiver;
            currentPreviewData = rewardReceiver.GetRewardPreviewData(category);
            Bind(currentPreviewData);
        }

        private void Bind(SummonRewardPreviewData data)
        {
            if (data == null || data.ItemId == null)
            {
                if (amountText != null)
                    amountText.text = string.Empty;

                if (rewardIcon != null)
                {
                    rewardIcon.sprite = null;
                    rewardIcon.enabled = false;
                }

                ApplyState(false);
                return;
            }

            if (amountText != null)
                amountText.text = data.Quantity.ToString();

            if (rewardIcon != null)
            {
                var visual = DatabaseManager.Instance.ItemDb.LoadIconByItemId(data.ItemId);
                rewardIcon.sprite = visual != null ? visual : null;
                rewardIcon.enabled = rewardIcon.sprite != null;
            }

            ApplyState(data.IsClaimable);
        }

        private void ApplyState(bool isClaimable)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = isClaimable ? claimableAlpha : claimedAlpha;

            if (claimableHighlight != null)
                claimableHighlight.SetActive(isClaimable);

            if (redDot != null)
                redDot.SetActive(isClaimable);

            if (claimButton != null)
                claimButton.interactable = isClaimable;
        }

        private void HandleClaim()
        {
            if (currentPreviewData == null || !currentPreviewData.IsClaimable)
                return;

            if (rewardReceiver == null)
            {
                Debug.LogWarning("rewardReceiver is null");
                return;
            }

            if (claimButton != null)
                claimButton.interactable = false;

            HandleClaimAsync().Forget();
        }

        private async UniTask HandleClaimAsync()
        {
            await rewardReceiver.ClaimReward(currentPreviewData.SummonLevel, rewardReceiver, category);
            Refresh();
        }
    }
}