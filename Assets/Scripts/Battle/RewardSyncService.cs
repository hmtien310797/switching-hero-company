using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    public class RewardSyncService : MonoBehaviour
    {
        [SerializeField] private int onlineIdleFlushIntervalSeconds = 60;

        private readonly OnlineIdleRewardBuffer onlineIdleBuffer = new OnlineIdleRewardBuffer();

        private StageRuntimeData currentStageData;
        private float onlineIdleTimer;
        private int onlineIdleElapsedSeconds;

        public void SetCurrentStageData(StageRuntimeData stageData)
        {
            currentStageData = stageData;
            onlineIdleTimer = 0f;
            onlineIdleElapsedSeconds = 0;
            onlineIdleBuffer.Clear();
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

            if (onlineIdleElapsedSeconds >= onlineIdleFlushIntervalSeconds)
            {
                FlushOnlineIdleReward().Forget();
            }
        }

        private void AddOnlineIdleRewardBySeconds(int seconds)
        {
            // Hiện tại quy ước BaseRewards là reward per minute.
            double minutes = seconds / 60d;

            for (int i = 0; i < currentStageData.BaseRewards.Length; i++)
            {
                StageReward reward = currentStageData.BaseRewards[i];

                if (!reward.IsValid)
                    continue;

                double amount = reward.Amount * minutes;

                if (amount <= 0)
                    continue;

                onlineIdleBuffer.Add(reward.ResourceType, amount);
            }
        }

        public async UniTask ClaimClearStageReward(StageRuntimeData stageData)
        {
            if (stageData == null)
                return;

            await FlushOnlineIdleReward();

            ClearStageRewardRequest request = new ClearStageRewardRequest
            {
                stage = stageData.GlobalStage,
                chapterId = stageData.ChapterId,
                localStage = stageData.LocalStage,
                rewards = StageRewardConverter.ToRewardDtos(stageData.ClearRewards)
            };

            // TODO SERVER:
            // Đây chỉ là cộng tạm dưới client để demo.
            // Sau này thay bằng:
            // var response = await serverApi.ClaimClearStageReward(request);
            // CurrencyManager.Instance.ApplyServerBalances(response.balances);
            ApplyRewardsLocallyForDemo(request.rewards, "ClearReward");

            await UniTask.CompletedTask;
        }

        public async UniTask FlushOnlineIdleReward()
        {
            if (currentStageData == null)
                return;

            if (!onlineIdleBuffer.HasAny())
                return;

            List<RewardAmountDto> rewards = onlineIdleBuffer.BuildDtos();

            if (rewards == null || rewards.Count == 0)
                return;

            OnlineIdleRewardRequest request = new OnlineIdleRewardRequest
            {
                stage = currentStageData.GlobalStage,
                chapterId = currentStageData.ChapterId,
                elapsedSeconds = onlineIdleElapsedSeconds,
                rewards = rewards
            };

            // TODO SERVER:
            // Đây chỉ là cộng tạm dưới client để demo.
            // Sau này thay bằng:
            // var response = await serverApi.FlushOnlineIdleReward(request);
            // CurrencyManager.Instance.ApplyServerBalances(response.balances);
            ApplyRewardsLocallyForDemo(request.rewards, "OnlineIdleReward");

            onlineIdleBuffer.Clear();
            onlineIdleElapsedSeconds = 0;

            await UniTask.CompletedTask;
        }

        public async UniTask ClaimOfflineAfkReward(
            int afkStage,
            int elapsedSeconds,
            StageReward[] offlineRewards
        )
        {
            OfflineAfkRewardRequest request = new OfflineAfkRewardRequest
            {
                afkStage = afkStage,
                elapsedSeconds = elapsedSeconds,
                rewards = StageRewardConverter.ToRewardDtos(offlineRewards)
            };

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

            Debug.Log($"[RewardSync][DEMO LOCAL] Apply {reason}");

            for (int i = 0; i < rewards.Count; i++)
            {
                RewardAmountDto reward = rewards[i];

                Debug.Log($"[RewardSync][DEMO LOCAL] {reward.currencyType} +{reward.amount}");

                // TODO SERVER:
                // Chỗ này chỉ làm tạm thời để demo.
                // Sau này không cộng trực tiếp ở client nữa.
                // Thay bằng apply balance server trả về.
                //
                // Ví dụ sau này:
                // CurrencyManager.Instance.ApplyServerBalances(response.balances);

                // TODO CLIENT:
                // Khi CurrencyManager chuyển sang BigNumber/string amount,
                // gọi hàm AddLocalDemoReward(currencyType, amount) tại đây.
            }
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