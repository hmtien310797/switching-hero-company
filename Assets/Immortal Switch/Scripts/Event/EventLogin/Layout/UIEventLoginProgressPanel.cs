using System;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventLogin.UI;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLogin.Layout
{
    public class UIEventLoginProgressPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtCurrentPoint;

        [SerializeField]
        private Button btnClaim;

        [Header("Milestone references")]
        [SerializeField]
        private Image imgFill;

        [SerializeField]
        private RectTransform milestoneContainer;

        [SerializeField]
        private UIEventLoginMilestoneTarget milestonePrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIEventLoginMilestoneTarget> _pools;
        private Action _onClaimAll;
        private Action<int> _onClaimMilestone;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaimAll);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaimAll);
        }

        private void OnClickClaimAll()
        {
            _onClaimAll?.Invoke();
        }

        private void OnClickClaimMilestone(int milestoneId)
        {
            _onClaimMilestone?.Invoke(milestoneId);
        }

        public void Bind(
            List<EventLoginMilestoneDto> milestones,
            int currentPoint,
            Action onClaimAll,
            Action<int> onClickClaimMilestone
        )
        {
            _onClaimAll = onClaimAll;
            _onClaimMilestone = onClickClaimMilestone;

            var maxValue = milestones.LastOrDefault()?.PointsRequired ?? 1;

            imgFill.fillAmount = currentPoint / (maxValue * 1f);
            txtCurrentPoint.text = LocalizationManager.GetText(LocalizationKeys.UI_QUEST_POINT, $"{currentPoint:00}");

            btnClaim.interactable = milestones.Any(m => !m.IsClaimed && currentPoint >= m.PointsRequired);
            RefreshMilestones(milestones, currentPoint);
        }

        private void RefreshMilestones(
            List<EventLoginMilestoneDto> milestones,
            int currentPoint
        )
        {
            _pools ??= new SimpleUIPool<UIEventLoginMilestoneTarget>(milestonePrefab, milestoneContainer);

            for (int i = 0; i < milestones.Count; i++)
            {
                var milestone = milestones[i];
                var clone = _pools.Get(i);

                clone.Bind(
                    currentPoint,
                    milestone.Reward.ItemId,
                    milestone.Reward.Amount,
                    milestone.PointsRequired,
                    milestone.IsClaimed,
                    OnClickClaimMilestone,
                    milestone.Milestone
                );
            }

            _pools.ReleaseFrom(milestones.Count);
        }
    }
}