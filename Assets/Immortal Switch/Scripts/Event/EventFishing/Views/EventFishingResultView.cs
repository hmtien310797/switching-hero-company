using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventFishing.Views
{
    public enum EEventFishingResult
    {
        Lose = 0,
        Win = 1,
    }

    public class EventFishingResultArgs
    {
        public readonly EEventFishingResult Result;
        public readonly bool IsFirst;

        public EventFishingResultArgs(EEventFishingResult result, bool isFirst)
        {
            Result = result;
            IsFirst = isFirst;
        }
    }

    public class EventFishingResultView : AnimatedUIView
    {
        [SerializeField]
        private GameObject goWin;

        [SerializeField]
        private GameObject goLose;

        [SerializeField]
        private Button btnClose;

        // --- Private Fields ---
        private EventFishingResultArgs _args;

        private void Awake()
        {
            btnClose.onClick.AddListener(OnClickClose);
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (args is EventFishingResultArgs runtime)
            {
                _args = runtime;

                RefreshVisual();
            }
        }

        private void RefreshVisual()
        {
            if (_args == null)
            {
                return;
            }

            var isWin = _args.Result == EEventFishingResult.Win;

            goLose.SetActive(!isWin);
            goWin.SetActive(isWin);
        }

        private void OnDestroy()
        {
            btnClose.onClick.RemoveListener(OnClickClose);
        }

        private void OnClickClose()
        {
            UIManager.Instance.Close<EventFishingResultView>();
        }
    }
}