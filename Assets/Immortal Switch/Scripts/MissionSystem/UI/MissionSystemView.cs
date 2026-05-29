using Immortal_Switch.Scripts.MissionSystem.Models;
using Immortal_Switch.Scripts.UI;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.UI
{
    public class MissionSystemView : AnimatedUIView
    {
        [Header("Reward panel")] [SerializeField]
        private TextMeshProUGUI txtRewardQuantity;

        [SerializeField] private Image imgRewardIcon;

        [Header("Mission info")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private GameObject goTaskDone;

        [Header("Button claim")] [SerializeField]
        private Button btnClaim;

        // --- Private Field ---

        private void Awake()
        {
            MissionSystemManager.Instance.OnUpdateProgress += OnMissionSystemUpdateProgress;
            btnClaim.onClick.AddListener(OnClaim);
        }

        private void OnMissionSystemUpdateProgress(EMissionSystemType arg1, int arg2)
        {
            var entry = MissionSystemManager.Instance.Entry;

            if (entry != null &&
                entry.Value.type == arg1)
            {
                txtTitle.text = entry.Value.FormatTitle;
                txtDescription.text = $"( {arg2} / {entry.Value.target} )";
                txtRewardQuantity.text = $"{entry.Value.reward.quantity}";
                RefreshVisual();
            }
        }

        private void RefreshVisual()
        {
            var isClaim = MissionSystemManager.Instance.IsComplete && !MissionSystemManager.Instance.Data.IsClaimed;
            goTaskDone.SetActive(isClaim);
            btnClaim.interactable = isClaim;
        }

        private void OnClaim()
        {
            var entry = MissionSystemManager.Instance.Entry;

            if (entry != null)
            {
                // todo: claim reward.
                MissionSystemManager.Instance.Complete();
            }
        }
    }
}