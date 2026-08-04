using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventDice.Layout;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventDice
{
    public class EventDiceView : AnimatedUIView
    {
        [SerializeField]
        private UIEventDiceLayoutController layoutVertical;

        [SerializeField]
        private Button btnBack;

        private void Awake()
        {
            btnBack.onClick.AddListener(OnClickClose);
            ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;
        }

        public override async UniTask PlayShowAsync(object args)
        {
            await EventDiceManager.Instance.RefreshAsync();
            Bind();
            await base.PlayShowAsync(args);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<EventDiceView>();
        }

        private void OnDestroy()
        {
            btnBack.onClick.RemoveListener(OnClickClose);
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
        }

        private void OnEnable()
        {
            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
        }

        public void Bind()
        {
            var manager = EventDiceManager.Instance;
            var boardRows = DatabaseManager.Instance.GetEventDiceBoard();

            layoutVertical.Bind(boardRows,
                manager.GetPendingRewards(),
                manager.CurrentIndex,
                manager.RollAsync
            );
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