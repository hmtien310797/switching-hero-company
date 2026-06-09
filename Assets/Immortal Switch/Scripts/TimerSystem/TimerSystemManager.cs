using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.TimerSystem
{
    public class TimerSystemManager : Singleton<TimerSystemManager>
    {
        /// <summary>
        /// event fire mỗi phút
        /// 1: thoi gian con lai trong ngay
        /// </summary>
        public event Action<TimeSpan> OnMinuteTick;

        // --- Private Fields ---
        private float _nextMinuteTime;

        protected override void OnSingletonAwake()
        {
            _nextMinuteTime = Time.unscaledTime + 60f;
            base.OnSingletonAwake();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextMinuteTime)
            {
                return;
            }

            _nextMinuteTime += 60f;
            var remain = GetRemainingTimeToday();
            OnMinuteTick?.Invoke(remain);
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Thời gian còn lại đến 00:00 ngày tiếp theo.
        /// </summary>
        public static TimeSpan GetRemainingTimeToday()
        {
            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1);
            return tomorrow - now;
        }
    }
}