using Game.Configs.Generated;
using Immortal_Switch.Scripts.UI;
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
        private DynamicHeroesGlobalSpecificationsMissionConfigRow _currentCfg;

        private void Awake()
        {
            MissionSystemManager.Instance.OnChangeProgress += OnMissionSystemChangeProgress;
            btnClaim.onClick.AddListener(OnClaim);
        }

        private void OnMissionSystemChangeProgress(string arg1, float arg2, string arg3)
        {
            if (arg1 == MissionSystemTypes.MAIN)
            {
                var cfg = MissionSystemManager.Instance.GetMission(arg3);

                if (cfg != null)
                {
                    _currentCfg = cfg;
                    txtTitle.text = cfg.title;
                    txtDescription.text = $"( {arg2:F0} / {cfg.target:F0} )";
                    RefreshVisual(cfg);
                }
            }
        }

        private void RefreshVisual(DynamicHeroesGlobalSpecificationsMissionConfigRow cfg)
        {
            var isCompleted = MissionSystemManager.Instance.IsCompleted(cfg);
            goTaskDone.SetActive(isCompleted);
            btnClaim.interactable = isCompleted;
        }

        private void OnClaim()
        {
            if (_currentCfg == null)
            {
                Debug.LogError("MissionSystem Claim Not Found");
                return;
            }

            MissionSystemManager.Instance.Claim(_currentCfg);
        }
    }
}