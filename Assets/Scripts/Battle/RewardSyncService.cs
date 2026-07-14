using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    // Active farming (per-second local ticking + idle/flush) has been removed — everything
    // is AFK now. This service only exposes a manual claim that hits afk/claim, the same
    // unified endpoint OfflineBattleRewardService uses on app resume.
    public class RewardSyncService : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private StageRuntimeData currentStageData;

        public event Action OnOnlineIdlePreviewChanged;

        public void SetCurrentStageData(StageRuntimeData stageData)
        {
            currentStageData = stageData;

            if (enableDebugLog && currentStageData != null)
            {
                Debug.Log(
                    "[RewardSync] SetCurrentStageData: " +
                    $"Stage={currentStageData.GlobalStage}, " +
                    $"Chapter={currentStageData.ChapterId}, " +
                    $"LocalStage={currentStageData.LocalStage}"
                );
            }
        }

        /// <summary>
        /// Xem trước reward hiện có (afk/preview) — KHÔNG commit, dùng để mở popup claim với
        /// số liệu thật trước khi player xác nhận.
        /// </summary>
        public async UniTask<AfkClaimResponse> PreviewRewardAsync()
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn)
                return null;

            try
            {
                return await NakamaClient.Instance.PeekAfkRewardAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RewardSync] PreviewRewardAsync failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gọi khi player bấm nút Claim trong popup — đây mới là lúc commit thật (afk/claim),
        /// server tính reward từ checkpoint cuối cùng và stage hiện tại của player.
        /// </summary>
        public async UniTask<AfkClaimResponse> ClaimRewardAsync()
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn)
                return null;

            try
            {
                AfkClaimResponse response = await NakamaClient.Instance.ClaimAfkRewardAsync();

                if (response != null && response.Success && response.Balances != null)
                    CurrencyManager.Instance?.ApplyServerBalances(response.Balances);

                OnOnlineIdlePreviewChanged?.Invoke();

                if (enableDebugLog)
                    Debug.Log($"[RewardSync] afk/claim elapsed={response?.ElapsedSeconds}s rewards={response?.Rewards?.Count}");

                return response;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RewardSync] ClaimRewardAsync failed: {e.Message}");
                return null;
            }
        }
    }
}
