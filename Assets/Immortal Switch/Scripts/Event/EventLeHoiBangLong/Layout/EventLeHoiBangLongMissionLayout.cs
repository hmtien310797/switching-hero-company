using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI;
using Immortal_Switch.Scripts.Shared.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout
{
    public class EventLeHoiBangLongMissionLayout : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [Header("Milestone references")]
        [SerializeField]
        private RectTransform milestoneContainer;

        [SerializeField]
        private UIEventLeHoiBangLongMilestoneTarget milestonePrefab;

        [Header("Mission references")]
        [SerializeField]
        private RectTransform missionContainer;

        [SerializeField]
        private UIEventLeHoiBangLongMissionItem missionPrefab;

        [SerializeField]
        private TextMeshProUGUI txtCurrentPoint;

        [Header("Progress references")]
        [SerializeField]
        private Image imgFill;

        // --- Private Fields ---
        private SimpleUIPool<UIEventLeHoiBangLongMissionItem> _missionPools;
        private SimpleUIPool<UIEventLeHoiBangLongMilestoneTarget> _milestonePools;
        private List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> _missions = new();
        private List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow> _milestones = new();

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaimMilestone);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaimMilestone);
        }

        private void OnClickClaimMilestone()
        {
            OnClickClaimMilestoneAsync().Forget();
        }

        private async UniTaskVoid OnClickClaimMilestoneAsync()
        {
            var rewards = await EventLeHoiBangLongManager.Instance.ClaimAvailableMissionMilestones();

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void OnEnable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged += RefreshState;
        }

        private void OnDisable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged -= RefreshState;
        }

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> missions,
            List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow> milestones
        )
        {
            _missions = missions ?? new List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow>();
            _milestones = milestones ?? new List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow>();

            RefreshState();
        }

        private void RefreshMissions(List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> missions)
        {
            _missionPools ??= new SimpleUIPool<UIEventLeHoiBangLongMissionItem>(missionPrefab, missionContainer);

            for (int i = 0; i < missions.Count; i++)
            {
                var mission = missions[i];
                var clone = _missionPools.Get(i);
                var state = EventLeHoiBangLongManager.Instance.FindMission(mission.missionId);

                clone.Bind(
                    mission,
                    state?.Progress ?? 0,
                    state?.IsClaimed ?? false,
                    OnClaimMission,
                    i == missions.Count - 1
                );
            }

            _missionPools.ReleaseFrom(missions.Count);
        }

        private void RefreshMilestones(
            List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow> milestones
        )
        {
            _milestonePools ??= new SimpleUIPool<UIEventLeHoiBangLongMilestoneTarget>(milestonePrefab, milestoneContainer);

            for (int i = 0; i < milestones.Count; i++)
            {
                var milestone = milestones[i];
                var clone = _milestonePools.Get(i);
                var isClaimed = EventLeHoiBangLongManager.Instance.IsMissionMilestoneClaimed(milestone.milestone);

                clone.Bind(milestone.itemId1, milestone.pointsRequired);
                clone.SetClaimed(isClaimed);
            }

            _milestonePools.ReleaseFrom(milestones.Count);
        }

        private void OnClaimMission(DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow row)
        {
            OnClaimMissionAsync(row).Forget();
        }

        private async UniTaskVoid OnClaimMissionAsync(DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow row)
        {
            var rewards = await EventLeHoiBangLongManager.Instance.ClaimMission(row.missionId);

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void RefreshState()
        {
            var currentPoint = EventLeHoiBangLongManager.Instance.State?.Progress?.MissionPoints ?? 0;
            var maxValue = _milestones.LastOrDefault()?.pointsRequired ?? 1;

            imgFill.fillAmount = maxValue <= 0 ? 0f : currentPoint / (maxValue * 1f);
            txtCurrentPoint.text = $"Điểm hiện tại {currentPoint:N0}";

            btnClaim.interactable = _milestones.Any(milestone =>
                currentPoint >= milestone.pointsRequired &&
                !EventLeHoiBangLongManager.Instance.IsMissionMilestoneClaimed(milestone.milestone)
            );

            RefreshMissions(_missions);
            RefreshMilestones(_milestones);
        }
    }
}
