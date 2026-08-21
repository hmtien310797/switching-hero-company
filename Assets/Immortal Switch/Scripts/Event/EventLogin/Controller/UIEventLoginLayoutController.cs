using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventLogin.Layout;
using Immortal_Switch.Scripts.Event.EventLogin.UI;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLogin.Controller
{
    public class UIEventLoginLayoutController : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtCountdown;

        [SerializeField]
        private UIEventLoginProgressPanel progressPanel;

        [SerializeField]
        private UIEventLoginDayPanel dayPanel;

        [SerializeField]
        private Button btnClaim;

        [Header("Mission references")]
        [SerializeField]
        private RectTransform missionContainer;

        [SerializeField]
        private UIEventLoginMissionItem missionPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIEventLoginMissionItem> _pools;

        private int _eventId;
        private int _selectedDay;
        private int _currentDay;
        private int _totalDay;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaimMission);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaimMission);
        }

        private void OnEnable()
        {
            EventLoginManager.Instance.OnDataChanged += OnEventLoginDataChanged;
        }

        private void OnDisable()
        {
            EventLoginManager.Instance.OnDataChanged -= OnEventLoginDataChanged;
        }

        private void OnClickClaimMission()
        {
            ClaimAllMissionsAsync().Forget();
        }

        private async UniTaskVoid ClaimAllMissionsAsync()
        {
            var rewards = await EventLoginManager.Instance.ClaimAllMissions(_eventId, _selectedDay);

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        public void Bind(int eventId, EventLoginStateResponse state)
        {
            _eventId = eventId;
            _selectedDay = state.Progress.CurrentDay;
            _currentDay = state.Progress.CurrentDay;
            _totalDay = state.Progress.TotalDay;

            txtTitle.text = LocalizationManager.GetText(LocalizationKeys.UI_NEWBIE_DAY, _totalDay);

            txtCountdown.text =
                LocalizationManager.GetText(LocalizationKeys.UI_EVENT_DURATION, $"{(_totalDay - _currentDay):00}");

            OnChangeDay(_currentDay);
            progressPanel.Bind(state.Milestones, state.Progress.Points, OnClaimAllMilestones, OnClaimMilestone);
            dayPanel.Bind(_totalDay, _currentDay, OnChangeDay);
        }

        private void OnChangeDay(int day)
        {
            _selectedDay = day;

            var state = EventLoginManager.Instance.GetState(_eventId);

            var missions = state?.Missions?
                               .Where(m => m.Day == day)
                               .OrderBy(m => m.SortOrder)
                               .ToList() ??
                           new List<EventLoginMissionDto>();

            RefreshMissions(missions);
            RefreshClaimMissionButton(missions);
        }

        private void RefreshMissions(List<EventLoginMissionDto> missions)
        {
            _pools ??= new SimpleUIPool<UIEventLoginMissionItem>(missionPrefab, missionContainer);

            for (int i = 0; i < missions.Count; i++)
            {
                var mission = missions[i];
                var clone = _pools.Get(i);
                var isUnlockedDay = mission.Day <= _currentDay;

                clone.Bind(
                    i + 1,
                    mission,
                    isUnlockedDay,
                    OnClaimMission
                );
            }

            _pools.ReleaseFrom(missions.Count);
        }

        private void OnClaimMission(EventLoginMissionDto mission)
        {
            ClaimMissionAsync(mission.MissionId).Forget();
        }

        private async UniTaskVoid ClaimMissionAsync(string missionId)
        {
            var rewards = await EventLoginManager.Instance.ClaimMission(_eventId, missionId);

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void OnClaimMilestone(int milestoneId)
        {
            ClaimMilestoneAsync(milestoneId).Forget();
        }

        private async UniTaskVoid ClaimMilestoneAsync(int milestoneId)
        {
            var rewards = await EventLoginManager.Instance.ClaimMilestone(_eventId, milestoneId);

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void OnClaimAllMilestones()
        {
            ClaimAllMilestonesAsync().Forget();
        }

        private async UniTaskVoid ClaimAllMilestonesAsync()
        {
            var rewards = await EventLoginManager.Instance.ClaimAllMilestones(_eventId);

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void RefreshClaimMissionButton(List<EventLoginMissionDto> missions)
        {
            btnClaim.interactable = missions.Any(m => !m.IsClaimed && m.Progress >= m.Target);
        }

        private void OnEventLoginDataChanged(int eventId)
        {
            var state = EventLoginManager.Instance.GetState(eventId);

            if (eventId != _eventId ||
                state == null)
            {
                return;
            }

            _totalDay = state.Progress.TotalDay;

            txtCountdown.text =
                $"Thời gian sự kiện: <color=#75ce80>{Mathf.Max(0, _totalDay - state.Progress.CurrentDay):00} ngày</color>";

            progressPanel.Bind(state.Milestones, state.Progress.Points, OnClaimAllMilestones, OnClaimMilestone);

            if (state.Progress.CurrentDay != _currentDay)
            {
                _currentDay = state.Progress.CurrentDay;
                _selectedDay = _currentDay;

                dayPanel.Bind(_totalDay, _currentDay, OnChangeDay);
            }
            else
            {
                OnChangeDay(_selectedDay);
            }
        }
    }
}