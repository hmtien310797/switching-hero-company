using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.MissionSystem.Models;
using Immortal_Switch.Scripts.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionSystemRepeatView : MonoBehaviour
    {
        [Header("References prefab")]
        [SerializeField]
        private RectTransform taskContainer;

        [SerializeField]
        private UIMissionRepeatEntry taskPrefab;

        [Header("References button claim all")]
        [SerializeField]
        private Button btnClaimAll;

        [SerializeField]
        private GameObject goBtnClaimAllRedDot;

        // --- Private Fields ---
        private readonly List<UIMissionRepeatEntry> _tasks = new();

        private string _missionType;

        private void Awake()
        {
            MissionSystemManager.Instance.OnMissionClaimed += OnMissionClaimed;
            MissionSystemManager.Instance.OnChangeProgress += OnMissionChangeProgress;
            btnClaimAll.onClick.AddListener(OnClickClaimAll);
        }

        private void OnDestroy()
        {
            MissionSystemManager.Instance.OnMissionClaimed -= OnMissionClaimed;
            MissionSystemManager.Instance.OnChangeProgress -= OnMissionChangeProgress;
        }

        private void OnMissionClaimed(string arg1, string arg2)
        {
            if (_missionType != arg2)
            {
                return;
            }

            var anyCompleted = MissionSystemManager.Instance.AnyCompleted(arg2);

            RefreshBtnClaimAll(anyCompleted);
        }

        private void OnMissionChangeProgress(string arg1, int arg2, string arg3)
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
            List<MissionSystemEntry> tasks,
            string missionType,
            Func<string, UniTask> onJump)
        {
            _missionType = missionType;

            // check btn claim trang thai
            var anyCompleted = MissionSystemManager.Instance.AnyCompleted(_missionType);
            RefreshBtnClaimAll(anyCompleted);
            CreateMissions(rows, tasks, onJump);
        }

        private void CreateMissions(
            List<DynamicHeroesGlobalSpecificationsMissionConfigRow> rows,
            List<MissionSystemEntry> tasks,
            Func<string, UniTask> onJump)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var currentTask = tasks.Find(v => v.Id == rows[i].missionId);

                if (currentTask == null)
                {
                    Debug.LogError($"Mission: {rows[i].missionId} not have task");
                    continue;
                }

                var rewards = DatabaseManager.Instance.GetRewards(rows[i].rewards);
                var reward = rewards.Count > 0 ? rewards[0] : null;

                if (reward == null)
                {
                    Debug.LogError($"Mission: {rows[i].missionId}");
                    continue;
                }

                if (_tasks.Count > i)
                {
                    _tasks[i].gameObject.SetActive(true);
                    _tasks[i].Bind(rows[i], currentTask.Progress, reward.ItemId, reward.Quantity, onJump);

                    if (currentTask.IsClaimed)
                    {
                        _tasks[i].ApplyStateClaimed();
                    }
                }
                else
                {
                    var clone = Instantiate(taskPrefab, taskContainer);
                    clone.gameObject.SetActive(true);
                    clone.Bind(rows[i], currentTask.Progress, reward.ItemId, reward.Quantity, onJump);

                    if (currentTask.IsClaimed)
                    {
                        clone.ApplyStateClaimed();
                    }

                    _tasks.Add(clone);
                }
            }
        }
    }
}