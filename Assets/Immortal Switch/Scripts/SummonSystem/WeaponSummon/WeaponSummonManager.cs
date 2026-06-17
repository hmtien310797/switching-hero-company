using System;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon
{
    public class WeaponSummonManager : MonoBehaviour
    {
        public static WeaponSummonManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private WeaponSummonConfigSO config;

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Load();
            InitService();
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
        
        public bool ClaimReward(int rewardLevel, ISummonRewardReceiver receiver)
        {
            if (service == null)
                return false;

            bool success = service.ClaimReward(rewardLevel, receiver);
            if (!success)
                return false;

            Save();
            NotifyChanged();

            return true;
        }

        public WeaponSummonResult ExecuteSummon(
            string optionId,
            WeaponSummonPaymentType paymentType)
        {
            if (service == null)
                return null;

            var option = service.GetOption(optionId);
            if (option == null)
                return null;

            var result = service.ExecuteSummon(option, paymentType);
            if (result == null)
                return null;

            Save();
            NotifyChanged();

            return result;
        }

        public int GetCurrentSummonLevel()
        {
            return service != null ? service.GetCurrentSummonLevel() : 1;
        }

        public void Save()
        {
            if (saveData == null)
                saveData = new WeaponSummonSaveData();

            ES3.Save(saveKey, saveData);
        }

        public void Load()
        {
            saveData = ES3.Load(saveKey, new WeaponSummonSaveData());

            if (saveData == null)
                saveData = new WeaponSummonSaveData();

            if (saveData.ClaimedRewardLevels == null)
                saveData.ClaimedRewardLevels = new System.Collections.Generic.List<int>();
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
            saveData.TotalRoll = response.NewTotalRoll;
            Save();
            NotifyChanged();
        }

        /// <summary>Đồng bộ toàn bộ summon state từ server (gọi sau login).</summary>
        public void ApplySummonState(BasicSummonState state)
        {
            if (state == null) return;
            saveData.TotalRoll = state.TotalRoll;
            if (state.ClaimedRewardLevels != null)
                saveData.ClaimedRewardLevels = new System.Collections.Generic.List<int>(state.ClaimedRewardLevels);
            Save();
            NotifyChanged();
        }

        public void ClearSave()
        {
            saveData = new WeaponSummonSaveData();

            if (ES3.KeyExists(saveKey))
                ES3.DeleteKey(saveKey);

            Save();
            NotifyChanged();
        }
    }
}