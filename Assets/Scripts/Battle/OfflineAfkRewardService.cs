using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    public class OfflineAfkRewardService : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private int  currentAfkStage = 1;
        private bool initialized;

        /// <summary>
        /// Bắn ra sau khi claim thành công và server trả về reward.
        /// UI đăng ký event này để hiển thị popup OfflineBattleRewardPopupView.
        /// </summary>
        public event Action<AfkClaimResponse> AfkRewardClaimed;

        public void Initialize(int currentStage)
        {
            currentAfkStage = Mathf.Max(1, currentStage);
            initialized     = true;
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await TryClaimOfflineAfkReward();
            await SaveCheckpointAsync(currentAfkStage);
        }

        // Chỉ cập nhật stage cục bộ (dùng khi pause/quit) — KHÔNG gọi afk/checkpoint ở đây.
        // Server bỏ qua afk_stage trong payload (rpcAfkCheckpoint chỉ stamp checkpoint_unix,
        // xem nakama/src/handler/afk.js), nên gọi RPC mỗi lần đổi stage chỉ có tác dụng phụ
        // là reset checkpoint_unix liên tục trong lúc chơi — khiến elapsed luôn gần 0 và
        // popup AFKRewardView luôn hiển thị phần thưởng trống dù đã treo máy bao lâu.
        public void SetCurrentAfkStage(int stage)
        {
            currentAfkStage = Mathf.Max(1, stage);
        }

        private async UniTask TryClaimOfflineAfkReward()
        {
            if (!IsClientReady()) return;

            try
            {
                AfkClaimResponse response = await NakamaClient.Instance.ClaimAfkRewardAsync();

                if (response == null || !response.Success)
                {
                    if (enableDebugLog) Debug.LogWarning("[OfflineAFK] Claim failed or null response.");
                    return;
                }

                if (!response.HasReward)
                {
                    if (enableDebugLog)
                        Debug.Log($"[OfflineAFK] No reward (elapsed={response.ElapsedSeconds}s).");
                    return;
                }

                if (enableDebugLog)
                    Debug.Log($"[OfflineAFK] Claimed: stage={response.AfkStage}, elapsed={response.ElapsedSeconds}s, monsters={response.MonstersDefeated}");

                if (response.Balances != null && CurrencyManager.Instance != null)
                    CurrencyManager.Instance.ApplyServerBalances(response.Balances);

                AfkRewardClaimed?.Invoke(response);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OfflineAFK] ClaimAfkRewardAsync failed: {e.Message}");
            }
        }

        private async UniTask SaveCheckpointAsync(int afkStage)
        {
            if (!IsClientReady()) return;

            try
            {
                await NakamaClient.Instance.SaveAfkCheckpointAsync(afkStage);

                if (enableDebugLog)
                    Debug.Log($"[OfflineAFK] Checkpoint saved: stage={afkStage}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OfflineAFK] SaveAfkCheckpointAsync failed: {e.Message}");
            }
        }

        private static bool IsClientReady()
            => NakamaClient.Instance != null && NakamaClient.Instance.IsLoggedIn;

        private void OnApplicationPause(bool pause)
        {
            if (!initialized) return;

            if (pause)
                SaveCheckpointAsync(currentAfkStage).Forget();
            else
                TryClaimOfflineAfkReward().Forget();
        }

        private void OnApplicationQuit()
        {
            if (!initialized) return;
            SaveCheckpointAsync(currentAfkStage).Forget();
        }
    }
}
