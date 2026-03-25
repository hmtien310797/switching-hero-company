using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonRewardClaimView : MonoBehaviour
    {
        [SerializeField] private Button claimAllButton;
        [SerializeField] private TMP_Text claimInfoText;
        [SerializeField] private MonoBehaviour rewardReceiverBehaviour;

        private IHeroSummonRewardReceiver rewardReceiver;

        private void Awake()
        {
            rewardReceiver = rewardReceiverBehaviour as IHeroSummonRewardReceiver;

            if (claimAllButton != null)
                claimAllButton.onClick.AddListener(ClaimAll);
        }

        public void Refresh()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            List<int> claimable = HeroSummonManager.Instance.Service.GetClaimableRewardLevels();

            if (claimAllButton != null)
                claimAllButton.gameObject.SetActive(claimable.Count > 0);

            if (claimInfoText != null)
            {
                if (claimable.Count > 0)
                    claimInfoText.text = $"Claimable: {string.Join(", ", claimable)}";
                else
                    claimInfoText.text = string.Empty;
            }
        }

        private void ClaimAll()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            if (rewardReceiver == null)
            {
                Debug.LogWarning("HeroSummonRewardClaimView: rewardReceiver is null");
                return;
            }

            var claimable = HeroSummonManager.Instance.Service.GetClaimableRewardLevels();
            for (int i = 0; i < claimable.Count; i++)
            {
                HeroSummonManager.Instance.ClaimReward(claimable[i], rewardReceiver);
            }

            Refresh();
        }
    }
}