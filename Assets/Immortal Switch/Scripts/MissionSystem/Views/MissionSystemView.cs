using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.MissionSystem.Views.UI;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem.Views
{
    public class MissionSystemView : AnimatedUIView
    {
        [Header("Tabs")]
        [SerializeField]
        private UIMissionSystemTaskView dailyView;

        [SerializeField]
        private UIMissionSystemTaskView weeklyView;

        [SerializeField]
        private UIMissionSystemRepeatView repeatView;

        [SerializeField]
        private SegmentedControlOption dailyOption;

        [SerializeField]
        private SegmentedControlOption weeklyOption;

        [SerializeField]
        private SegmentedControlOption repeatOption;

        // --- Private Fields ---
        // loai tab da chon
        private string _selectedMissionType;

        private void Awake()
        {
            dailyOption.Bind(() => OnClickOption(MissionTypes.DAILY));
            weeklyOption.Bind(() => OnClickOption(MissionTypes.WEEKLY));
            repeatOption.Bind(() => OnClickOption(MissionTypes.REPEAT));
            OnClickOption(MissionTypes.DAILY);
        }

        private void OnClickOption(string missionType)
        {
            if (!string.IsNullOrWhiteSpace(_selectedMissionType))
            {
                SetActive(_selectedMissionType, false);
            }

            SetActive(missionType, true);
            RefreshVisual(missionType);
        }

        private void SetActive(string missionType, bool value)
        {
            switch (missionType)
            {
                case MissionTypes.DAILY:
                    dailyView.gameObject.SetActive(value);
                    dailyOption.SetSelected(value);

                    weeklyView.gameObject.SetActive(!value);
                    weeklyOption.SetSelected(!value);

                    repeatView.gameObject.SetActive(!value);
                    repeatOption.SetSelected(!value);
                    break;

                case MissionTypes.WEEKLY:
                    dailyView.gameObject.SetActive(!value);
                    dailyOption.SetSelected(!value);

                    weeklyView.gameObject.SetActive(value);
                    weeklyOption.SetSelected(value);

                    repeatView.gameObject.SetActive(!value);
                    repeatOption.SetSelected(!value);
                    break;

                case MissionTypes.REPEAT:
                    dailyView.gameObject.SetActive(!value);
                    dailyOption.SetSelected(!value);

                    weeklyView.gameObject.SetActive(!value);
                    weeklyOption.SetSelected(!value);

                    repeatView.gameObject.SetActive(value);
                    repeatOption.SetSelected(value);
                    break;
            }
        }

        private UniTask JumpTo(string eventKey)
        {
            UIManager.Instance.TogglePopupAsync<MissionSystemView>().Forget();
            return UniTask.CompletedTask;
        }

        private void RefreshVisual(string missionType)
        {
            var missions = MissionSystemManager.Instance.GetMissions(missionType);
            var tasks = MissionSystemManager.Instance.GetTasks(missionType);

            switch (missionType)
            {
                case MissionTypes.DAILY:
                {
                    var milestone = MissionSystemManager.Instance.GetMilesStone(missionType);
                    var point = MissionSystemManager.Instance.GetPoint(missionType);
                    var states = MissionSystemManager.Instance.GetStates(missionType);
                    dailyView.Initialize(missions, milestone, tasks, states, missionType, point, JumpTo);
                    break;
                }

                case MissionTypes.WEEKLY:
                {
                    var milestone = MissionSystemManager.Instance.GetMilesStone(missionType);
                    var point = MissionSystemManager.Instance.GetPoint(missionType);
                    var states = MissionSystemManager.Instance.GetStates(missionType);
                    weeklyView.Initialize(missions, milestone, tasks, states, missionType, point, JumpTo);
                    break;
                }

                case MissionTypes.REPEAT:
                    repeatView.Initialize(missions, tasks, missionType, JumpTo);
                    break;
            }
        }
    }
}