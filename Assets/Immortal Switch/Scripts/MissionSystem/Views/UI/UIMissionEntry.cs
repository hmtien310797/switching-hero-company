using System;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionEntry : MonoBehaviour
    {
        [Header("References button")] [SerializeField]
        private Button btnClaim;

        [SerializeField] private Button btnChallenge;

        [Header("References overlay")] [SerializeField]
        private GameObject goOverlayClaimed;

        [Header("References mission")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private TextMeshProUGUI txtDesc;
        [SerializeField] private TextMeshProUGUI txtProgress;
        [SerializeField] private TextMeshProUGUI txtQuantityReward;
        [SerializeField] private Image imgProgress;

        [Header("References sprites")] [SerializeField]
        private Image imgReward;

        [PreviewField] [SerializeField] private Sprite sprIconDaily;
        [PreviewField] [SerializeField] private Sprite sprIconWeekly;

        // --- Private Fields ---
        public DynamicHeroesGlobalSpecificationsMissionConfigRow Row { get; private set; }

        // moc diem cua nhiem vu
        private float _target;

        // diem hien tai cua nhiem vu
        private int _currentProgress;

        // thuc hien viec jump to trang ui khac
        private Func<string, UniTask> _onJump;

        private void Awake()
        {
            MissionSystemManager.Instance.OnMissionClaimed += OnMissionSystemMissionClaimed;
            MissionSystemManager.Instance.OnChangeProgress += OnMissionSystemChangeProgress;

            btnClaim.onClick.AddListener(OnClickClaim);
            btnChallenge.onClick.AddListener(OnClickChallenge);
        }

        private void OnMissionSystemChangeProgress(string arg1, int arg2, string arg3)
        {
            if (Row == null)
            {
                //Debug.LogError("Row is null");
                return;
            }

            if (Row.missionId != arg3)
            {
                //Debug.Log($"Row not match with {arg3}");
                return;
            }

            SetProgress(arg2);
            RefreshVisual();
        }

        private void OnMissionSystemMissionClaimed(string arg1, string arg2)
        {
            if (Row == null)
            {
                //Debug.LogError("Row is null");
                return;
            }

            if (Row.missionId != arg1)
            {
                //Debug.Log($"Row not match with {arg1}");
                return;
            }

            ApplyStateClaimed();
        }

        private void OnClickClaim()
        {
            MissionSystemManager.Instance.MissionClaimAndNotify(Row);
        }

        private void OnClickChallenge()
        {
            if (Row == null)
            {
                Debug.LogError("Row is null");
                return;
            }

            _onJump?.Invoke(Row.eventKey);
        }

        public void Bind(DynamicHeroesGlobalSpecificationsMissionConfigRow row, string title, int currentProgress,
            Func<string, UniTask> onJump)
        {
            Row = row;
            _target = row.target;
            _onJump = onJump;

            txtTitle.text = title;
            txtDesc.text = row.title;
            txtQuantityReward.text = row.points.ToString();

            SetProgress(currentProgress);
            RefreshVisual();
            RefreshIcon();
        }

        public void SetProgress(int currentProgress)
        {
            _currentProgress = currentProgress;
            txtProgress.text = $"{Mathf.Min(currentProgress, _target)} / {_target:F0}";
            imgProgress.fillAmount = Mathf.Clamp01(currentProgress / _target);
        }

        public void RefreshIcon()
        {
            imgReward.sprite = Row.type == MissionTypes.DAILY ? sprIconDaily : sprIconWeekly;
        }

        public void RefreshVisual()
        {
            if (_currentProgress >= _target)
            {
                ApplyStateCanClaim();
            }
            else
            {
                ApplyStateChallenge();
            }
        }

        private void ApplyStateCanClaim()
        {
            goOverlayClaimed.SetActive(false);
            btnClaim.gameObject.SetActive(true);
            btnChallenge.gameObject.SetActive(false);
        }

        private void ApplyStateChallenge()
        {
            goOverlayClaimed.SetActive(false);
            btnClaim.gameObject.SetActive(false);
            btnChallenge.gameObject.SetActive(true);
        }

        public void ApplyStateClaimed()
        {
            goOverlayClaimed.SetActive(true);
            btnClaim.gameObject.SetActive(false);
            btnChallenge.gameObject.SetActive(false);
        }
    }
}