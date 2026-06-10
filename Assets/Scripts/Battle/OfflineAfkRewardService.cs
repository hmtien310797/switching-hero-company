using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    public class OfflineAfkRewardService : MonoBehaviour
    {
        private const string LastAfkCheckpointUnixKey = "OFFLINE_AFK_LAST_CHECKPOINT_UNIX";
        private const string LastAfkStageKey = "OFFLINE_AFK_STAGE";

        [Header("References")] [SerializeField]
        private StageDataResolverSO stageDataResolver;

        [SerializeField] 
        private RewardSyncService rewardSyncService;

        [Header("Config")] [SerializeField] private int maxOfflineSeconds = 12 * 60 * 60;
        [SerializeField] private int minClaimSeconds = 60;

        [Header("Debug")] [SerializeField] private bool enableDebugLog = true;

        private int currentAfkStage = 1;
        private bool initialized;

        public void Initialize(int currentStage)
        {
            currentAfkStage = Mathf.Max(1, currentStage);
            initialized = true;

            TryClaimOfflineAfkReward().Forget();

            SaveCheckpoint(currentAfkStage);
        }

        public void SetCurrentAfkStage(int stage)
        {
            currentAfkStage = Mathf.Max(1, stage);

            // TODO SERVER:
            // Tạm thời lưu checkpoint local dưới client.
            // Sau này server sẽ lưu lastAfkCheckpointTime và afkStage.
            SaveCheckpoint(currentAfkStage);
        }

        public async UniTask TryClaimOfflineAfkReward()
        {
            if (stageDataResolver == null || rewardSyncService == null)
            {
                Debug.LogWarning("[OfflineAFK] Missing references.");
                return;
            }

            if (!PlayerPrefs.HasKey(LastAfkCheckpointUnixKey))
            {
                if (enableDebugLog)
                    Debug.Log("[OfflineAFK] No checkpoint found. Skip claim.");

                return;
            }

            // TODO SERVER:
// Đây là logic tạm thời dưới client để demo Offline AFK.
// Sau này không dùng PlayerPrefs/client time làm source of truth.
// Server sẽ trả về:
// - serverNow
// - lastAfkCheckpointTime
// - afkStage
// - maxOfflineSeconds
// Client chỉ hiển thị/claim theo data server trả về.
            long lastUnix = long.Parse(PlayerPrefs.GetString(LastAfkCheckpointUnixKey, "0"));
            long nowUnix = GetUnixNow();

            int elapsedSeconds = (int)Math.Max(0, nowUnix - lastUnix);

            if (elapsedSeconds < minClaimSeconds)
            {
                if (enableDebugLog)
                    Debug.Log($"[OfflineAFK] Elapsed too small: {elapsedSeconds}s. Skip claim.");

                return;
            }

            elapsedSeconds = Mathf.Min(elapsedSeconds, maxOfflineSeconds);

            int afkStage = PlayerPrefs.GetInt(LastAfkStageKey, currentAfkStage);
            afkStage = Mathf.Max(1, afkStage);

            StageRuntimeData afkStageData = ResolveStageForAfk(afkStage);
            if (afkStageData == null)
            {
                Debug.LogError($"[OfflineAFK] Cannot resolve afk stage: {afkStage}");
                return;
            }

            StageReward[] rewards = CalculateOfflineRewards(
                afkStageData.BaseRewards,
                elapsedSeconds
            );

            if (rewards == null || rewards.Length == 0)
            {
                if (enableDebugLog)
                    Debug.Log("[OfflineAFK] No offline rewards.");

                SaveCheckpoint(currentAfkStage);
                return;
            }

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[OfflineAFK] Claim offline reward. " +
                    $"AfkStage={afkStage}, Elapsed={elapsedSeconds}s"
                );
            }

            // TODO SERVER:
// Hiện tại client tự tính offline reward rồi gọi RewardSyncService để cộng local demo.
// Sau này đổi thành:
// - Client gọi API ClaimOfflineAfkReward()
// - Server tự check elapsed bằng server time
// - Server tự cộng reward
// - Server trả balances
// - Client gọi CurrencyManager.ApplyServerBalances(response.balances)
            await rewardSyncService.ClaimOfflineAfkReward(
                afkStage,
                elapsedSeconds,
                rewards
            );

            SaveCheckpoint(currentAfkStage);
        }

        private StageRuntimeData ResolveStageForAfk(int stage)
        {
            return stageDataResolver.Resolve(stage);
        }

        private StageReward[] CalculateOfflineRewards(StageReward[] baseRewards, int elapsedSeconds)
        {
            if (baseRewards == null || baseRewards.Length == 0)
                return Array.Empty<StageReward>();

            // Quy ước hiện tại: BaseRewards = reward per minute.
            double minutes = elapsedSeconds / 60d;

            StageReward[] result = new StageReward[baseRewards.Length];

            for (int i = 0; i < baseRewards.Length; i++)
            {
                StageReward reward = baseRewards[i];

                result[i] = new StageReward(
                    reward.currencyType,
                     (reward.Amount * minutes).FloorToIntSafe()
                );
            }

            return result;
        }

        private void SaveCheckpoint(int afkStage)
        {
            // TODO SERVER:
            // Đây chỉ là checkpoint local tạm thời để demo.
            // Sau này checkpoint AFK phải được lưu trên server:
            // - lastAfkCheckpointTime
            // - afkStage
            // - maxOfflineSeconds/cap
            // Client không nên tự quyết định thời gian offline thật.
            long nowUnix = GetUnixNow();

            PlayerPrefs.SetString(LastAfkCheckpointUnixKey, nowUnix.ToString());
            PlayerPrefs.SetInt(LastAfkStageKey, Mathf.Max(1, afkStage));
            PlayerPrefs.Save();

            if (enableDebugLog)
            {
                Debug.Log(
                    $"[OfflineAFK] Save checkpoint. " +
                    $"Stage={afkStage}, Unix={nowUnix}"
                );
            }
        }

        private static long GetUnixNow()
        {
            // TODO SERVER:
            // Tạm dùng client UTC time để demo.
            // Sau này phải dùng serverNow để tránh player chỉnh giờ máy.
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private void OnApplicationPause(bool pause)
        {
            if (!initialized)
                return;

            if (pause)
                SaveCheckpoint(currentAfkStage);
            else
                TryClaimOfflineAfkReward().Forget();
        }

        private void OnApplicationQuit()
        {
            if (!initialized)
                return;

            SaveCheckpoint(currentAfkStage);
        }
    }
}