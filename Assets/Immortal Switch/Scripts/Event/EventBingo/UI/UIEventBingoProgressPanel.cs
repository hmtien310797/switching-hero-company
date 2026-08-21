using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventBingo.UI
{
    public class UIEventBingoProgressPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [Header("Milestone references")]
        [SerializeField]
        private RectTransform milestoneContainer;

        [SerializeField]
        private UIEventBingoMilestoneTarget milestoneTarget;

        // --- Private Fields ---
        private SimpleUIPool<UIEventBingoMilestoneTarget> _pool;
        private Action<int> _onClaimMilestone;

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventBingoMilestoneRow> milestones,
            int currentProgress,
            IReadOnlyCollection<int> claimedMilestoneIds,
            Action<int> onClaimMilestone
        )
        {
            _onClaimMilestone = onClaimMilestone;

            txtProgress.text = LocalizationManager.GetText(LocalizationKeys.UI_CURRENT_POINT, currentProgress);

            RefreshMilestones(milestones, currentProgress, claimedMilestoneIds);
        }

        private void RefreshMilestones(
            List<DynamicHeroesGlobalSpecificationsEventBingoMilestoneRow> milestones,
            int currentProgress,
            IReadOnlyCollection<int> claimedMilestoneIds
        )
        {
            _pool ??= new SimpleUIPool<UIEventBingoMilestoneTarget>(
                milestoneTarget,
                milestoneContainer
            );

            for (var index = 0; index < milestones.Count; index++)
            {
                var milestone = milestones[index];
                var isClaimed = claimedMilestoneIds.Contains(milestone.milestone);
                var clone = _pool.Get(index);

                clone.Bind(
                    milestone.milestone,
                    milestone.pointsRequired,
                    !isClaimed && currentProgress >= milestone.pointsRequired,
                    OnClickClaimMilestone
                );
            }

            _pool.ReleaseFrom(milestones.Count);
        }

        private void OnClickClaimMilestone(int milestoneId)
        {
            _onClaimMilestone?.Invoke(milestoneId);
        }
    }
}