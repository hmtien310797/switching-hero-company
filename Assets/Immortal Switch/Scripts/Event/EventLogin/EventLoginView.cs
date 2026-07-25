using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventLogin.Controller;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLogin
{
    public class EventLoginArgs
    {
        /// <summary>
        /// event id
        /// </summary>
        public int EventId;

        public EventLoginArgs(int eventId)
        {
            EventId = eventId;
        }
    }

    public class EventLoginView : AnimatedUIView
    {
        [SerializeField]
        private UIEventLoginLayoutController layoutHorizontal;

        [SerializeField]
        private UIEventLoginLayoutController layoutVertical;

        // --- Private Fields ---

        private void Awake()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;
        }

        private void OnDestroy()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
        }

        private void OnOrientationChanged(ScreenOrientationTracker.ScreenViewMode obj)
        {
            switch (obj)
            {
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    layoutVertical.gameObject.SetActive(true);
                    layoutHorizontal.gameObject.SetActive(false);
                    break;

                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    layoutVertical.gameObject.SetActive(false);
                    layoutHorizontal.gameObject.SetActive(true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is not EventLoginArgs runtime)
            {
                return;
            }

            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);

            RefreshAndBindAsync(runtime).Forget();
        }

        /// <summary>Tải state server trước khi bind layout — tránh hiện UI rỗng rồi mới cập nhật,
        /// cùng cách EventLeHoiBangLongView.RefreshAndBindAsync làm.</summary>
        private async UniTaskVoid RefreshAndBindAsync(EventLoginArgs runtime)
        {
            await EventLoginManager.Instance.RefreshAsync(runtime.EventId);

            var state = EventLoginManager.Instance.GetState(runtime.EventId);
            if (state == null)
            {
                return;
            }

            layoutHorizontal.Bind(runtime.EventId, state);
            layoutVertical.Bind(runtime.EventId, state);
        }
    }
}
