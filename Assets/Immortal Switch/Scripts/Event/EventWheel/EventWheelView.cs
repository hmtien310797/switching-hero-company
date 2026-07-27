using System;
using Immortal_Switch.Scripts.Event.EventWheel.Controller;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel
{
    public enum EEventWheelTab
    {
        /// <summary>
        /// event
        /// </summary>
        Event = 0,

        /// <summary>
        /// shop
        /// </summary>
        Shop = 1,

        /// <summary>
        /// ticket
        /// </summary>
        Ticket = 2,
    }

    public enum EEventWheelShopLimitType
    {
        /// <summary>
        /// tai khoan
        /// </summary>
        Account = 0,

        /// <summary>
        /// ngay
        /// </summary>
        Daily = 1,
    }

    [Serializable]
    public class EventWheelTab
    {
        public EEventWheelTab tab;
        public string localizedKey;
        public UITabPreset preset;
        public GameObject layout;
    }

    public class EventWheelView : AnimatedUIView
    {
        [SerializeField]
        private UIEventWheelLayoutController layoutHorizontal;

        [SerializeField]
        private UIEventWheelLayoutController layoutVertical;

        // --- Public Fields ---
        public bool IsRolling => layoutVertical.IsRolling || layoutHorizontal.IsRolling;

        private void Awake()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged += OnOrientationChanged;
        }

        private void OnDestroy()
        {
            ScreenOrientationTracker.Instance.OnOrientationChanged -= OnOrientationChanged;
        }

        private void OnEnable()
        {
            OnOrientationChanged(ScreenOrientationTracker.Instance.CurrentMode);
        }

        private void OnOrientationChanged(ScreenOrientationTracker.ScreenViewMode obj)
        {
            switch (obj)
            {
                case ScreenOrientationTracker.ScreenViewMode.Portrait:
                    layoutHorizontal.gameObject.SetActive(false);
                    layoutVertical.gameObject.SetActive(true);
                    break;

                case ScreenOrientationTracker.ScreenViewMode.Landscape:
                    layoutHorizontal.gameObject.SetActive(true);
                    layoutVertical.gameObject.SetActive(false);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(obj), obj, null);
            }
        }
    }
}