using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class UISkillViewEnhanceAllController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private UISkillView uiSkillView;
        [SerializeField] private SkillViewDataProvider dataProvider;
        [SerializeField] private Button enhanceAllButton;
        [SerializeField] private TMP_Text summaryText;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private SkillEnhanceAllService enhanceAllService;
        private SkillInventoryEnhanceRepository repository;

        private void Awake()
        {
            repository = new SkillInventoryEnhanceRepository();
            enhanceAllService = new SkillEnhanceAllService(repository, dataProvider);

            if (enhanceAllButton != null)
            {
                enhanceAllButton.onClick.RemoveAllListeners();
                enhanceAllButton.onClick.AddListener(OnClickEnhanceAll);
            }
        }

        private void OnEnable()
        {
            if (dataProvider != null)
                dataProvider.OnDataChanged += HandleDataChanged;

            RefreshInteractable();
        }

        private void OnDisable()
        {
            if (dataProvider != null)
                dataProvider.OnDataChanged -= HandleDataChanged;
        }

        private void HandleDataChanged()
        {
            RefreshInteractable();
        }

        public void RefreshInteractable()
        {
            if (enhanceAllButton == null || enhanceAllService == null)
                return;

            bool canEnhance = enhanceAllService.CanEnhanceAnySkill();
            enhanceAllButton.interactable = canEnhance;

            Log($"RefreshInteractable -> canEnhance={canEnhance}");
        }

        private void OnClickEnhanceAll()
        {
            if (enhanceAllService == null)
            {
                LogWarning("OnClickEnhanceAll failed because enhanceAllService is null.");
                return;
            }

            SkillEnhanceAllResult result = enhanceAllService.EnhanceAll();
            ApplySummary(result);

            Log(
                $"EnhanceAll -> processed={result.ProcessedSkillCount}, " +
                $"upgradedSkills={result.UpgradedSkillCount}, " +
                $"totalLevelGained={result.TotalLevelGained}, " +
                $"totalShardSpent={result.TotalShardSpent}"
            );

            if (uiSkillView != null)
            {
                uiSkillView.RefreshAll();
            }
            else
            {
                LogWarning("uiSkillView is null. RefreshAll was skipped.");
            }

            if (dataProvider != null)
                dataProvider.NotifyDataChanged();

            RefreshInteractable();
        }

        private void ApplySummary(SkillEnhanceAllResult result)
        {
            if (summaryText == null)
                return;

            if (result == null || result.TotalLevelGained <= 0)
            {
                summaryText.text = "No skill can be enhanced.";
                return;
            }

            summaryText.text =
                $"Enhanced {result.UpgradedSkillCount} skills, " +
                $"+{result.TotalLevelGained} levels, spent {result.TotalShardSpent} shards.";
        }

        private void Log(string message)
        {
            if (!enableDebugLog)
                return;

            Debug.Log($"[UISkillViewEnhanceAllController] {message}", this);
        }

        private void LogWarning(string message)
        {
            if (!enableDebugLog)
                return;

            Debug.LogWarning($"[UISkillViewEnhanceAllController] {message}", this);
        }
    }
}