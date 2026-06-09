using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.MissionSystem.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionSystemRepeatView : MonoBehaviour
    {
        [Header("References prefab")] [SerializeField]
        private RectTransform taskContainer;

        [SerializeField] private UIMissionRepeatEntry taskPrefab;

        [Header("References button claim all")] [SerializeField]
        private Button btnClaimAll;

        [SerializeField] private GameObject goBtnClaimAllRedDot;

        // --- Private Fields ---
        private readonly List<UIMissionRepeatEntry> _taskObjects = new();

        private string _missionType;

        private void Awake()
        {
            MissionSystemManager.Instance.OnMissionClaimed += OnMissionSystemMissionClaimed;
            MissionSystemManager.Instance.OnChangeProgress += OnMissionSystemChangeProgress;
            btnClaimAll.onClick.AddListener(OnClickClaimAll);
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

        public void Initialize(
            List<DynamicHeroesGlobalSpecificationsMissionConfigRow> rows,
            MissionSystemEntry[] tasks,
            string missionType,
            Func<string, UniTask> onJump)
        {
            _missionType = missionType;

            // check btn claim trang thai
            var anyCompleted = MissionSystemManager.Instance.AnyCompleted(_missionType);
            RefreshBtnClaimAll(anyCompleted);
            _CreateMissions(rows, tasks, missionType, onJump);
        }

        private void _CreateMissions(List<DynamicHeroesGlobalSpecificationsMissionConfigRow> rows, MissionSystemEntry[] tasks,
            string missionType, Func<string, UniTask> onJump)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var currentTask = Array.Find(tasks, v => v.Id == rows[i].missionId);

                if (currentTask == null)
                {
                    Debug.LogError($"MissionType: {missionType} with {JsonConvert.SerializeObject(rows[i])}");
                    continue;
                }

                if (_taskObjects.Count > i)
                {
                    _taskObjects[i].gameObject.SetActive(true);
                    _taskObjects[i].Bind(rows[i], currentTask.Progress, onJump);

                    if (currentTask.IsClaimed)
                    {
                        _taskObjects[i].ApplyStateClaimed();
                    }
                }
                else
                {
                    var clone = Instantiate(taskPrefab, taskContainer);
                    clone.gameObject.SetActive(true);
                    clone.Bind(rows[i], currentTask.Progress, onJump);

                    if (currentTask.IsClaimed)
                    {
                        clone.ApplyStateClaimed();
                    }

                    _taskObjects.Add(clone);
                }
            }
        }
    }
}