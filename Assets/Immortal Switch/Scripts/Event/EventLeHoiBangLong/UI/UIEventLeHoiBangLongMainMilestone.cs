using System;
using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI
{
    public class UIEventLeHoiBangLongMainMilestone : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtPoint;

        [SerializeField]
        private Button btnClaim;

        [Header("Milestone references")]
        [SerializeField]
        private RectTransform milestoneContainer;

        [SerializeField]
        private UIEventLeHoiBangLongMilestoneTarget milestonePrefab;

        [Header("Progress references")]
        [SerializeField]
        private Image imgFill;

        // --- Private Fields ---
        private SimpleUIPool<UIEventLeHoiBangLongMilestoneTarget> _pools;
        private Action _onClaimMilestone;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaim);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaim);
        }

        private void OnClickClaim()
        {
            _onClaimMilestone?.Invoke();
        }

        public void Bind(
            int currentPoint, int maxValue, Action onClaimMilestone,
            List<DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow> rows
        )
        {
            _onClaimMilestone = onClaimMilestone;

            txtPoint.text = $"{currentPoint:N0}";
            imgFill.fillAmount = Mathf.Approximately(maxValue, 0f) ? 0f : currentPoint / (maxValue * 1f);

            RefreshMilestones(rows);

            btnClaim.interactable = rows.Exists(row =>
                currentPoint >= row.pointsRequired &&
                !EventLeHoiBangLongManager.Instance.IsSummonMilestoneClaimed(row.milestone)
            );
        }

        public void RefreshMilestones(List<DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow> rows)
        {
            _pools ??= new SimpleUIPool<UIEventLeHoiBangLongMilestoneTarget>(milestonePrefab, milestoneContainer);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var clone = _pools.Get(i);
                var isClaimed = EventLeHoiBangLongManager.Instance.IsSummonMilestoneClaimed(row.milestone);

                clone.Bind(row.itemId1, row.pointsRequired);
                clone.SetClaimed(isClaimed);
            }

            _pools.ReleaseFrom(rows.Count);
        }
    }
}