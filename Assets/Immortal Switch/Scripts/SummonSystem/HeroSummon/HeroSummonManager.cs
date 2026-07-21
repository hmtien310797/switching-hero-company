using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.SummonSystem.Shared.Base;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public class HeroSummonManager : Singleton<HeroSummonManager>
    {
        [SerializeField] private string saveKey = "hero_summon_save";

        private HeroSummonSaveData saveData;
        [ShowInInspector]
        private HeroSummonService service;
        private IHeroSummonCurrencyGateway currencyGateway;

        public HeroSummonService Service => service;
        public HeroSummonSaveData SaveData => saveData;
        public HeroSummonConfigSO Config => summonConfig;

        private HeroSummonConfigSO summonConfig;
        public event Action OnSummonDataChanged;

        public override UniTask InitializeAsync()
        {
            summonConfig = DatabaseManager.Instance.HeroSummonConfig;
            Load();
            Init(new GameCurrencyGateway());
            return UniTask.CompletedTask;
        }

        private void Init(IHeroSummonCurrencyGateway gateway)
        {
            currencyGateway = gateway;

            service = new HeroSummonService(
                summonConfig,
                saveData,
                currencyGateway,
                HeroProgressionManager.Instance);
        }
        
        private void Load()
        {
            saveData = new HeroSummonSaveData();
        }

        public bool CanSummon(string optionId, out SummonPaymentType paymentType, out int paidAmount)
        {
            paymentType = SummonPaymentType.Ticket;
            paidAmount = 0;

            if (service == null)
                return false;

            var option = service.GetOption(optionId);
            return service.CanSummon(option, out paymentType, out paidAmount);
        }

        public HeroSummonResult ExecuteSummon(string optionId, SummonPaymentType paymentType)
        {
            if (service == null)
            {
                Debug.LogError("HeroSummonManager.ExecuteSummon: service is null");
                return null;
            }

            var option = service.GetOption(optionId);
            var result = service.ExecuteSummon(option, paymentType);

            if (result != null)
            {
                NotifyChanged();
            }

            return result;
        }

        public int GetCurrentSummonLevel()
        {
            return service != null ? service.GetCurrentSummonLevel() : 1;
        }

        public bool ClaimReward(int summonLevel, ISummonRewardReceiver rewardReceiver)
        {
            if (service == null) return false;

            bool result = service.ClaimReward(summonLevel, rewardReceiver);
            if (result)
            {
                NotifyChanged();
            }

            return result;
        }

        /// <summary>
        /// Cập nhật local save data từ response của server sau mỗi lần summon.
        /// Pity counter được đồng bộ riêng qua GetSummonStateAsync (sau login).
        /// </summary>
        public void ApplyServerResponse(SummonExecuteResponse response)
        {
            saveData.TotalRoll   = response.NewTotalRoll;
            saveData.SummonLevel = response.NewSummonLevel;
            NotifyChanged();
        }

        /// <summary>Đồng bộ toàn bộ summon state từ server (gọi sau login).</summary>
        public void ApplySummonState(HeroSummonState state)
        {
            if (state == null) return;
            saveData.TotalRoll        = state.TotalRoll;
            saveData.SummonLevel      = state.SummonLevel;
            saveData.PityMissCounter  = state.PityMissCounter;
            if (state.ClaimedRewardLevels != null)
                saveData.ClaimedRewardLevels = new System.Collections.Generic.List<int>(state.ClaimedRewardLevels);
            NotifyChanged();
        }

        public void ResetSummonData()
        {
            saveData = new HeroSummonSaveData();
            service = new HeroSummonService(
                summonConfig,
                saveData,
                currencyGateway,
                HeroProgressionManager.Instance);

            if (ES3.KeyExists(saveKey))
                ES3.DeleteKey(saveKey);

            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnSummonDataChanged?.Invoke();
        }
    }
}