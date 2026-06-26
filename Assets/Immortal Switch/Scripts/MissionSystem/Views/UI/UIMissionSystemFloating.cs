using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.Views.UI
{
    public class UIMissionSystemFloating : MonoBehaviour
    {
        [Header("Reward panel")] [SerializeField]
        private TextMeshProUGUI txtRewardQuantity;

        [SerializeField] private Image imgRewardIcon;

        [Header("Mission info")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private TextMeshProUGUI txtProgress;

        [Header("Button claim")] [SerializeField]
        private Button btnClaim;

        // --- Private Field ---
        private DynamicHeroesGlobalSpecificationsMissionConfigRow _cfg;

        private void Awake()
        {
            MissionSystemManager.Instance.OnChangeProgress += OnMissionSystemChangeProgress;
            btnClaim.onClick.AddListener(OnClaim);
        }

        private void OnEnable()
        {
            MissionSystemManager.Instance.NotifyReady();
        }

        private void OnMissionSystemChangeProgress(string arg1, int arg2, string arg3)
        {
            if (arg1 == MissionSystemTypes.MAIN)
            {
                var cfg = MissionSystemManager.Instance.GetMission(arg3);

                if (cfg != null)
                {
                    _cfg = cfg;
                    txtTitle.text = cfg.title;
                    txtDescription.text = cfg.description;
                    txtProgress.text = $"( {arg2} / {cfg.target:F0} )";
                    RefreshVisual(cfg).Forget();
                }
            }
        }

        private async UniTask RefreshVisual(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            var isCompleted = MissionSystemManager.Instance.IsCompleted(cfg);
            btnClaim.interactable = isCompleted;

            var rewards = RewardHelper.ParseRewards(cfg.rewards);

            if (rewards.Count > 0)
            {
                var reward = rewards[0];
                var sprite = await DatabaseManager.Instance.ItemDb.LoadCurrencyIconByKey(reward.itemKey);

                if (sprite != null)
                {
                    imgRewardIcon.sprite = sprite;
                }

                txtRewardQuantity.text = BigIntegerHelper.Format(reward.quantity);
            }
        }

        private void OnClaim()
        {
            if (_cfg == null)
            {
                Debug.LogError("MissionSystemClaim Not Found");
                return;
            }

            MissionSystemManager.Instance.MissionClaim(_cfg);
        }
    }
}