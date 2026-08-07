using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventBingo.UI
{
    public class UIEventBingoMilestoneTarget : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private TextMeshProUGUI txtTarget;

        // --- Private Fields ---
        private Action<int> _onClaimMilestone;
        private int _milestoneId;

        private void Awake()
        {
            btnClaim.onClick.AddListener(OnClickClaimMilestone);
        }

        private void OnDestroy()
        {
            btnClaim.onClick.RemoveListener(OnClickClaimMilestone);
        }

        private void OnClickClaimMilestone()
        {
            _onClaimMilestone?.Invoke(_milestoneId);
        }

        public void Bind(
            int milestoneId,
            int target,
            bool canClaim,
            Action<int> onClaimMilestone
        )
        {
            _milestoneId = milestoneId;
            _onClaimMilestone = onClaimMilestone;

            btnClaim.interactable = canClaim;
            txtTarget.text = target.ToString();
        }
    }
}