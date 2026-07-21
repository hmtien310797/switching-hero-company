using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.Shared.Views;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout
{
    [Serializable]
    public class EventLeHoiBangLongMainLayoutTab
    {
        public EEventLeHoiBangLongLayoutType type;
        public UITabPreset tab;
    }

    public class EventLeHoiBangLongMainLayout : MonoBehaviour
    {
        [SerializeField]
        private List<EventLeHoiBangLongMainLayoutTab> tabs = new();

        [SerializeField]
        private UIEventLeHoiBangLongMainMilestone milestone;

        [SerializeField]
        private UICountdownTimer countdownTimer;

        // --- Private Fields ---
        private Action<EEventLeHoiBangLongLayoutType> _onChangeLayout;
        private EventLeHoiBangLongMainLayoutTab _selectedTab;
        private List<DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow> _milestones = new();

        private void OnEnable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged += RefreshState;
        }

        private void OnDisable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged -= RefreshState;
        }

        private void Awake()
        {
            BindTabs();
            OnClickTab(0);
        }

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow> milestones,
            Action<EEventLeHoiBangLongLayoutType> onChangeLayout,
            double remainTime
        )
        {
            _onChangeLayout = onChangeLayout;
            _milestones = milestones ?? new List<DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow>();

            countdownTimer.Bind(remainTime, OnCountdown);
            RefreshState();
        }

        private string OnCountdown(long days, long hours, long minutes, long seconds)
        {
            return $"Kết thúc sau: {days:00} ngày {hours:00}:{minutes:00}:{seconds:00}";
        }

        private void OnClickClaimMilestone()
        {
            var rewards = EventLeHoiBangLongManager.Instance.ClaimAvailableSummonMilestones();

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void RefreshState()
        {
            var maxPointRequired = _milestones.LastOrDefault()?.pointsRequired ?? 0;

            milestone.Bind(
                EventLeHoiBangLongManager.Instance.Storage.Data.summonPoints,
                maxPointRequired,
                OnClickClaimMilestone,
                _milestones
            );
        }

        private void BindTabs()
        {
            for (var index = 0; index < tabs.Count; index++)
            {
                var tab = tabs[index];
                tab.tab.Bind(index, string.Empty, OnClickTab);
                tab.tab.SetStatus(ETabPresetStatus.Normal);
            }
        }

        private void OnClickTab(int tabIdx)
        {
            if (tabIdx < 0 ||
                tabs.Count < tabIdx)
            {
                Debug.LogError($"[EventLeHoiBangLong] idx: {tabIdx} > tabs: {tabs.Count}");
                return;
            }

            if (_selectedTab != null)
            {
                _selectedTab.tab.SetStatus(ETabPresetStatus.Normal);
                _selectedTab = null;
            }

            _selectedTab = tabs[tabIdx];

            _onChangeLayout?.Invoke(_selectedTab.type);
            _selectedTab.tab.SetStatus(ETabPresetStatus.Selected);
        }
    }
}