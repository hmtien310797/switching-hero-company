using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.MissionSystem.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionRewardGroup : MonoBehaviour
    {
        [Header("References reward")]
        [SerializeField]
        private UIMissionTotalPoint uiMissionTotalPoint;

        [SerializeField]
        private Image imgProgress;

        [SerializeField]
        private List<UIMissionPoint> points = new();

        [SerializeField]
        private List<UIMissionRewardGroupEntry> rewardGroupEntries = new();

        // --- Private Fields ---
        private string _missionType;

        // diem group max
        private int _maxPoint;

        private void Awake()
        {
            MissionSystemManager.Instance.OnRewardGroupClaimed += OnMissionSystemRewardGroupClaimed;
            MissionSystemManager.Instance.OnChangePoint += OnMissionSystemChangePoint;
        }

        private void OnMissionSystemChangePoint(int arg1, string arg2)
        {
            if (_missionType == arg2)
            {
                RefreshPoint(arg1, arg2);
            }
        }

        private void OnDestroy()
        {
            MissionSystemManager.Instance.OnRewardGroupClaimed -= OnMissionSystemRewardGroupClaimed;
            MissionSystemManager.Instance.OnChangePoint -= OnMissionSystemChangePoint;
        }

        private void OnMissionSystemRewardGroupClaimed(List<MissionSystemPoint> arg1, string arg2)
        {
            foreach (var entry in rewardGroupEntries)
            {
                var state = arg1.Find(v => v.Target == entry.Row.pointThreshold);

                if (state != null)
                {
                    entry.BindState(state);
                }
            }
        }

        public void Bind(
            int point,
            string missionType,
            List<DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow> rows,
            List<MissionSystemPoint> states
        )
        {
            _missionType = missionType;
            _maxPoint = rows[^1].pointThreshold;

            RefreshRewardGroup(rows, states);
            RefreshPoint(point, missionType);
        }

        private void RefreshPoint(int point, string missionType)
        {
            uiMissionTotalPoint.Bind(point, missionType);
            imgProgress.fillAmount = Mathf.Clamp01(1f * point / _maxPoint);
        }

        private void RefreshRewardGroup(
            List<DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow> rows,
            List<MissionSystemPoint> states
        )
        {
            for (var idx = 0; idx < rows.Count; idx++)
            {
                if (points.Count > idx)
                {
                    points[idx].Bind(rows[idx].pointThreshold);
                }

                if (rewardGroupEntries.Count > idx)
                {
                    var state = states.Find(v => v.Target == rows[idx].pointThreshold);
                    rewardGroupEntries[idx].BindState(state);
                    rewardGroupEntries[idx].Bind(rows[idx]);
                }
            }
        }
    }
}