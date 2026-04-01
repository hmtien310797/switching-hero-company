using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SummonAchievementRewardItemUI : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private Image rewardIcon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardText;

        [Header("State")]
        [SerializeField] private GameObject claimedIconObject;

        [Header("Visual")]
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private float normalAlpha = 1f;
        [SerializeField] private float claimedAlpha = 0.45f;

        private SummonAchievementRewardItemData boundData;

        public void Bind(SummonAchievementRewardItemData data)
        {
            boundData = data;

            if (titleText != null)
                titleText.text = data != null ? data.Title : string.Empty;

            if (rewardText != null)
                rewardText.text = data != null ? data.RewardText : string.Empty;

            if (rewardIcon != null)
            {
                rewardIcon.sprite = data != null ? data.RewardIcon : null;
                rewardIcon.enabled = rewardIcon.sprite != null;
            }

            ApplyState(data != null ? data.State : SummonAchievementRewardState.Normal);
        }

        private void ApplyState(SummonAchievementRewardState state)
        {
            bool isClaimed = state == SummonAchievementRewardState.Claimed;

            if (claimedIconObject != null)
                claimedIconObject.SetActive(isClaimed);

            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = isClaimed ? claimedAlpha : normalAlpha;
        }
    }
}