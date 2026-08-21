using System;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionRepeatEntry : MonoBehaviour
    {
        [Header("References button")]
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private Button btnChallenge;

        [Header("References overlay")]
        [SerializeField]
        private GameObject goOverlayClaimed;

        [Header("References mission")]
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [SerializeField]
        private Image imgProgress;

        [Header("Reward references")]
        [SerializeField]
        private UIRewardQuantity rewardQuantity;

        // --- Private Fields ---
        private DynamicHeroesGlobalSpecificationsMissionConfigRow _row;

        // moc diem cua nhiem vu
        private float _target;

        // diem hien tai cua nhiem vu
        private int _currentProgress;

        // thuc hien viec jump to trang ui khac
        private Func<string, UniTask> _onJump;

        private void Awake()
        {
            MissionSystemManager.Instance.OnMissionClaimed += OnMissionClaimed;
            MissionSystemManager.Instance.OnChangeProgress += OnMissionChangeProgress;

            btnClaim.onClick.AddListener(OnClickClaim);
            btnChallenge.onClick.AddListener(OnClickChallenge);
        }

        private void OnDestroy()
        {
            if (MissionSystemManager.Instance != null)
            {
                MissionSystemManager.Instance.OnMissionClaimed -= OnMissionClaimed;
                MissionSystemManager.Instance.OnChangeProgress -= OnMissionChangeProgress;
            }

            btnClaim.onClick.RemoveListener(OnClickClaim);
            btnChallenge.onClick.RemoveListener(OnClickChallenge);
        }

        private void OnMissionChangeProgress(string arg1, int arg2, string arg3)
        {
            if (_row == null)
            {
                //Debug.LogError("Row is null");
                return;
            }

            if (_row.missionId != arg3)
            {
                //Debug.Log($"Row not match with {arg3}");
                return;
            }

            SetProgress(arg2);
            RefreshVisual();
        }

        private void OnMissionClaimed(string arg1, string arg2)
        {
            if (_row == null)
            {
                //Debug.LogError("Row is null");
                return;
            }

            if (_row.missionId != arg1)
            {
                //Debug.Log($"Row not match with {arg1}");
                return;
            }

            ApplyStateClaimed();
        }

        private void OnClickClaim()
        {
            MissionSystemManager.Instance.ClaimAndNotify(_row);
        }

        private void OnClickChallenge()
        {
            if (_row == null)
            {
                Debug.LogError("Row is null");
                return;
            }

            _onJump?.Invoke(_row.eventKey);
        }

        public void Bind(DynamicHeroesGlobalSpecificationsMissionConfigRow row, int currentProgress, int rewardItemId,
            BigNumber rewardAmount, Func<string, UniTask> onJump)
        {
            _row = row;
            _target = row.target;
            _onJump = onJump;

            txtTitle.text = LocalizationManager.GetText(row.title, _target);

            rewardQuantity.Bind(rewardItemId, rewardAmount);

            SetProgress(currentProgress);
            RefreshVisual();
        }

        public void SetProgress(int newProgress)
        {
            _currentProgress = newProgress;
            txtProgress.text = $"{Mathf.Min(newProgress, _target)} / {_target:F0}";
            imgProgress.fillAmount = Mathf.Clamp01(newProgress / _target);
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