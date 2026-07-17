using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Leaderboard.Views.UI
{
    public class UILeaderboardCountdown : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtRefreshCountdown;

        // --- Private Fields ---
        private Action _onRefresh;
        private CancellationTokenSource _countdownCancellation;
        private DateTime _endTimeUtc;

        private bool _isBound;
        private bool _refreshInvoked;

        private void OnEnable()
        {
            if (_isBound)
            {
                StartCountdown();
            }
        }

        private void OnDisable()
        {
            StopCountdown();
        }

        private void OnDestroy()
        {
            StopCountdown();
        }

        public void Bind(double remainTime, Action onRefresh)
        {
            _endTimeUtc = DateTime.UtcNow.AddSeconds(Math.Max(0d, remainTime));
            _onRefresh = onRefresh;

            _isBound = true;
            _refreshInvoked = false;

            StartCountdown();
        }

        /// <summary>Bắt đầu vòng lặp cập nhật thời gian mỗi giây.</summary>
        private void StartCountdown()
        {
            StopCountdown();

            _countdownCancellation = new CancellationTokenSource();

            if (UpdateCountdown())
            {
                UpdateEverySecondAsync(_countdownCancellation.Token).Forget();
            }
        }

        /// <summary>Cập nhật thời gian hiển thị mỗi giây cho đến khi hết hạn.</summary>
        private async UniTaskVoid UpdateEverySecondAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var isCanceled = await UniTask
                    .Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();

                if (isCanceled || !UpdateCountdown())
                {
                    return;
                }
            }
        }

        /// <summary>Cập nhật nội dung và trả về countdown còn tiếp tục hay không.</summary>
        private bool UpdateCountdown()
        {
            var remainSeconds = Math.Max(0d, (_endTimeUtc - DateTime.UtcNow).TotalSeconds);

            var totalSeconds = remainSeconds > 0d
                ? (long)Math.Ceiling(remainSeconds)
                : 0L;

            var minutes = totalSeconds / 60L;
            var seconds = totalSeconds % 60L;

            txtRefreshCountdown.text = $"Cập nhật bảng xếp hạng vào {minutes:00}m{seconds:00}s";

            if (totalSeconds > 0L)
            {
                return true;
            }

            InvokeRefreshOnce();
            return false;
        }

        /// <summary>Gọi callback refresh đúng một lần khi thời gian kết thúc.</summary>
        private void InvokeRefreshOnce()
        {
            if (_refreshInvoked)
            {
                return;
            }

            _refreshInvoked = true;

            var onRefresh = _onRefresh;

            StopCountdown();
            onRefresh?.Invoke();
        }

        /// <summary>Dừng vòng lặp countdown hiện tại.</summary>
        private void StopCountdown()
        {
            if (_countdownCancellation == null)
            {
                return;
            }

            _countdownCancellation.Cancel();
            _countdownCancellation.Dispose();

            _countdownCancellation = null;
        }
    }
}