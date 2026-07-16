using TMPro;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Immortal_Switch.Scripts.Event.EventWheel.UI
{
    public class UIEventCountdownTimer : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtCountdown;

        // --- Private Fields ---
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

        public void Bind(double remainTime)
        {
            _endTime = DateTime.Now.AddSeconds(Math.Max(0d, remainTime));
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

            var totalMinutes = remainSeconds > 0d
                ? (long)Math.Ceiling(remainSeconds / 60d)
                : 0L;

            var days = totalMinutes / (24L * 60L);
            var hours = totalMinutes / 60L % 24L;
            var minutes = totalMinutes % 60L;

            txtCountdown.text = $"{days:00} ngày {hours:00} giờ {minutes:00} phút";
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