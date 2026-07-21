using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionSystemFloating : MonoBehaviour
    {
        [Header("Reward panel")]
        [SerializeField]
        private TextMeshProUGUI txtRewardQuantity;

        [SerializeField]
        private UIItemSlot rewardSlot;

        [Header("Mission info")]
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtDescription;

        [SerializeField]
        private TextMeshProUGUI txtProgress;

        [Header("Button claim")]
        [SerializeField]
        private Button btnClaim;

        // --- Private Field ---
        private DynamicHeroesGlobalSpecificationsMissionConfigRow _cfg;

        private void Awake()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
            MissionSystemManager.Instance.OnChangeProgress += OnMissionSystemChangeProgress;
            btnClaim.onClick.AddListener(OnClaim);
        }

        private void OnEnable()
        {
            MissionSystemManager.Instance.NotifyReady();
        }

        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
            MissionSystemManager.Instance.OnChangeProgress -= OnMissionSystemChangeProgress;
            btnClaim.onClick.RemoveListener(OnClaim);
        }

        private UniTask OnClickTutorial(string arg1, int arg2)
        {
            switch (arg2)
            {
                // Chỉ step 14 (targetUI: QuestClaimButton) là bước claim thật sự.
                // Step 12/13 nhắm vào QuestButton (mở màn hình nhiệm vụ) và step 15 là
                // variant lặp lại của 14 — gọi OnClaim() cho các step này chỉ khiến
                // MissionClaim fail vì nhiệm vụ chưa xong (12/13) hoặc đã claim rồi (15).
                case 14:
                    OnClaim();
                    break;
            }

            return UniTask.CompletedTask;
        }

        private RectTransform OnResolveTarget(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 12:
                case 13:
                case 14:
                case 15:
                    return transform as RectTransform;

                default:
                    return null;
            }
        }

        private void OnMissionSystemChangeProgress(string arg1, int arg2, string arg3)
        {
            if (arg1 == MissionTypes.MAIN)
            {
                var cfg = MissionSystemManager.Instance.GetMission(arg3);

                if (cfg != null)
                {
                    _cfg = cfg;
                    txtTitle.text = cfg.title;
                    txtDescription.text = cfg.description;
                    txtProgress.text = $"( {arg2} / {cfg.target:F0} )";
                    RefreshVisual(cfg);
                }
            }
        }

        private void RefreshVisual(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            var isCompleted = MissionSystemManager.Instance.IsCompleted(cfg);
            btnClaim.interactable = isCompleted;

            var rewards = DatabaseManager.Instance.GetRewards(cfg.rewards);

            if (rewards.Count > 0)
            {
                var reward = rewards[0];

                rewardSlot.Bind(reward.ItemIcon, reward.TierInfo.border, reward.TierInfo.background, reward.TierInfo.tierIcon);
                txtRewardQuantity.SetText(reward.Quantity.ToInputString());
            }
        }

        private void OnClaim()
        {
            if (_cfg != null)
            {
                MissionSystemManager.Instance.MissionClaim(_cfg);
            }
            else
            {
                Debug.LogError("Mission claim not found");
            }
        }
    }
}