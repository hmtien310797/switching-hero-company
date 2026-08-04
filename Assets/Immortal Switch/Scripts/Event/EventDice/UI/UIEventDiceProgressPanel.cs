using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventDice.UI
{
    public class UIEventDiceProgressPanel : MonoBehaviour
    {
        [SerializeField]
        private Image imgFill;

        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [Header("Milestone references")]
        [SerializeField]
        private RectTransform milestoneContainer;

        [SerializeField]
        private UIEventDiceMilestoneTarget milestoneTarget;

        // --- Private Fields ---
        private SimpleUIPool<UIEventDiceMilestoneTarget> _pool;
        private Action<int> _onClaimMilestone;

        public void Bind(
            List<DynamicHeroesGlobalSpecificationsEventDiceMilestoneRow> milestones,
            int currentProgress,
            IReadOnlyCollection<int> claimedMilestoneIds,
            Action<int> onClaimMilestone
        )
        {
            _onClaimMilestone = onClaimMilestone;

            var maxProgress = milestones.LastOrDefault()?.pointsRequired ?? 0;

            txtProgress.text = $"{Mathf.Min(currentProgress, maxProgress)}/{maxProgress}";

            imgFill.fillAmount = maxProgress > 0
                ? Mathf.Clamp01(currentProgress / (float)maxProgress)
                : 0f;

            RefreshMilestones(milestones, currentProgress, claimedMilestoneIds);
        }

        private void RefreshMilestones(
            List<DynamicHeroesGlobalSpecificationsEventDiceMilestoneRow> milestones,
            int currentProgress,
            IReadOnlyCollection<int> claimedMilestoneIds
        )
        {
            _pool ??= new SimpleUIPool<UIEventDiceMilestoneTarget>(
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
                    isClaimed,
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