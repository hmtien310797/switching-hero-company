using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.ItemSystem;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.MissionSystem.UI
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

        private void OnMissionSystemChangeProgress(string arg1, float arg2, string arg3)
        {
            if (arg1 == MissionSystemTypes.MAIN)
            {
                var cfg = MissionSystemManager.Instance.GetMission(arg3);

                if (cfg != null)
                {
                    _cfg = cfg;
                    txtTitle.text = cfg.title;
                    txtDescription.text = cfg.description;
                    txtProgress.text = $"( {arg2:F0} / {cfg.target:F0} )";
                    RefreshVisual(cfg).Forget();
                }
            }
        }

        private async UniTask RefreshVisual(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            var isCompleted = MissionSystemManager.Instance.IsCompleted(cfg);
            btnClaim.interactable = isCompleted;

            var rewards = MissionSystemManager.Instance.ParseRewards(cfg.rewards);

            if (rewards.Count > 0)
            {
                var reward = rewards[0];
                var sprite = await ItemSystemManager.Instance.Database.GetCurrencyIcon(reward.itemKey);

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
                Debug.LogError("MissionSystem Claim Not Found");
                return;
            }

            MissionSystemManager.Instance.Claim(_cfg);
        }
    }
}