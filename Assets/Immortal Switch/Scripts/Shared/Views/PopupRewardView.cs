using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private RectTransform rewardContainer;

        [SerializeField] private UIReward rewardPrefab;

        [SerializeField] private TMP_Text remainingText;

        [SerializeField] private Button buttonClose;

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
            int displayIndex = 0;

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    ItemRewardData reward = rewards[i];

                    // Reward null thì bỏ qua, vẫn tiếp tục hiển thị item khác.
                    if (reward == null)
                    {
                        Debug.LogWarning(
                            $"[PopupRewardView] Reward tại index {i} bị null.");
                        continue;
                    }

                    bool setDisplaySuccess =
                        DatabaseManager.Instance != null &&
                        DatabaseManager.Instance.TrySetDisplayData(reward);

                    // Không tìm thấy data thì bỏ qua riêng item này.
                    if (!setDisplaySuccess)
                    {
                        Debug.LogWarning(
                            $"[PopupRewardView] Không tìm thấy display data. " +
                            $"ItemKey={reward.ItemKey}");

                        continue;
                    }

                    // TierInfo null cũng chỉ bỏ qua item đó.
                    if (reward.TierInfo == null)
                    {
                        Debug.LogWarning(
                            $"[PopupRewardView] TierInfo bị null. " +
                            $"ItemKey={reward.ItemKey}");

                        continue;
                    }

                    UIReward clone;

                    if (displayIndex < _rewards.Count)
                    {
                        clone = _rewards[displayIndex];

                        if (clone == null)
                        {
                            Debug.LogWarning(
                                $"[PopupRewardView] UIReward cache null tại index {displayIndex}.");

                            continue;
                        }

                        clone.gameObject.SetActive(true);
                    }
                    else
                    {
                        if (rewardPrefab == null || rewardContainer == null)
                        {
                            Debug.LogError(
                                "[PopupRewardView] rewardPrefab hoặc rewardContainer chưa được gán.");

                            break;
                        }

                        clone = Instantiate(
                            rewardPrefab,
                            rewardContainer,
                            false);

                        _rewards.Add(clone);
                    }

                    clone.transform.SetSiblingIndex(displayIndex);

                    clone.Bind(
                        reward.ItemIcon,
                        reward.TierInfo.border,
                        reward.TierInfo.background,
                        reward.TierInfo.tierIcon);

                    clone.BindQuantity(reward.Quantity);

                    displayIndex++;
                }
            }

            // Tắt những UI cũ không còn được sử dụng.
            for (int i = displayIndex; i < _rewards.Count; i++)
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
            PopupRewardService.Close();
        }

        private void OnDestroy()
        {
            KillCountdown();
        }
    }

    public static class PopupRewardService
    {
        public static bool IsShowing =>
            UIManager.Instance != null &&
            UIManager.Instance.IsOpen<PopupRewardView>();

        public static void Show(
            IReadOnlyList<ItemRewardData> rewards,
            Action onClose = null,
            bool withBackdrop = false)
        {
            ShowAsync(rewards, onClose, withBackdrop).Forget();
        }

        private static async UniTask<PopupRewardView> ShowAsync(
            IReadOnlyList<ItemRewardData> rewards,
            Action onClose = null,
            bool withBackdrop = true)
        {
            if (UIManager.Instance == null)
            {
                UnityEngine.Debug.LogError(
                    "[PopupRewardService] UIManager instance was not found.");
                return null;
            }

            var args = new PopupRewardArgs
            {
                Rewards = rewards == null
                    ? new List<ItemRewardData>()
                    : new List<ItemRewardData>(rewards),
                OnClose = onClose
            };

            return await UIManager.Instance.OpenPopupAsync<PopupRewardView>(
                args,
                withBackdrop);
        }

        public static void Close()
        {
            if (UIManager.Instance == null)
                return;

            UIManager.Instance.Close<PopupRewardView>();
        }
    }
}