using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public enum ECountdownUpdateUnit
    {
        Minute = 0,
        Hour = 1,
        Second = 2,
    }

    public class UICountdownTimer : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtCountdown;

        [Header("Update Settings")]
        [SerializeField]
        private ECountdownUpdateUnit updateUnit = ECountdownUpdateUnit.Minute;

        [SerializeField]
        [Min(0.01f)]
        private float updateElapsed = 1f;

        [SerializeField]
        private UnityEvent onCountdownCompleted;

        // --- Private Fields ---
        /// <summary>
        /// callback được gọi để trả về string countdown tương ứng.
        /// 1: ngày
        /// 2: giờ
        /// 3: phút
        /// 4: giây
        /// </summary>
        private Func<long, long, long, long, string> _onCountdown;

        private Action _onCountdownCompleted;
        private DateTime _endTime;

        private CancellationTokenSource _countdownCts;

        private bool _isBound;
        private bool _hasCompleted;

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

        /// <summary>
        /// Gán thời gian, callback định dạng và callback runtime được gọi khi countdown kết thúc.
        /// </summary>
        public void Bind(
            double remainTime,
            Func<long, long, long, long, string> onCountdown,
            Action onCompleted = null
        )
        {
            _endTime = DateTime.Now.AddSeconds(Math.Max(0d, remainTime));
            _onCountdown = onCountdown;
            _onCountdownCompleted = onCompleted;
            _isBound = true;
            _hasCompleted = false;

            StartCountdown();
        }

        /// <summary>Bắt đầu cập nhật thời gian còn lại theo chu kỳ đã cấu hình.</summary>
        private void StartCountdown()
        {
            StopCountdown();

            _countdownCts = new CancellationTokenSource();

            if (UpdateDisplay())
            {
                return;
            }

            UpdatePeriodicallyAsync(_countdownCts.Token).Forget();
        }

        /// <summary>Cập nhật nội dung đếm ngược theo đơn vị và khoảng thời gian đã cấu hình.</summary>
        private async UniTaskVoid UpdatePeriodicallyAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var isCanceled = await UniTask
                    .Delay(GetUpdateInterval(), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();

                if (isCanceled)
                {
                    return;
                }

                if (UpdateDisplay())
                {
                    return;
                }
            }
        }

        /// <summary>Chuyển cấu hình đơn vị và elapsed thành khoảng thời gian giữa hai lần cập nhật.</summary>
        private TimeSpan GetUpdateInterval()
        {
            var elapsed = Math.Max(0.01d, updateElapsed);

            return updateUnit switch
            {
                ECountdownUpdateUnit.Hour => TimeSpan.FromHours(elapsed),
                ECountdownUpdateUnit.Second => TimeSpan.FromSeconds(elapsed),
                _ => TimeSpan.FromMinutes(elapsed),
            };
        }

        /// <summary>Hiển thị thời gian còn lại và trả về true khi countdown đã kết thúc.</summary>
        private bool UpdateDisplay()
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

            if (totalSeconds > 0L)
            {
                return false;
            }

            if (!_hasCompleted)
            {
                _hasCompleted = true;
                var runtimeCallback = _onCountdownCompleted;

                onCountdownCompleted?.Invoke();
                runtimeCallback?.Invoke();
            }

            return true;
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