using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Reward
{
    public static class FarmingIdleScreenService
    {
        private static readonly FarmingIdleScreenSession session = new();

        private static bool isActive;
        private static StageRuntimeData currentStageData;

        public static bool IsActive => isActive;
        public static FarmingIdleScreenSession Session => session;
        public static StageRuntimeData CurrentStageData => currentStageData;

        public static event Action<StageRuntimeData> OnStageDataChanged;

        public static void Open(StageRuntimeData stageData)
        {
            if (isActive)
                return;

            if (stageData == null)
            {
                Debug.LogError("[FarmingIdleScreenService] Missing stage data.");
                return;
            }

            isActive = true;
            currentStageData = stageData;

            session.Begin();

            UIManager.Instance.TogglePopupAsync<FarmingIdleScreenView>(
                new FarmingIdleScreenOpenParam
                {
                    StageData = stageData,
                    Session = session
                }
            ).Forget();
        }

        public static void Close()
        {
            if (!isActive)
                return;

            isActive = false;
            currentStageData = null;

            session.End();

            UIManager.Instance.TogglePopupAsync<FarmingIdleScreenView>(false).Forget();
        }

        public static void UpdateStageData(StageRuntimeData stageData)
        {
            if (!isActive)
                return;

            if (stageData == null)
                return;

            currentStageData = stageData;
            OnStageDataChanged?.Invoke(stageData);
        }

        public static void AddMonsterKill()
        {
            if (!isActive)
                return;

            session.AddMonsterKill();
        }

        public static void AddEarnedReward(CurrencyType currencyType, BigNumber amount)
        {
            if (!isActive)
                return;

            session.AddEarnedReward(currencyType, amount);
        }

        public static void ForceReset()
        {
            isActive = false;
            currentStageData = null;

            session.End();
            OnStageDataChanged = null;
        }
    }
}