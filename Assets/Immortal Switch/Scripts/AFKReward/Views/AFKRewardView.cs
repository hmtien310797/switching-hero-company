using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.AFKReward.Views
{
    public class AFKRewardArgs
    {
        /// <summary>
        /// claim thuong.
        /// value 1: nhan x2 hay ko
        /// </summary>
        public Action<bool> OnClaim { get; set; }

        /// <summary>
        /// rewards
        /// </summary>
        public StageReward[] Rewards { get; set; }

        /// <summary>
        /// thoi gian chien dau
        /// </summary>
        public DateTime AfkCurrentTime { get; set; }
    }

    public class AFKRewardView : AnimatedUIView
    {
        [Header("View references")]
        [SerializeField]
        private TextMeshProUGUI txtAfkCurrentTime;

        [SerializeField]
        private TextMeshProUGUI txtAds;

        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private Button btnClaimX2;

        [Header("Reward references")]
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIReward rewardPrefab;

        [Header("Base reward references")]
        [SerializeField]
        private TextMeshProUGUI txtGoldPs;

        [SerializeField]
        private TextMeshProUGUI txtDiamondPs;

        // --- Private Fields ---
        private List<UIReward> _rewards = new();
        private AFKRewardArgs _args;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
            btnClaimX2.onClick.AddListener(OnClickClaimX2);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaim);
            btnClaimX2.onClick.RemoveListener(OnClickClaimX2);
        }

        private void OnClickClaim()
        {
            _args?.OnClaim?.Invoke(false);
            UIManager.Instance.Close<AFKRewardView>();
        }

        private void OnClickClaimX2()
        {
            if (AFKRewardManager.Instance.RecordClaimX2())
            {
                _args?.OnClaim?.Invoke(true);
            }
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is AFKRewardArgs runtime)
            {
                _args = runtime;

                RefreshRewards(_args.Rewards);
                RefreshBaseRewards(_args.Rewards);
                RefreshCurrentTime();
            }

            RefreshAdsState();
        }

        private void RefreshCurrentTime()
        {
            txtAfkCurrentTime.text =
                $"{_args.AfkCurrentTime.Minute:00} phút {_args.AfkCurrentTime.Second:00}s" +
                "\n<color=#afa071><size=32>Tối đa 12 giờ</size></color>";
        }

        private void RefreshAdsState()
        {
            var remaining = AFKRewardManager.Instance.GetRemainingAds();
            btnClaimX2.interactable = remaining > 0;

            if (txtAds != null)
            {
                txtAds.text = $"{remaining}/{ValueConstants.MAX_ADS_COUNT}";
            }
        }

        private void RefreshBaseRewards(StageReward[] rewards)
        {
            var baseGoldReward = rewards.FirstOrDefault(v => v.currencyType == CurrencyType.gold);

            if (baseGoldReward != null)
            {
                txtGoldPs.text = $"+{baseGoldReward.Amount.ToInputString()}/60s";
            }

            var baseDiamondReward = rewards.FirstOrDefault(v => v.currencyType == CurrencyType.diamond);

            if (baseDiamondReward != null)
            {
                txtDiamondPs.text = $"+{baseDiamondReward.Amount.ToInputString()}/60s";
            }
        }

        private void RefreshRewards(StageReward[] rewards)
        {
            for (var index = 0; index < rewards.Length; index++)
            {
                var reward = rewards[index];
                var itemDisplay = DatabaseManager.Instance.GetDisplayDataByCurrency(reward.currencyType);

                if (itemDisplay != null)
                {
                    if (_rewards.Count > index)
                    {
                        var clone = _rewards[index];
                        clone.gameObject.SetActive(true);

                        clone.Bind(
                            itemDisplay.ItemIcon,
                            itemDisplay.TierInfo.border,
                            itemDisplay.TierInfo.background,
                            itemDisplay.TierInfo.tierIcon
                        );

                        clone.BindQuantity(reward.Amount);
                    }
                    else
                    {
                        var clone = Instantiate(rewardPrefab, rewardContainer);

                        clone.Bind(
                            itemDisplay.ItemIcon,
                            itemDisplay.TierInfo.border,
                            itemDisplay.TierInfo.background,
                            itemDisplay.TierInfo.tierIcon
                        );

                        clone.BindQuantity(reward.Amount);
                        _rewards.Add(clone);
                    }
                }
            }

            for (int i = rewards.Length; i < _rewards.Count; i++)
            {
                _rewards[i].gameObject.SetActive(false);
            }
        }
    }
}