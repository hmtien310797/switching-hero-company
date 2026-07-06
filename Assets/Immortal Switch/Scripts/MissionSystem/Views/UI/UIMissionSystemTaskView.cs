using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.MissionSystem.Models;
using Immortal_Switch.Scripts.TimerSystem;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionSystemTaskView : MonoBehaviour
    {
        [Header("References prefab")] [SerializeField]
        private RectTransform taskContainer;

        [SerializeField] private UIMissionEntry taskPrefab;

        [Header("References button claim all")] [SerializeField]
        private Button btnClaimAll;

        [SerializeField] private GameObject goBtnClaimAllRedDot;

        [Header("References info group")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private UIMissionRewardGroup uiMissionRewardGroup;

        // --- Private Fields ---
        private readonly List<UIMissionEntry> _taskObjects = new();

        private string _missionType;

        private void Awake()
        {
            TimerSystemManager.Instance.OnMinuteTick += OnTimerSystemMinuteTick;
            MissionSystemManager.Instance.OnMissionClaimed += OnMissionSystemMissionClaimed;
            MissionSystemManager.Instance.OnChangeProgress += OnMissionSystemChangeProgress;
            btnClaimAll.onClick.AddListener(OnClickClaimAll);
        }

        private void OnTimerSystemMinuteTick(TimeSpan remain)
        {
            RefreshVisual(_missionType);
        }

        private void OnDestroy()
        {
            MissionSystemManager.Instance.OnChangeProgress -= OnMissionSystemChangeProgress;
        }

        private void OnMissionSystemMissionClaimed(string arg1, string arg2)
        {
            if (_missionType != arg2)
            {
                return;
            }

            var anyCompleted = MissionSystemManager.Instance.AnyCompleted(arg2);
            RefreshBtnClaimAll(anyCompleted);
        }

        private void OnMissionSystemChangeProgress(string arg1, int arg2, string arg3)
        {
            if (_missionType != arg1)
            {
                return;
            }

            var anyCompleted = MissionSystemManager.Instance.AnyCompleted(arg1);
            RefreshBtnClaimAll(anyCompleted);
        }

        private void OnClickClaimAll()
        {
            MissionSystemManager.Instance.ClaimAll(_missionType);
            RefreshBtnClaimAll(false);
        }

        private void RefreshBtnClaimAll(bool active)
        {
            if (active)
            {
                btnClaimAll.interactable = true;
                goBtnClaimAllRedDot.SetActive(true);
            }
            else
            {
                btnClaimAll.interactable = false;
                goBtnClaimAllRedDot.SetActive(false);
            }
        }

        public void Initialize(List<DynamicHeroesGlobalSpecificationsMissionConfigRow> rows,
            List<DynamicHeroesGlobalSpecificationsMissionPointMilesStoneRow> milesStoneRows,
            List<MissionSystemEntry> tasks,
            List<MissionSystemPoint> states,
            string missionType,
            int point,
            Func<string, UniTask> onJump)
        {
            _missionType = missionType;

            // check btn claim trang thai
            var anyCompleted = MissionSystemManager.Instance.AnyCompleted(_missionType);
            RefreshBtnClaimAll(anyCompleted);

            RefreshVisual(missionType);
            _CreateMissions(rows, tasks, missionType, onJump);
            uiMissionRewardGroup.Bind(point, missionType, milesStoneRows, states);
        }

        private void RefreshVisual(string missionType)
        {
            if (missionType == MissionSystemTypes.DAILY)
            {
                var remain = TimerSystemManager.GetRemainingTimeToday();
                Debug.Log($"Remain: {remain}");
                txtTitle.text = string.Format(_GetTitle(missionType), $"{remain.Hours:D2}h{remain.Minutes:D2}m");
            }
            else
            {
                txtTitle.text = _GetTitle(missionType);
            }
        }

        private void _CreateMissions(
            List<DynamicHeroesGlobalSpecificationsMissionConfigRow> rows,
            List<MissionSystemEntry> tasks,
            string missionType,
            Func<string, UniTask> onJump)
        {
            var title = _GetMissionTitle(missionType);

            for (var i = 0; i < rows.Count; i++)
            {
                var currentTask = tasks.Find(v => v.Id == rows[i].missionId);

                if (currentTask == null)
                {
                    Debug.LogError($"MissionType: {missionType} with {JsonConvert.SerializeObject(rows[i])}");
                    continue;
                }

                if (_taskObjects.Count > i)
                {
                    _taskObjects[i].gameObject.SetActive(true);
                    _taskObjects[i].Bind(rows[i], title, currentTask.Progress, onJump);

                    if (currentTask.IsClaimed)
                    {
                        _taskObjects[i].ApplyStateClaimed();
                    }
                }
                else
                {
                    var clone = Instantiate(taskPrefab, taskContainer);
                    clone.gameObject.SetActive(true);
                    clone.Bind(rows[i], title, currentTask.Progress, onJump);

                    if (currentTask.IsClaimed)
                    {
                        clone.ApplyStateClaimed();
                    }

                    _taskObjects.Add(clone);
                }
            }
        }

        private string _GetTitle(string missionType)
        {
            switch (missionType)
            {
                case MissionSystemTypes.WEEKLY:
                    return "Nhiệm Vụ Hằng Tuần Trong 7 Ngày";

                case MissionSystemTypes.DAILY:
                    return "Nhiệm Vụ Hằng Ngày trong {0}";
            }

            return string.Empty;
        }

        private string _GetMissionTitle(string missionType)
        {
            switch (missionType)
            {
                case MissionSystemTypes.WEEKLY:
                    return "[Hằng Tuần]";

                case MissionSystemTypes.DAILY:
                    return "[Hằng Ngày]";
            }

            return string.Empty;
        }
    }
}