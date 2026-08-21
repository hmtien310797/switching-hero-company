using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Controller;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Popup;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Layout
{
    public class EventLeHoiBangLongSummonLayout : MonoBehaviour
    {
        [SerializeField]
        private UICountdownTimer countdownTimer;

        [SerializeField]
        private Button btnExchangePoint;

        [Header("Summon references")]
        [SerializeField]
        private Toggle toggleSkipAnimation;

        [SerializeField]
        private UIEventLeHoiBangLongSummonButton btnX1;

        [SerializeField]
        private UIEventLeHoiBangLongSummonButton btnX10;

        [Header("Reward references")]
        [SerializeField]
        private TextMeshProUGUI txtDropRate;

        [SerializeField]
        private UIEventLeHoiBangLongProgressPanel progressPanel;

        // --- Private Fields ---
        private Action<EEventLeHoiBangLongLayoutType> _onChangeLayout;
        private EventBLMilestoneDto _currentMilestone;

        private bool _isClaimingMilestone;

        private void OnEnable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged += RefreshProgress;
        }

        private void OnDisable()
        {
            EventLeHoiBangLongManager.Instance.OnDataChanged -= RefreshProgress;
        }

        private void Awake()
        {
            btnExchangePoint.onClick.AddListener(OnClickExchangePoint);
            btnX1.Bind(1, OnClickSummon);
            btnX10.Bind(10, OnClickSummon);
        }

        private async void OnClickSummon(int times)
        {
            var rewards = await TrySummon(times);

            if (rewards.Count > 0)
            {
                UIManager.Instance
                    .OpenPopupAsync<PopupEventLeHoiBangLongSummonView>(new PopupEventLeHoiBangLongSummonArgs(
                        toggleSkipAnimation.isOn,
                        TrySummon,
                        rewards
                    ))
                    .Forget();
            }
        }

        private UniTask<List<ItemData>> TrySummon(int times)
        {
            return EventLeHoiBangLongManager.Instance.SummonAsync(times);
        }

        private void OnDestroy()
        {
            btnExchangePoint.onClick.RemoveListener(OnClickExchangePoint);
        }

        private void OnClickExchangePoint()
        {
            _onChangeLayout?.Invoke(EEventLeHoiBangLongLayoutType.Mission);
        }

        public void Bind(
            Action<EEventLeHoiBangLongLayoutType> onChangeLayout,
            double remainTime
        )
        {
            _onChangeLayout = onChangeLayout;

            countdownTimer.Bind(remainTime, OnCountdown);
            RefreshProgress();
        }

        private string OnCountdown(long days, long hours, long minutes, long seconds)
        {
            return LocalizationManager.GetText(LocalizationKeys.UI_END_TIME, days, hours, minutes, seconds);
        }

        private void OnClickClaimAccumulated()
        {
            ClaimCurrentMilestoneAsync().Forget();
        }

        private async UniTaskVoid ClaimCurrentMilestoneAsync()
        {
            if (_isClaimingMilestone ||
                _currentMilestone == null ||
                _currentMilestone.IsClaimed)
            {
                return;
            }

            _isClaimingMilestone = true;

            var rewards = await EventLeHoiBangLongManager.Instance
                .ClaimSummonMilestone(_currentMilestone.Milestone);

            _isClaimingMilestone = false;

            if (rewards.Count > 0)
            {
                PopupRewardService.Show(rewards);
            }
        }

        private void RefreshProgress()
        {
            var state = EventLeHoiBangLongManager.Instance.State;
            var milestones = state?.SummonMilestones;
            var accumulatedPoint = state?.Progress?.SummonPoints ?? 0;
            var remainAccumulated = ValueConstants.ACCUMULATED_STEP - (accumulatedPoint % ValueConstants.ACCUMULATED_STEP);

            if (milestones == null ||
                milestones.Count == 0)
            {
                _currentMilestone = null;

                progressPanel.gameObject.SetActive(false);
                return;
            }

            progressPanel.gameObject.SetActive(true);

            _currentMilestone = milestones
                .Where(value => !value.IsClaimed)
                .OrderBy(value => value.PointsRequired)
                .FirstOrDefault();

            var displayMilestone = _currentMilestone ??
                                   milestones
                                       .OrderByDescending(value => value.PointsRequired)
                                       .First();

            var isClaimedOrProcessing = _currentMilestone == null ||
                                        _isClaimingMilestone;

            progressPanel.Bind(
                OnClickClaimAccumulated, accumulatedPoint,
                displayMilestone.PointsRequired,
                isClaimedOrProcessing,
                displayMilestone.Reward?.ItemId ?? 0
            );

            txtDropRate.text = LocalizationManager.GetText(LocalizationKeys.UI_EVENT_BL_SUMMON_NOTE, remainAccumulated);
        }
    }
}