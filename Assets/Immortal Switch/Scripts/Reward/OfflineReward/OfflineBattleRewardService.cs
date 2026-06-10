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

        private const string LastExitStageKey = "OfflineBattle.LastExitStage";
        private const string LastExitTimeKey = "OfflineBattle.LastExitTime";

        [Header("Data")]
        [SerializeField] private StageDataResolverSO stageDataResolver;

        [Header("Config")]
        [SerializeField] private bool baseRewardIsPerMinute = true;
        [SerializeField] private int minOfflineSecondsToShow = 60;
        [SerializeField] private int maxOfflineSeconds = 12 * 60 * 60;
        [SerializeField] private int defeatsPerMinute = 200;

        private OfflineBattleRewardResult currentResult;

        public bool HasReward => currentResult != null &&
                                 currentResult.Rewards != null &&
                                 currentResult.Rewards.Count > 0;

        public OfflineBattleRewardResult CurrentResult => currentResult;

        private void Awake()
        {
            Instance = this;
        }

        public void SaveExitState(int currentStage)
        {
            currentStage = Mathf.Max(1, currentStage);

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            PlayerPrefs.SetInt(LastExitStageKey, currentStage);
            PlayerPrefs.SetString(LastExitTimeKey, now.ToString());
            PlayerPrefs.Save();

            Debug.Log($"[OfflineBattle] Save exit. Stage={currentStage}, Time={now}");
        }

        public void CalculateAndShowOnReturn()
        {
            currentResult = CalculateReward();

            if (!HasReward)
                return;

            UIManager.Instance.TogglePopupAsync<OfflineBattleRewardPopupView>(
                new OfflineBattleRewardOpenParam
                {
                    Result = currentResult
                }
            );
        }

        private OfflineBattleRewardResult CalculateReward()
        {
            int stage = PlayerPrefs.GetInt(LastExitStageKey, 0);
            string timeString = PlayerPrefs.GetString(LastExitTimeKey, string.Empty);

            if (stage <= 0)
                return null;

            if (!long.TryParse(timeString, out long lastExitTime))
                return null;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            int elapsedSeconds = Mathf.Max(0, (int)(now - lastExitTime));
            elapsedSeconds = Mathf.Min(elapsedSeconds, maxOfflineSeconds);

            if (elapsedSeconds < minOfflineSecondsToShow)
                return null;

            if (stageDataResolver == null)
            {
                Debug.LogError("[OfflineBattle] Missing StageDataResolverSO.");
                return null;
            }

            StageRuntimeData stageData = stageDataResolver.Resolve(stage);
            if (stageData == null)
            {
                Debug.LogError($"[OfflineBattle] Missing stage data: {stage}");
                return null;
            }

            OfflineBattleRewardResult result = new OfflineBattleRewardResult
            {
                Stage = stage,
                OfflineSeconds = elapsedSeconds,
                MaxOfflineSeconds = maxOfflineSeconds,
                DefeatsPerMinute = defeatsPerMinute,
                MonstersDefeated = Mathf.FloorToInt(defeatsPerMinute * (elapsedSeconds / 60f)),
                Rewards = new List<StageReward>()
            };

            CalculateRewards(stageData, elapsedSeconds, result);

            return result;
        }

        private void CalculateRewards(
            StageRuntimeData stageData,
            int seconds,
            OfflineBattleRewardResult result
        )
        {
            if (stageData.BaseRewards == null)
                return;

            double timeMultiplier = baseRewardIsPerMinute
                ? seconds / 60d
                : seconds;

            for (int i = 0; i < stageData.BaseRewards.Length; i++)
            {
                StageReward reward = stageData.BaseRewards[i];

                if (!reward.IsValid)
                    continue;

                if (reward.currencyType == CurrencyType.none)
                {
                    Debug.LogError($"[OfflineBattle] Unknown currency type: {reward.currencyType}");
                    continue;
                }

                BigNumber amountDouble = reward.Amount * timeMultiplier;

                if (amountDouble <= 0)
                    continue;

                AddOrMerge(result.Rewards, reward.currencyType, amountDouble);
            }
        }

        private void AddOrMerge(
            List<StageReward> rewards,
            CurrencyType currencyType,
            BigNumber amount
        )
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i].currencyType == currencyType)
                {
                    rewards[i].Amount += amount;
                    return;
                }
            }

            rewards.Add(new StageReward(currencyType, amount));
        }

        public async UniTask ClaimCurrentReward()
        {
            if (!HasReward)
                return;

            if (CurrencyLedgerService.Instance == null)
            {
                Debug.LogError("[OfflineBattle] Missing CurrencyLedgerService.");
                return;
            }

            for (int i = 0; i < currentResult.Rewards.Count; i++)
            {
                StageReward reward = currentResult.Rewards[i];

                CurrencyLedgerService.Instance.AddOrMergeIncome(
                    reward.currencyType,
                    reward.Amount,
                    CurrencyTransactionReason.OfflineAfkReward
                );
            }

            ClearSavedExitState();

            currentResult = null;

            await CurrencyLedgerService.Instance.SyncPendingTransactions();
        }

        public void ClearSavedExitState()
        {
            PlayerPrefs.DeleteKey(LastExitStageKey);
            PlayerPrefs.DeleteKey(LastExitTimeKey);
            PlayerPrefs.Save();
        }
    }
}