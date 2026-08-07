using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Event.EventDice.UI
{
    public class UIEventDiceMilestoneTarget : MonoBehaviour
    {
        [SerializeField]
        private Button btnClaim;

        [SerializeField]
        private Image imgChest;

        [PreviewField]
        [SerializeField]
        private Sprite chestNormal;

        [PreviewField]
        [SerializeField]
        private Sprite chestClaimed;

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
            bool isClaimed,
            bool canClaim,
            Action<int> onClaimMilestone
        )
        {
            _milestoneId = milestoneId;
            _onClaimMilestone = onClaimMilestone;

            btnClaim.interactable = canClaim;
            imgChest.sprite = isClaimed ? chestClaimed : chestNormal;
            txtTarget.text = target.ToString();
        }
    }
}