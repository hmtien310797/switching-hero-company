using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Event.EventWheel.Layout;
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
        private List<EventWheelTab> tabs = new();

        // --- Private Fields ---
        private EventWheelTab _selectedTab;

        private void Awake()
        {
            BindTabs();
            OnClickTab(0);
        }

        private void BindTabs()
        {
            for (var index = 0; index < tabs.Count; index++)
            {
                var tab = tabs[index];
                tab.preset.Bind(index, tab.localizedKey, OnClickTab);
                tab.preset.SetStatus(ETabPresetStatus.Normal);
            }
        }

        private void OnClickTab(int idx)
        {
            if (_selectedTab != null)
            {
                _selectedTab.preset.SetStatus(ETabPresetStatus.Normal);
                _selectedTab.layout.SetActive(false);
                _selectedTab = null;
            }

            _selectedTab = tabs[idx];
            _selectedTab.preset.SetStatus(ETabPresetStatus.Selected);
            _selectedTab.layout.SetActive(true);
            BindTab();
        }

        private void BindTab()
        {
            if (_selectedTab == null)
            {
                Debug.LogError("[EventWheelView] No tab selected");
                return;
            }

            switch (_selectedTab.tab)
            {
                case EEventWheelTab.Event:
                    _selectedTab.layout.GetComponent<EventLayout>().Bind(5, 50, 5, 50);
                    break;

                case EEventWheelTab.Shop:
                    break;

                case EEventWheelTab.Ticket:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}