using System;
using System.Collections.Generic;
using DG.Tweening;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.Views
{
    /// <summary>
    /// Thông tin dùng để hiển thị popup reward.
    /// </summary>
    public class PopupRewardArgs
    {
        /// <summary>
        /// ds item thuong
        /// </summary>
        public List<ItemRewardData> Rewards { get; set; }

        /// <summary>
        /// callback khi close popup.
        /// </summary>
        public Action OnClose { get; set; }
    }

    public class PopupRewardView : AnimatedUIView
    {
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIReward rewardPrefab;

        [SerializeField] 
        private TMP_Text remainingText;
        
        [SerializeField] 
        private Button buttonClose;

        // --- Private Fields ---
        private List<UIReward> _rewards = new();
        private PopupRewardArgs _args;
        private Tween countdownTween;

        private void Start()
        {
            buttonClose.onClick.AddListener(HidePopupRewardView);
        }


        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is not PopupRewardArgs runtime)
            {
                _args = null;
                HideAllRewards();
                KillCountdown();
                return;
            }

            _args = runtime;

            RefreshRewards(runtime.Rewards);
            StartCountdown(3);
        }
        
        private void HideAllRewards()
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i] != null)
                {
                    _rewards[i].gameObject.SetActive(false);
                }
            }
        }

        public override void OnHide()
        {
            KillCountdown();

            var callback = _args?.OnClose;
            _args = null;

            base.OnHide();

            callback?.Invoke();
        }

        private void RefreshRewards(List<ItemRewardData> rewards)
        {
            int rewardCount = rewards?.Count ?? 0;

            for (int i = 0; i < rewardCount; i++)
            {
                var reward = rewards[i];

                if (reward == null)
                    continue;

                DatabaseManager.Instance.TrySetDisplayData(reward);

                UIReward clone;

                if (_rewards.Count > i)
                {
                    clone = _rewards[i];
                    clone.gameObject.SetActive(true);
                }
                else
                {
                    clone = Instantiate(
                        rewardPrefab,
                        rewardContainer,
                        false);

                    _rewards.Add(clone);
                }

                clone.transform.SetSiblingIndex(i);

                clone.Bind(
                    reward.ItemIcon,
                    reward.TierInfo.border,
                    reward.TierInfo.background,
                    reward.TierInfo.tierIcon);

                clone.BindQuantity(reward.Quantity);
            }

            for (int i = rewardCount; i < _rewards.Count; i++)
            {
                if (_rewards[i] != null)
                {
                    _rewards[i].gameObject.SetActive(false);
                }
            }
        }
        
        private void StartCountdown(int durationSeconds)
        {
            KillCountdown();

            durationSeconds = Mathf.Max(0, durationSeconds);

            if (durationSeconds == 0)
            {
                UpdateRemainingText(0);
                HidePopupRewardView();
                return;
            }

            UpdateRemainingText(durationSeconds);

            countdownTween = DOVirtual.Int(
                    durationSeconds,
                    0,
                    durationSeconds,
                    UpdateRemainingText)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    countdownTween = null;
                    HidePopupRewardView();
                });
        }

        private void UpdateRemainingText(int value)
        {
            if (remainingText != null)
            {
                remainingText.text = $"Pop up will close in {value}.";
            }
        }

        private void KillCountdown()
        {
            countdownTween?.Kill();
            countdownTween = null;
        }
        
        private void HidePopupRewardView()
        {
            KillCountdown();
            UIManager.Instance.Close<PopupRewardView>();
        }
        
        private void OnDestroy()
        {
            KillCountdown();
        }
    }
}