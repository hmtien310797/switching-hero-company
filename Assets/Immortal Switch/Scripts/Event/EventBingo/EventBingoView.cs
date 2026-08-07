using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventBingo.Layout;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.Helper;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventBingo
{
    public class EventBingoView : AnimatedUIView
    {
        [SerializeField]
        private UIEventBingoLayoutController layoutVertical;

        [SerializeField]
        private Button btnBack;

        private void Awake()
        {
            btnBack.onClick.AddListener(OnClickClose);

            if (ScreenOrientationTracker.Instance != null)
            {
                ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;
            }
        }

        public override async UniTask PlayShowAsync(object args)
        {
            Bind();
            await base.PlayShowAsync(args);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<EventBingoView>();
        }

        private void OnDestroy()
        {
            btnBack.onClick.RemoveListener(OnClickClose);

            if (ScreenOrientationTracker.Instance != null)
            {
                ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
            }
        }

        private void OnEnable()
        {
            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
        }

        public void Bind()
        {
            EventBingoManager.Instance.RefreshAsync().Forget();

            var activity = DatabaseManager.Instance.GetEventIfActive(EventIdConstants.EVENT_BINGO);
            var remainTime = activity == null ? 0 : DateTimeHelper.CalculateRemainTime(DateTime.Now, activity.endTime);

            layoutVertical.Bind(remainTime);
        }

        private void OnOrientationChanged(ScreenOrientationTracker.ScreenViewMode obj)
        {
            switch (obj)
            {
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    layoutVertical.gameObject.SetActive(true);
                    break;

                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    layoutVertical.gameObject.SetActive(true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }
    }
}