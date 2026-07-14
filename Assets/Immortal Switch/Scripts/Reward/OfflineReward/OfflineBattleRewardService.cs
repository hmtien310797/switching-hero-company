using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    public class OfflineBattleRewardService : MonoBehaviour
    {
        public static OfflineBattleRewardService Instance { get; private set; }

        [Header("Config")]
        [Tooltip("Fallback nếu server không trả defeats_per_minute.")]
        [SerializeField] private int defeatsPerMinute = 200;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private OfflineBattleRewardResult currentResult;

        public OfflineBattleRewardResult CurrentResult => currentResult;

        public bool HasReward =>
            currentResult != null &&
            currentResult.Rewards != null &&
            currentResult.Rewards.Count > 0;

        private void Awake() { Instance = this; }

        private void OnEnable()
        {
            GameEventManager.Subscribe(GameEvents.OnAppPaused,  HandleAppPaused);
            GameEventManager.Subscribe(GameEvents.OnAppQuit,    HandleAppQuit);
            GameEventManager.Subscribe(GameEvents.OnAppResumed, HandleAppResumed);
        }

        private void OnDisable()
        {
            GameEventManager.Unsubscribe(GameEvents.OnAppPaused,  HandleAppPaused);
            GameEventManager.Unsubscribe(GameEvents.OnAppQuit,    HandleAppQuit);
            GameEventManager.Unsubscribe(GameEvents.OnAppResumed, HandleAppResumed);
        }

        private void HandleAppPaused()  => SaveExitState(CurrentStageService.CurrentStage);
        private void HandleAppQuit()    => SaveExitState(CurrentStageService.CurrentStage);
        private void HandleAppResumed() => CalculateAndShowOnReturn();

        // ── Checkpoint ────────────────────────────────────────────────────────

        public void SaveExitState(int currentStage)
        {
            if (!IsClientReady()) return;

            SaveCheckpointAsync(currentStage).Forget();

            if (enableDebugLog)
                Debug.Log($"[OfflineBattle] Save checkpoint stage={currentStage}");
        }

        private static async UniTaskVoid SaveCheckpointAsync(int stage)
        {
            try
            {
                await NakamaClient.Instance.SaveAfkCheckpointAsync(Mathf.Max(1, stage));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OfflineBattle] SaveAfkCheckpointAsync failed: {e.Message}");
            }
        }

        // ── Claim ─────────────────────────────────────────────────────────────

        public void CalculateAndShowOnReturn()
        {
            CalculateAndShowAsync().Forget();
        }

        private async UniTaskVoid CalculateAndShowAsync()
        {
            if (!IsClientReady()) return;

            AfkClaimResponse response;
            try
            {
                response = await NakamaClient.Instance.ClaimAfkRewardAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OfflineBattle] ClaimAfkRewardAsync failed: {e.Message}");
                return;
            }

            if (response == null || !response.Success || !response.HasReward)
            {
                if (enableDebugLog) Debug.Log("[OfflineBattle] No offline reward.");
                return;
            }

            if (enableDebugLog)
                Debug.Log($"[OfflineBattle] Reward claimed: stage={response.AfkStage}, elapsed={response.ElapsedSeconds}s, monsters={response.MonstersDefeated}");

            // Áp dụng balance ngay để UI hiển thị đúng trước khi player bấm Claim
            if (response.Balances != null && CurrencyManager.Instance != null)
                CurrencyManager.Instance.ApplyServerBalances(response.Balances);

            currentResult = BuildResult(response);

            UIManager.Instance.TogglePopupAsync<OfflineBattleRewardPopupView>(
                new OfflineBattleRewardOpenParam { Result = currentResult },
                false
            );
        }

        public async UniTask ClaimCurrentReward()
        {
            // Server đã cộng reward khi afk/claim được gọi.
            // Balance đã apply lên CurrencyManager khi popup mở.
            // Popup gọi method này chỉ để clear state.
            currentResult = null;
            await UniTask.CompletedTask;
        }

        // Giữ lại để không break caller cũ — giờ là no-op vì server quản lý state.
        public void ClearSavedExitState() { }

        // ── Helpers ───────────────────────────────────────────────────────────

        private OfflineBattleRewardResult BuildResult(AfkClaimResponse response)
        {
            return new OfflineBattleRewardResult
            {
                Stage             = response.AfkStage,
                OfflineSeconds    = response.ElapsedSeconds,
                MaxOfflineSeconds = response.MaxOfflineSeconds > 0 ? response.MaxOfflineSeconds : 43200,
                DefeatsPerMinute  = response.DefeatsPerMinute  > 0 ? response.DefeatsPerMinute  : defeatsPerMinute,
                MonstersDefeated  = response.MonstersDefeated,
                Rewards           = ConvertRewards(response.Rewards)
            };
        }

        private static List<StageReward> ConvertRewards(List<RewardDto> dtos)
        {
            return new List<StageReward>(StageRewardConverter.FromRewardDtos(dtos));
        }

        private static bool IsClientReady()
            => NakamaClient.Instance != null && NakamaClient.Instance.IsLoggedIn;
    }
}
