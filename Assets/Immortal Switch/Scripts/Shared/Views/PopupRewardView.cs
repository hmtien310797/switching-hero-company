using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
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

        public PopupRewardType RewardType;
        
        public List<HeroItemData> HeroItemRewards { get; set; }
        public List<SkillItemData> SkillItemRewards { get; set; }
        public List<WeaponItemData> WeaponItemRewards { get; set; }
        
        public List<ItemData> Rewards { get; set; }

        /// <summary>
        /// callback khi close popup.
        /// </summary>
        public Action OnClose { get; set; }
    }

    public class PopupRewardView : BouncePopupUIView
    {
        [SerializeField]
        private RectTransform rewardContainer;

        [SerializeField]
        private UIRewardQuantity rewardPrefab;

        [SerializeField]
        private TMP_Text remainingText;

        [SerializeField]
        private Button buttonClose;

        // --- Private Fields ---
        private List<UIRewardQuantity> _rewards = new();
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

            switch (_args.RewardType)
            {
                case PopupRewardType.NormalItem:
                    RefreshRewards(runtime.Rewards);
                    break;
                case  PopupRewardType.HeroItem:
                    RefreshRewards(_args.HeroItemRewards);
                    break;
                case PopupRewardType.SkillItem:
                    RefreshRewards(_args.SkillItemRewards);
                    break;
                case PopupRewardType.WeaponItem:
                    RefreshRewards(_args.WeaponItemRewards);
                    break;
            }
            
            // Ẩn tất cả reward để chờ animate tuần tự
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i] != null && _rewards[i].gameObject.activeSelf)
                {
                    _rewards[i].transform.localScale = Vector3.zero;
                }
            }
        }

        private void HideAllRewards()
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i] != null)
                {
                    _rewards[i].transform.DOKill();
                    _rewards[i].gameObject.SetActive(false);
                }
            }
        }

        public override void OnHide()
        {
            KillCountdown();
            KillRewardAnimations();

            var callback = _args?.OnClose;
            _args = null;

            base.OnHide();

            callback?.Invoke();
        }

        public override async UniTask PlayShowAsync(object args)
        {
            await base.PlayShowAsync(args);

            if (_args != null)
            {
                await PlayRewardAnimationsAsync();
                StartCountdown(3);
            }
        }

        private async UniTask PlayRewardAnimationsAsync()
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i] != null && _rewards[i].gameObject.activeSelf)
                {
                    var rt = _rewards[i].transform;
                    var reward = _rewards[i];
                    rt.DOScale(1f, 0.35f)
                        .SetEase(Ease.OutBack);
                    reward.PlayAppearEffect();
                    await UniTask.Delay(120, DelayType.UnscaledDeltaTime);
                }
            }
        }

        private void RefreshRewards(List<ItemData> rewards)
        {
            int displayIndex = 0;

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    var reward = rewards[i];

                    // Reward null thì bỏ qua, vẫn tiếp tục hiển thị item khác.
                    if (reward == null)
                    {
                        Debug.LogWarning($"[PopupRewardView] Reward tại index {i} bị null.");
                        continue;
                    }

                    var itemDisplay = DatabaseManager.Instance.GetDisplayData(reward);

                    // TierInfo null cũng chỉ bỏ qua item đó.
                    if (itemDisplay?.TierInfo == null)
                    {
                        Debug.LogWarning(
                            $"[PopupRewardView] TierInfo bị null. " +
                            $"ItemKey={reward.ItemKey}");

                        continue;
                    }

                    UIRewardQuantity clone;

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
                        if (rewardPrefab == null ||
                            rewardContainer == null)
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
                        itemDisplay.ItemIcon,
                        itemDisplay.TierInfo.border,
                        itemDisplay.TierInfo.background,
                        itemDisplay.TierInfo.tierIcon,
                        reward.Quantity
                    );

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
        
        private void RefreshRewards(List<HeroItemData> rewards)
        {
            int displayIndex = 0;

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    var reward = rewards[i];

                    // Reward null thì bỏ qua, vẫn tiếp tục hiển thị item khác.
                    if (reward == null)
                    {
                        Debug.LogWarning($"[PopupRewardView] Reward tại index {i} bị null.");
                        continue;
                    }
                    
                    UIRewardQuantity clone;

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
                        if (rewardPrefab == null ||
                            rewardContainer == null)
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
                        HeroImageService.GetHeroIcon(reward.HeroId),
                        HeroImageService.GetHeroTierFrame(reward.HeroId),
                        HeroImageService.GetHeroTierBackground(reward.HeroId),
                        HeroImageService.GetHeroTierIcon(reward.HeroId),
                        reward.Quantity
                    );

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
        
        private void RefreshRewards(List<SkillItemData> rewards)
        {
            int displayIndex = 0;

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    var reward = rewards[i];

                    // Reward null thì bỏ qua, vẫn tiếp tục hiển thị item khác.
                    if (reward == null)
                    {
                        Debug.LogWarning($"[PopupRewardView] Reward tại index {i} bị null.");
                        continue;
                    }
                    
                    UIRewardQuantity clone;

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
                        if (rewardPrefab == null ||
                            rewardContainer == null)
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
                    
                    var itemTier = EnumHelper.GradeToItemTier(reward.Grade);
                    var tierInfo = ItemTierVisualImageService.GetItemTierEntry(itemTier);
                    
                    clone.Bind(
                        SkillImageService.GetSkillIcon(reward.SkillId),
                        tierInfo.border,
                        tierInfo.background,
                        tierInfo.tierIcon,
                        reward.Quantity
                    );

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
        
        private void RefreshRewards(List<WeaponItemData> rewards)
        {
            int displayIndex = 0;

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    var reward = rewards[i];

                    // Reward null thì bỏ qua, vẫn tiếp tục hiển thị item khác.
                    if (reward == null)
                    {
                        Debug.LogWarning($"[PopupRewardView] Reward tại index {i} bị null.");
                        continue;
                    }
                    
                    UIRewardQuantity clone;

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
                        if (rewardPrefab == null ||
                            rewardContainer == null)
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
                    
                    var tier = EnumHelper.GradeToItemTier(reward.Grade);
                    var tierInfo = ItemTierVisualImageService.GetItemTierEntry(tier);
                    
                    clone.Bind(
                        DatabaseManager.Instance.GetWeaponDatabase().GetStandard(reward.WeaponId).Icon,
                        tierInfo.border,
                        tierInfo.background,
                        tierInfo.tierIcon,
                        reward.Quantity, reward.Star
                    );

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
                remainingText.text = LocalizationManager.GetText(LocalizationKeys.UI_POPUP_CLOSES, value);
            }
        }

        private void KillCountdown()
        {
            countdownTween?.Kill();
            countdownTween = null;
        }

        private void KillRewardAnimations()
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                if (_rewards[i] != null)
                {
                    _rewards[i].transform.DOKill();
                }
            }
        }

        private void HidePopupRewardView()
        {
            KillCountdown();
            PopupRewardService.Close();
        }

        private void OnDestroy()
        {
            KillCountdown();
            KillRewardAnimations();
        }
    }

    public static class PopupRewardService
    {
        public static bool IsShowing =>
            UIManager.Instance != null &&
            UIManager.Instance.IsOpen<PopupRewardView>();

        public static void Show(
            IReadOnlyList<ItemData> rewards,
            Action onClose = null,
            bool withBackdrop = false)
        {
            ShowAsync(rewards, onClose, withBackdrop).Forget();
        }
        
        public static void ShowHeroItemReward(
            IReadOnlyList<HeroItemData> rewards,
            Action onClose = null,
            bool withBackdrop = false)
        {
            ShowAsync(rewards, onClose, withBackdrop).Forget();
        }
        
        public static void ShowSkillItemReward(
            IReadOnlyList<SkillItemData> rewards,
            Action onClose = null,
            bool withBackdrop = false)
        {
            ShowAsync(rewards, onClose, withBackdrop).Forget();
        }
        
        public static void ShowWeaponItemReward(
            IReadOnlyList<WeaponItemData> rewards,
            Action onClose = null,
            bool withBackdrop = false)
        {
            ShowAsync(rewards, onClose, withBackdrop).Forget();
        }

        private static async UniTask<PopupRewardView> ShowAsync(
            IReadOnlyList<ItemData> rewards,
            Action onClose = null,
            bool withBackdrop = true, PopupRewardType rewardType = PopupRewardType.NormalItem)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[PopupRewardService] UIManager instance was not found.");
                return null;
            }

            var args = new PopupRewardArgs
            {
                Rewards = rewards == null
                    ? new List<ItemData>()
                    : new List<ItemData>(rewards),
                OnClose = onClose,
                RewardType = rewardType
            };

            return await UIManager.Instance.OpenPopupAsync<PopupRewardView>(
                args,
                withBackdrop);
        }
        
        private static async UniTask<PopupRewardView> ShowAsync(
            IReadOnlyList<SkillItemData> rewards,
            Action onClose = null,
            bool withBackdrop = true, PopupRewardType rewardType = PopupRewardType.SkillItem)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[PopupRewardService] UIManager instance was not found.");
                return null;
            }

            var args = new PopupRewardArgs
            {
                SkillItemRewards = rewards == null
                    ? new List<SkillItemData>()
                    : new List<SkillItemData>(rewards),
                OnClose = onClose,
                RewardType = rewardType
            };

            return await UIManager.Instance.OpenPopupAsync<PopupRewardView>(
                args,
                withBackdrop);
        }
        
        private static async UniTask<PopupRewardView> ShowAsync(
            IReadOnlyList<HeroItemData> rewards,
            Action onClose = null,
            bool withBackdrop = true)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[PopupRewardService] UIManager instance was not found.");
                return null;
            }

            var args = new PopupRewardArgs
            {
                HeroItemRewards = rewards == null
                    ? new List<HeroItemData>()
                    : new List<HeroItemData>(rewards),
                OnClose = onClose,
                RewardType = PopupRewardType.HeroItem
            };

            return await UIManager.Instance.OpenPopupAsync<PopupRewardView>(
                args,
                withBackdrop);
        }
        
        private static async UniTask<PopupRewardView> ShowAsync(
            IReadOnlyList<WeaponItemData> rewards,
            Action onClose = null,
            bool withBackdrop = true)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[PopupRewardService] UIManager instance was not found.");
                return null;
            }

            var args = new PopupRewardArgs
            {
                WeaponItemRewards = rewards == null
                    ? new List<WeaponItemData>()
                    : new List<WeaponItemData>(rewards),
                OnClose = onClose,
                RewardType = PopupRewardType.HeroItem
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

    public enum PopupRewardType
    {
        None = 0,
        NormalItem = 1,
        HeroItem = 2,
        SkillItem = 3,
        WeaponItem = 4
    }
}