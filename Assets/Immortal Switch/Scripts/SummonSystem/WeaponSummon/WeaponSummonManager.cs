using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    public class WeaponSummonManager : Singleton<WeaponSummonManager>
    {
        [Header("Runtime")]
        [SerializeField] private WeaponManager weaponManager;

        [Header("Save")]
        [SerializeField] private string saveKey = "weapon_summon_save";

        private WeaponSummonSaveData saveData;
        private WeaponSummonService service;

        public WeaponSummonConfigSO Config => config;
        public WeaponSummonSaveData SaveData => saveData;
        public WeaponSummonService Service => service;

        public event Action OnSummonDataChanged;
        private WeaponSummonConfigSO config;

        public override UniTask InitializeAsync()
        {
            config = DatabaseManager.Instance.WeaponSummonConfig;
            Load();
            InitService();
            return UniTask.CompletedTask;
        }

        private void InitService()
        {
            if (weaponManager == null)
                weaponManager = WeaponManager.Instance;

            service = new WeaponSummonService(
                config,
                saveData,
                new GameWeaponSummonCurrencyGateway(),
                weaponManager
            );
        }

        public bool CanSummon(
            string optionId,
            out WeaponSummonPaymentType paymentType,
            out int paidAmount)
        {
            paymentType = WeaponSummonPaymentType.Ticket;
            paidAmount = 0;

            if (service == null)
                return false;

            var option = service.GetOption(optionId);
            return service.CanSummon(option, out paymentType, out paidAmount);
        }
        
        public int GetCurrentSummonLevel()
        {
            return service != null ? service.GetCurrentSummonLevel() : 1;
        }

        private void Load()
        {
            saveData = new WeaponSummonSaveData();
        }

        public void NotifyChanged()
        {
            OnSummonDataChanged?.Invoke();
        }

        /// <summary>
        /// Cập nhật local save data từ response của server sau mỗi lần summon.
        /// </summary>
        public void ApplyServerResponse(SummonExecuteResponse response)
        {
            saveData.TotalRoll   = response.NewTotalRoll;
            saveData.SummonLevel = response.NewSummonLevel;
            NotifyChanged();
        }

        /// <summary>Đồng bộ toàn bộ summon state từ server (gọi sau login).</summary>
        public void ApplySummonState(BasicSummonState state)
        {
            if (state == null) return;
            saveData.TotalRoll   = state.TotalRoll;
            saveData.SummonLevel = state.SummonLevel;
            if (state.ClaimedRewardLevels != null)
                saveData.ClaimedRewardLevels = new System.Collections.Generic.List<int>(state.ClaimedRewardLevels);
            NotifyChanged();
        }
    }
}