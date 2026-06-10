using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.StageSelection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class OfflineBattleRewardPopupView : AnimatedUIView
    {
        [Header("Runtime")]
        [SerializeField] private TMP_Text battleTimeText;
        [SerializeField] private TMP_Text maxTimeText;
        [SerializeField] private TMP_Text monstersDefeatedText;
        [SerializeField] private TMP_Text defeatsPerMinuteText;

        [Header("Rewards")]
        [SerializeField] private RewardItemView[] rewardItems;

        [Header("Buttons")]
        [SerializeField] private Button claimButton;
        [SerializeField] private Button bonusRewardButton;

        private OfflineBattleRewardResult result;

        protected void OnEnable()
        {

            if (claimButton != null)
                claimButton.onClick.AddListener(HandleClaimClicked);

            if (bonusRewardButton != null)
                bonusRewardButton.onClick.AddListener(HandleBonusRewardClicked);
        }

        protected void OnDisable()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(HandleClaimClicked);

            if (bonusRewardButton != null)
                bonusRewardButton.onClick.RemoveListener(HandleBonusRewardClicked);

        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            OfflineBattleRewardOpenParam param = args as OfflineBattleRewardOpenParam;
            if (param == null)
            {
                Debug.LogError("[OfflineBattleRewardPopupView] Invalid open param.");
                return;
            }

            result = param.Result;
            Bind(result);
        }

        private void Bind(OfflineBattleRewardResult result)
        {
            if (result == null)
                return;

            if (battleTimeText != null)
                battleTimeText.text = FormatTime(result.OfflineSeconds);

            if (maxTimeText != null)
                maxTimeText.text = $"Up to {result.MaxOfflineSeconds / 3600} Hours";

            if (monstersDefeatedText != null)
                monstersDefeatedText.text = result.MonstersDefeated.ToString("N0");

            if (defeatsPerMinuteText != null)
                defeatsPerMinuteText.text = $"{result.DefeatsPerMinute}/min";

            BindRewards(result);
        }

        private void BindRewards(OfflineBattleRewardResult result)
        {
            if (rewardItems == null)
                return;

            for (int i = 0; i < rewardItems.Length; i++)
            {
                RewardItemView item = rewardItems[i];

                if (item == null)
                    continue;

                if (result.Rewards == null || i >= result.Rewards.Count)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }

                item.gameObject.SetActive(true);
                item.Bind(result.Rewards[i]);
            }
        }

        private async void HandleClaimClicked()
        {
            if (claimButton != null)
                claimButton.interactable = false;

            await OfflineBattleRewardService.Instance.ClaimCurrentReward();

            UIManager.Instance.TogglePopupAsync<OfflineBattleRewardPopupView>(false);
        }

        private void HandleBonusRewardClicked()
        {
            // TODO ADS:
            // Xem quảng cáo x2 reward / bonus reward.
            Debug.Log("[OfflineBattleRewardPopupView] Bonus Reward clicked.");
        }

        private string FormatTime(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours:00}h {time.Minutes:00}m";

            return $"{time.Minutes:00}m {time.Seconds:00}s";
        }
    }
}