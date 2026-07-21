using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong
{
    public class EventLeHoiBangLongView : AnimatedUIView
    {
        [SerializeField]
        private UIEventLeHoiBangLongLayoutController layoutHorizontal;

        [SerializeField]
        private UIEventLeHoiBangLongLayoutController layoutVertical;

        [SerializeField]
        private EventLeHoiBangLongTopLayout topLayout;

        // --- Private Fields ---

        private void Awake()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;
            topLayout.Bind(ChangeLayout, OnClose, OnHelp);
            layoutHorizontal.DisableLayouts();
            layoutVertical.DisableLayouts();
        }

        private void OnHelp()
        {
            UIManager.Instance
                .TogglePopupAsync<PopupEventInfoView>(new PopupEventInfoArgs
                {
                    DescKey = "desc_key",
                    TitleKey = "title_key",
                })
                .Forget();
        }

        private void OnClose()
        {
            UIManager.Instance.TogglePopupAsync<EventLeHoiBangLongView>().Forget();
        }

        private void OnEnable()
        {
            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
            ChangeLayout(EEventLeHoiBangLongLayoutType.Main);
        }

        private void OnDestroy()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
        }

        private void ChangeLayout(EEventLeHoiBangLongLayoutType type)
        {
            layoutHorizontal.ChangeLayout(type);
            layoutVertical.ChangeLayout(type);
        }

        private void OnOrientationChanged(ScreenOrientationTracker.ScreenViewMode obj)
        {
            switch (obj)
            {
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    layoutHorizontal.gameObject.SetActive(false);
                    layoutVertical.gameObject.SetActive(true);
                    topLayout.SetEnableHelp(false);
                    break;

                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    layoutHorizontal.gameObject.SetActive(true);
                    layoutVertical.gameObject.SetActive(false);
                    topLayout.SetEnableHelp(true);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }
    }
}