using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public class UICountdownTimer : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtCountdown;

        // --- Private Fields ---
        /// <summary>
        /// callback được gọi để trả về string countdown tương ứng.
        /// 1: ngày
        /// 2: giờ
        /// 3: phút
        /// 4: giây
        /// </summary>
        private Func<long, long, long, long, string> _onCountdown;

        private CancellationTokenSource _countdownCts;
        private DateTime _endTime;
        private bool _isBound;

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

        public void Bind(double remainTime, Func<long, long, long, long, string> onCountdown)
        {
            _endTime = DateTime.Now.AddSeconds(Math.Max(0d, remainTime));
            _onCountdown = onCountdown;
            _isBound = true;

            StartCountdown();
        }

        /// <summary>Bắt đầu cập nhật thời gian còn lại mỗi phút.</summary>
        private void StartCountdown()
        {
            StopCountdown();

            _countdownCts = new CancellationTokenSource();

            UpdateDisplay();
            UpdateEveryMinuteAsync(_countdownCts.Token).Forget();
        }

        /// <summary>Cập nhật nội dung đếm ngược mỗi phút cho đến khi UI bị đóng.</summary>
        private async UniTaskVoid UpdateEveryMinuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var isCanceled = await UniTask
                    .Delay(TimeSpan.FromMinutes(1), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();

                if (isCanceled)
                {
                    return;
                }

                UpdateDisplay();
            }
        }

        /// <summary>Hiển thị thời gian còn lại theo định dạng ngày, giờ và phút.</summary>
        private void UpdateDisplay()
        {
            var remainSeconds = Math.Max(0d, (_endTime - DateTime.Now).TotalSeconds);
            var totalSeconds = (long)Math.Ceiling(remainSeconds);

            var days = totalSeconds / 86400L;
            var hours = totalSeconds / 3600L % 24L;
            var minutes = totalSeconds / 60L % 60L;
            var seconds = totalSeconds % 60L;
            var countdown = _onCountdown?.Invoke(days, hours, minutes, seconds);

            if (!string.IsNullOrWhiteSpace(countdown))
            {
                txtCountdown.text = countdown;
            }
        }

        /// <summary>Dừng vòng lặp cập nhật thời gian hiện tại.</summary>
        private void StopCountdown()
        {
            if (_countdownCts == null)
            {
                return;
            }

            _countdownCts.Cancel();
            _countdownCts.Dispose();
            _countdownCts = null;
        }
    }
}