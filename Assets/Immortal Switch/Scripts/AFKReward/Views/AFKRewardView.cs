using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        /// Base reward rate (per minute) của stage — chỉ dùng để hiển thị dòng "+X/60s",
        /// không phải số quà thực nhận.
        /// </summary>
        public StageReward[] Rewards { get; set; }

        /// <summary>
        /// Số quà thực đã được server cộng vào bag (response.Rewards của afk/claim) —
        /// đây là số hiển thị ở danh sách icon quà.
        /// </summary>
        public StageReward[] EarnedRewards { get; set; }

        /// <summary>
        /// Thời gian AFK thực tế tính bằng server (response.ElapsedSeconds của afk/claim).
        /// </summary>
        public int ElapsedSeconds { get; set; }

        /// <summary>
        /// Trần thời gian tích lũy AFK do server quy định (response.MaxOfflineSeconds).
        /// </summary>
        public int MaxOfflineSeconds { get; set; }
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

        [Header("Auto-claim")]
        [Tooltip("Player không thao tác gì sau ngần này giây thì tự claim (x1) và đóng popup.")]
        [SerializeField]
        private float autoClaimDelaySeconds = 3f;

        // --- Private Fields ---
        private List<UIReward> _rewards = new();
        private AFKRewardArgs _args;
        private CancellationTokenSource _autoClaimCts;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
            btnClaimX2.onClick.AddListener(OnClickClaimX2);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaim);
            btnClaimX2.onClick.RemoveListener(OnClickClaimX2);
            CancelAutoClaim();
        }

        // Tick theo cùng bộ đếm live của TopMainView (txtAfkClaimTimer cạnh btnActiveFramingClaim)
        // thay vì đứng yên tại ElapsedSeconds snapshot lúc mở popup.
        private void Update()
        {
            if (_args != null)
            {
                RefreshCurrentTime();
            }
        }

        private void OnClickClaim()
        {
            CancelAutoClaim();
            _args?.OnClaim?.Invoke(false);
            UIManager.Instance.Close<AFKRewardView>();
        }

        private void OnClickClaimX2()
        {
            if (AFKRewardManager.Instance.RecordClaimX2())
            {
                CancelAutoClaim();
                _args?.OnClaim?.Invoke(true);
            }
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is AFKRewardArgs runtime)
            {
                _args = runtime;

                RefreshRewards(_args.EarnedRewards ?? Array.Empty<StageReward>());
                RefreshBaseRewards(_args.Rewards ?? Array.Empty<StageReward>());
                RefreshCurrentTime();
            }

            RefreshAdsState();
            StartAutoClaimTimer();
        }

        public override void OnHide()
        {
            base.OnHide();
            CancelAutoClaim();
        }

        private void StartAutoClaimTimer()
        {
            CancelAutoClaim();
            _autoClaimCts = new CancellationTokenSource();
            AutoClaimAfterDelay(_autoClaimCts.Token).Forget();
        }

        private void CancelAutoClaim()
        {
            if (_autoClaimCts == null)
                return;

            _autoClaimCts.Cancel();
            _autoClaimCts.Dispose();
            _autoClaimCts = null;
        }

        // Player không bấm Claim/ClaimX2 trong autoClaimDelaySeconds → tự claim x1 rồi đóng popup,
        // tránh popup treo màn hình vô thời hạn.
        private async UniTaskVoid AutoClaimAfterDelay(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(autoClaimDelaySeconds), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            _args?.OnClaim?.Invoke(false);
            UIManager.Instance.Close<AFKRewardView>();
        }

        private void RefreshCurrentTime()
        {
            // Ưu tiên bộ đếm live của TopMainView (cùng giá trị đang hiển thị ở btnActiveFramingClaim
            // ngoài main view) — chỉ fallback về ElapsedSeconds snapshot khi TopMainView chưa sẵn sàng.
            double elapsedSeconds = TopMainView.Instance != null
                ? TopMainView.Instance.AfkAccumulatedSeconds
                : Math.Max(0, _args.ElapsedSeconds);

            double maxOfflineSeconds = TopMainView.Instance != null
                ? TopMainView.Instance.AfkMaxOfflineSeconds
                : _args.MaxOfflineSeconds;

            TimeSpan elapsed = TimeSpan.FromSeconds(elapsedSeconds);
            string timeText = elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours:00} giờ {elapsed.Minutes:00} phút"
                : $"{elapsed.Minutes:00} phút {elapsed.Seconds:00}s";

            int maxHours = maxOfflineSeconds > 0
                ? (int)(maxOfflineSeconds / 3600)
                : 12;

            txtAfkCurrentTime.text =
                timeText + $"\n<color=#afa071><size=32>Tối đa {maxHours} giờ</size></color>";
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