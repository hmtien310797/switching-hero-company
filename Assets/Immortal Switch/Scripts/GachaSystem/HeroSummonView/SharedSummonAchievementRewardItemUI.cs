using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SharedSummonAchievementRewardItemUI : MonoBehaviour
    {
        [SerializeField] private Image rewardIcon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private GameObject claimedIconObject;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private float normalAlpha = 1f;
        [SerializeField] private float claimedAlpha = 0.45f;

        public void Bind(SharedSummonAchievementItemData data)
        {
            if (titleText != null)
                titleText.text = data != null ? data.Title : string.Empty;

            if (rewardText != null)
                rewardText.text = data != null ? data.RewardText : string.Empty;

            if (rewardIcon != null)
            {
                rewardIcon.sprite = data != null ? data.RewardIcon : null;
                rewardIcon.enabled = rewardIcon.sprite != null;
            }

            bool isClaimed = data != null && data.IsClaimed;

            if (claimedIconObject != null)
                claimedIconObject.SetActive(isClaimed);

            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = isClaimed ? claimedAlpha : normalAlpha;
        }
    }
}