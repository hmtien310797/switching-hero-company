using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    public class RewardSyncService : MonoBehaviour
    {
        [Header("Online Idle")]
        [SerializeField] private int onlineIdleFlushIntervalSeconds = 60;

        [Tooltip("BaseRewards hiện tại được hiểu là reward per minute.")]
        [SerializeField] private bool baseRewardIsPerMinute = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = true;

        private StageRuntimeData currentStageData;
        private float onlineIdleTimer;
        [ShowInInspector]
        private int onlineIdleElapsedSeconds;

        public event Action OnOnlineIdlePreviewChanged;

        public void SetCurrentStageData(StageRuntimeData stageData)
        {
            if (currentStageData != null)
                FlushOnlineIdleReward().Forget();

            currentStageData        = stageData;
            onlineIdleTimer         = 0f;
            onlineIdleElapsedSeconds = 0;

            if (enableDebugLog && currentStageData != null)
            {
                Debug.Log(
                    "[RewardSync] SetCurrentStageData: " +
                    $"Stage={currentStageData.GlobalStage}, " +
                    $"Chapter={currentStageData.ChapterId}, " +
                    $"LocalStage={currentStageData.LocalStage}"
                );
            }

            OnOnlineIdlePreviewChanged?.Invoke();
        }

        private void Update()
        {
            TickOnlineIdleReward(Time.deltaTime);
        }

        private void TickOnlineIdleReward(float deltaTime)
        {
            if (currentStageData == null) return;
            if (currentStageData.BaseRewards == null || currentStageData.BaseRewards.Length == 0) return;

            onlineIdleTimer += deltaTime;
            if (onlineIdleTimer < 1f) return;

            int seconds = Mathf.FloorToInt(onlineIdleTimer);
            onlineIdleTimer -= seconds;
            onlineIdleElapsedSeconds += seconds;

            AddOnlineIdleRewardBySeconds(seconds);
            OnOnlineIdlePreviewChanged?.Invoke();

            // Auto-flush đã được thay bởi idle/claim (button hình gương).
            // Giữ onlineIdleElapsedSeconds để UI preview thời gian chưa claim.
        }

        /// <summary>
        /// Gọi khi player bấm button hình gương. Server tự tính reward từ last_claim_unix.
        /// Client reset preview sau khi claim thành công.
        /// </summary>
        public async UniTask<IdleClaimResponse> ClaimRewardAsync()
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn)
                return null;

            try
            {
                IdleClaimResponse response = await NakamaClient.Instance.ClaimOnlineIdleAsync();

                if (response != null && response.Success)
                {
                    // Reset preview
                    onlineIdleElapsedSeconds = 0;
                    CurrencyLedgerService.Instance?.ClearPendingByReason(CurrencyTransactionReason.OnlineFarming);
                    OnOnlineIdlePreviewChanged?.Invoke();

                    if (response.Balances != null)
                        CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
                }

                if (enableDebugLog)
                    Debug.Log($"[RewardSync] idle/claim elapsed={response?.ElapsedSeconds}s rewards={response?.Rewards?.Count}");

                return response;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RewardSync] ClaimRewardAsync failed: {e.Message}");
                return null;
            }
        }

        public async UniTask FlushOnlineIdleReward()
        {
            if (currentStageData == null || onlineIdleElapsedSeconds <= 0)
            {
                onlineIdleElapsedSeconds = 0;
                OnOnlineIdlePreviewChanged?.Invoke();
                return;
            }

            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn)
            {
                onlineIdleElapsedSeconds = 0;
                OnOnlineIdlePreviewChanged?.Invoke();
                return;
            }

            int  elapsed = onlineIdleElapsedSeconds;
            int  stage   = currentStageData.GlobalStage;
            List<RewardDto> clientRewards = BuildIdleRewardsForRequest(elapsed);

            // Reset trước khi chờ server để tick tiếp không bị đếm vào lần flush này.
            onlineIdleElapsedSeconds = 0;

            // Xoá pending ledger OnlineFarming để tránh double-count sau khi Set() từ server.
            CurrencyLedgerService.Instance?.ClearPendingByReason(CurrencyTransactionReason.OnlineFarming);
            OnOnlineIdlePreviewChanged?.Invoke();

            try
            {
                IdleFlushResponse response = await NakamaClient.Instance.FlushOnlineIdleAsync(
                    new IdleFlushRequest
                    {
                        Stage          = stage,
                        ElapsedSeconds = elapsed,
                        Rewards        = clientRewards
                    }
                );

                if (response != null && response.Success && response.Balances != null)
                    CurrencyManager.Instance?.ApplyServerBalances(response.Balances);

                if (enableDebugLog)
                    Debug.Log($"[RewardSync] idle/flush stage={stage} elapsed={elapsed}s");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RewardSync] FlushOnlineIdleAsync failed: {e.Message}");
            }

            OnOnlineIdlePreviewChanged?.Invoke();
        }

        private void AddOnlineIdleRewardBySeconds(int seconds)
        {
            if (seconds <= 0) return;
            if (CurrencyLedgerService.Instance == null)
            {
                Debug.LogError("[RewardSync] Missing CurrencyLedgerService.");
                return;
            }

            double timeMultiplier = baseRewardIsPerMinute ? seconds / 60d : seconds;

            for (int i = 0; i < currentStageData.BaseRewards.Length; i++)
            {
                StageReward reward = currentStageData.BaseRewards[i];
                if (!reward.IsValid) continue;
                if (reward.currencyType == CurrencyType.none)
                {
                    Debug.LogError($"[RewardSync] Unknown currency type: {reward.currencyType}");
                    continue;
                }

                BigNumber amount = reward.Amount * timeMultiplier;
                if (amount <= 0) continue;

                CurrencyLedgerService.Instance.AddOrMergeIncome(
                    reward.currencyType,
                    amount,
                    CurrencyTransactionReason.OnlineFarming
                );
                FarmingIdleScreenService.AddEarnedReward(reward.currencyType, amount);
            }
        }

        // Tính lại client-side rewards để gửi kèm vào idle/flush request (server verify).
        private List<RewardDto> BuildIdleRewardsForRequest(int elapsedSeconds)
        {
            var list = new List<RewardDto>();
            if (currentStageData?.BaseRewards == null) return list;

            double minutes = elapsedSeconds / 60.0;

            for (int i = 0; i < currentStageData.BaseRewards.Length; i++)
            {
                StageReward reward = currentStageData.BaseRewards[i];
                if (!reward.IsValid) continue;

                BigNumber amount  = reward.Amount * minutes;
                int       floored = amount.FloorToIntSafe();
                if (floored <= 0) continue;

                list.Add(new RewardDto
                {
                    CurrencyType = reward.currencyType.ToString(),
                    Amount       = floored.ToString()
                });
            }

            return list;
        }

        private void ApplyRewardsToLedger(List<StageReward> rewards, CurrencyTransactionReason reason)
        {
            if (rewards == null || rewards.Count == 0) return;
            if (CurrencyLedgerService.Instance == null)
            {
                Debug.LogError("[RewardSync] Missing CurrencyLedgerService.");
                return;
            }

            for (int i = 0; i < rewards.Count; i++)
                CurrencyLedgerService.Instance.AddOrMergeIncome(rewards[i].currencyType, rewards[i].Amount, reason);

            OnOnlineIdlePreviewChanged?.Invoke();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) FlushOnlineIdleReward().Forget();
        }

        private void OnApplicationQuit()
        {
            FlushOnlineIdleReward().Forget();
        }
    }
}
