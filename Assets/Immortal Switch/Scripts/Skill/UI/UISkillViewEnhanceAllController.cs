using Common;
using Cysharp.Threading.Tasks;
using Nakama;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class UISkillViewEnhanceAllController : MonoBehaviour
    {
        [SerializeField] private UISkillView uiSkillView;
        [SerializeField] private Button enhanceAllButton;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private GameObject redDot;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private SkillViewDataProvider dataProvider;
        private SkillEnhanceAllService enhanceAllService;
        private SkillInventoryEnhanceRepository repository;
        private bool isEnhancing;

        private void Awake()
        {
            dataProvider = SkillViewDataProvider.Instance;
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
            if (enhanceAllService == null)
                return;

            bool canEnhance = enhanceAllService.CanEnhanceAnySkill();

            if (enhanceAllButton != null)
                enhanceAllButton.interactable = canEnhance;

            if (redDot != null)
                redDot.SetActive(canEnhance);

            Log($"RefreshInteractable -> canEnhance={canEnhance}");
        }

        private void OnClickEnhanceAll()
        {
            EnhanceAllAsync().Forget();
        }

        // Gọi RPC skill/enhance_all — server tính + trừ shard + tăng level cho mọi skill đủ
        // điều kiện trong 1 lần gọi. Thay hoàn toàn cho enhanceAllService.EnhanceAll() local cũ.
        private async UniTaskVoid EnhanceAllAsync()
        {
            if (isEnhancing)
                return;

            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn)
            {
                LogWarning("OnClickEnhanceAll skipped because NakamaClient is not logged in.");
                return;
            }

            isEnhancing = true;
            if (enhanceAllButton != null)
                enhanceAllButton.interactable = false;

            try
            {
                SkillEnhanceAllResponse response = await NakamaClient.Instance.EnhanceAllSkillsAsync();
                ApplyResponse(response);
            }
            catch (ApiResponseException ex) when (ex.StatusCode == 16)
            {
                LogWarning($"OnClickEnhanceAll failed: session invalid (UNAUTHENTICATED). {ex.Message}");
            }
            catch (ApiResponseException ex)
            {
                LogError($"OnClickEnhanceAll failed: {ex.StatusCode} {ex.Message}");
            }
            finally
            {
                isEnhancing = false;
                RefreshInteractable();
            }
        }

        private void ApplyResponse(SkillEnhanceAllResponse response)
        {
            if (response == null || !response.Success)
            {
                LogWarning("OnClickEnhanceAll failed: server returned no/unsuccessful response.");
                return;
            }

            if (response.Entries != null)
            {
                foreach (SkillEnhanceEntry entry in response.Entries)
                {
                    SkillInventorySaveService.SetOwned(entry.SkillId, true);
                    SkillInventorySaveService.SetLevel(entry.SkillId, entry.NewLevel);
                    SkillInventorySaveService.SetCurrentShard(entry.SkillId, entry.NewShard);
                }

                SkillInventorySaveService.Save();
                UserDataCache.Instance?.ApplySkillEnhanceEntries(response.Entries);
            }

            ApplySummary(response);

            Log(
                $"EnhanceAll -> processed={response.ProcessedSkillCount}, " +
                $"upgradedSkills={response.UpgradedSkillCount}, " +
                $"totalLevelGained={response.TotalLevelGained}, " +
                $"totalShardSpent={response.TotalShardSpent}"
            );

            if (uiSkillView != null)
                uiSkillView.RefreshAll();
            else
                LogWarning("uiSkillView is null. RefreshAll was skipped.");

            if (dataProvider != null)
                dataProvider.NotifyDataChanged();
        }

        private void ApplySummary(SkillEnhanceAllResponse response)
        {
            if (summaryText == null)
                return;

            if (response.Entries == null || response.Entries.Length == 0)
            {
                summaryText.text = "No skill can be enhanced.";
                return;
            }

            summaryText.text =
                $"Enhanced {response.UpgradedSkillCount} skills, " +
                $"+{response.TotalLevelGained} levels, spent {response.TotalShardSpent} shards.";
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

        private void LogError(string message)
        {
            Debug.LogError($"[UISkillViewEnhanceAllController] {message}", this);
        }
    }
}