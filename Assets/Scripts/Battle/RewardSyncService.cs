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
        
        public event System.Action OnOnlineIdlePreviewChanged;

        public void SetCurrentStageData(StageRuntimeData stageData)
        {
            if (currentStageData != null)
            {
                FlushOnlineIdleReward().Forget();
            }

            currentStageData = stageData;
            onlineIdleTimer = 0f;
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
            if (currentStageData == null)
                return;

            if (currentStageData.BaseRewards == null || currentStageData.BaseRewards.Length == 0)
                return;

            onlineIdleTimer += deltaTime;

            if (onlineIdleTimer < 1f)
                return;

            int seconds = Mathf.FloorToInt(onlineIdleTimer);
            onlineIdleTimer -= seconds;
            onlineIdleElapsedSeconds += seconds;

            AddOnlineIdleRewardBySeconds(seconds);
            OnOnlineIdlePreviewChanged?.Invoke();

            if (onlineIdleElapsedSeconds >= onlineIdleFlushIntervalSeconds)
            {
                FlushOnlineIdleReward().Forget();
            }
        }
        
        public async UniTask FlushOnlineIdleReward()
        {
            if (CurrencyLedgerService.Instance == null)
                return;

            await CurrencyLedgerService.Instance.SyncPendingTransactions();

            onlineIdleElapsedSeconds = 0;

            OnOnlineIdlePreviewChanged?.Invoke();
        }
        

        private void AddOnlineIdleRewardBySeconds(int seconds)
        {
            if (seconds <= 0)
                return;

            if (CurrencyLedgerService.Instance == null)
            {
                Debug.LogError("[RewardSync] Missing CurrencyLedgerService.");
                return;
            }

            double timeMultiplier = baseRewardIsPerMinute
                ? seconds / 60d
                : seconds;

            for (int i = 0; i < currentStageData.BaseRewards.Length; i++)
            {
                StageReward reward = currentStageData.BaseRewards[i];

                if (!reward.IsValid)
                    continue;

                if (!System.Enum.TryParse(reward.ResourceType, true, out CurrencyType currencyType))
                {
                    Debug.LogError($"[RewardSync] Unknown currency type: {reward.ResourceType}");
                    continue;
                }

                double amountDouble = reward.Amount * timeMultiplier;

                if (amountDouble <= 0)
                    continue;

                BigNumber amount = BigNumber.FromDouble(amountDouble);

                CurrencyLedgerService.Instance.AddOrMergeIncome(
                    currencyType,
                    amount,
                    CurrencyTransactionReason.OnlineFarming
                );
            }
        }
        

        public async UniTask ClaimClearStageReward(StageRuntimeData stageData)
        {
            if (stageData == null)
                return;
            
            ClearStageRewardRequest request = new ClearStageRewardRequest
            {
                stage = stageData.GlobalStage,
                chapterId = stageData.ChapterId,
                localStage = stageData.LocalStage,
                rewards = StageRewardConverter.ToRewardDtos(stageData.ClearRewards)
            };
            ApplyRewardsToLedger(request.rewards, CurrencyTransactionReason.ClearStageReward);
            if (request.rewards == null || request.rewards.Count == 0)
            {
                if (enableDebugLog)
                    Debug.Log($"[RewardSync] Stage {request.stage} has no clear rewards.");

                return;
            }

            // TODO SERVER:
            // Đây chỉ là cộng tạm dưới client để demo.
            // Sau này thay bằng:
            // var response = await serverApi.ClaimClearStageReward(request);
            // CurrencyManager.Instance.ApplyServerBalances(response.balances);
            ApplyRewardsLocallyForDemo(request.rewards, "ClearReward");

            await UniTask.CompletedTask;
        }
        

        public async UniTask ClaimOfflineAfkReward(
            int afkStage,
            int elapsedSeconds,
            StageReward[] offlineRewards
        )
        {
            if (elapsedSeconds <= 0)
                return;

            OfflineAfkRewardRequest request = new OfflineAfkRewardRequest
            {
                afkStage = afkStage,
                elapsedSeconds = elapsedSeconds,
                rewards = StageRewardConverter.ToRewardDtos(offlineRewards)
            };

            if (request.rewards == null || request.rewards.Count == 0)
                return;

            // TODO SERVER:
            // Đây chỉ là cộng tạm dưới client để demo.
            // Sau này thay bằng:
            // var response = await serverApi.ClaimOfflineAfkReward(request);
            // CurrencyManager.Instance.ApplyServerBalances(response.balances);
            ApplyRewardsLocallyForDemo(request.rewards, "OfflineAfkReward");

            await UniTask.CompletedTask;
        }

        private void ApplyRewardsLocallyForDemo(List<RewardAmountDto> rewards, string reason)
        {
            if (rewards == null || rewards.Count == 0)
                return;

            if (enableDebugLog)
                Debug.Log($"[RewardSync][DEMO LOCAL] Apply {reason}");

            for (int i = 0; i < rewards.Count; i++)
            {
                RewardAmountDto reward = rewards[i];

                if (enableDebugLog)
                    Debug.Log($"[RewardSync][DEMO LOCAL] {reward.currencyType} +{reward.amount}");
            }

            // TODO SERVER:
            // Chỗ này chỉ làm tạm thời để demo.
            // Sau này không cộng trực tiếp dưới client nữa.
            // Thay bằng apply balances từ server response.
            CurrencyManager.Instance?.AddLocalDemoRewards(rewards);
        }
        
        private void ApplyRewardsToLedger(
            List<RewardAmountDto> rewards,
            CurrencyTransactionReason reason
        )
        {
            if (rewards == null || rewards.Count == 0)
                return;

            if (CurrencyLedgerService.Instance == null)
            {
                Debug.LogError("[RewardSync] Missing CurrencyLedgerService.");
                return;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                RewardAmountDto reward = rewards[i];

                if (!System.Enum.TryParse(reward.currencyType, true, out CurrencyType currencyType))
                {
                    Debug.LogError($"[RewardSync] Unknown currency type: {reward.currencyType}");
                    continue;
                }

                if (!BigNumber.TryParseInputString(reward.amount, out BigNumber amount))
                {
                    Debug.LogError($"[RewardSync] Cannot parse reward amount: {reward.amount}");
                    continue;
                }

                CurrencyLedgerService.Instance.AddOrMergeIncome(
                    currencyType,
                    amount,
                    reason
                );
            }

            OnOnlineIdlePreviewChanged?.Invoke();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                FlushOnlineIdleReward().Forget();
        }

        private void OnApplicationQuit()
        {
            FlushOnlineIdleReward().Forget();
        }
    }
}