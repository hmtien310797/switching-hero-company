using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Skill.UI;
using Immortal_Switch.Scripts.SkillSummon;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public class SkillSummonManager : Singleton<SkillSummonManager>
    {
        [SerializeField] private SkillSummonConfigSO summonConfig;
        [SerializeField] private string saveKey = "skill_summon_save";

        [ShowInInspector]
        private SkillSummonSaveData saveData;
        [ShowInInspector]
        private SkillSummonService service;
        private ISkillSummonCurrencyGateway currencyGateway;
        private SkillProgressionService progressionService;

        public SkillSummonService Service => service;
        public SkillSummonSaveData SaveData => saveData;
        public SkillSummonConfigSO Config => summonConfig;

        public event Action OnSummonDataChanged;

        protected override void Awake()
        {
            base.Awake();
            Load();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void Start()
        {
            Init(new GameSkillSummonCurrencyGateway());
        }

        public void Init(ISkillSummonCurrencyGateway gateway)
        {
            currencyGateway = gateway;
            progressionService = new SkillProgressionService();

            service = new SkillSummonService(
                summonConfig,
                saveData,
                currencyGateway,
                progressionService);
        }

        public void Save()
        {
            ES3.Save(saveKey, saveData);
        }

        public void Load()
        {
            if (ES3.KeyExists(saveKey))
                saveData = ES3.Load<SkillSummonSaveData>(saveKey);
            else
                saveData = new SkillSummonSaveData();
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

        public SkillSummonResult ExecuteSummon(string optionId, SummonPaymentType paymentType)
        {
            if (service == null)
                return null;

            var option = service.GetOption(optionId);
            Debug.Log($"option: {JsonConvert.SerializeObject(option)}");
            var result = service.ExecuteSummon(option, paymentType);

            if (result != null)
            {
                Save();
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
            if (service == null)
                return false;

            bool result = service.ClaimReward(summonLevel, rewardReceiver);
            if (result)
            {
                Save();
                NotifyChanged();
            }

            return result;
        }

        /// <summary>
        /// Cập nhật local save data từ response của server sau mỗi lần summon.
        /// </summary>
        public void ApplyServerResponse(SummonExecuteResponse response)
        {
            saveData.TotalRoll = response.NewTotalRoll;
            saveData.SummonLevel = response.NewSummonLevel;

            if (response.Entries != null)
            {
                foreach (var entry in response.Entries)
                {
                    if (entry.SkillId <= 0) continue;

                    if (entry.IsNew)
                    {
                        SkillInventorySaveService.SetOwned(entry.SkillId, true);
                        SkillInventorySaveService.SetLevel(entry.SkillId, 1);
                    }

                    if (entry.ShardGained > 0)
                        SkillInventorySaveService.AddShard(entry.SkillId, entry.ShardGained);
                }

                SkillInventorySaveService.Save();
                UserDataCache.Instance?.ApplySkillSummonEntries(response.Entries);
            }

            Save();
            NotifyChanged();
        }

        /// <summary>Đồng bộ toàn bộ summon state từ server (gọi sau login).</summary>
        public void ApplySummonState(BasicSummonState state)
        {
            if (state == null) return;
            saveData.TotalRoll = state.TotalRoll;
            saveData.SummonLevel = state.SummonLevel;
            if (state.ClaimedRewardLevels != null)
                saveData.ClaimedRewardLevels = new System.Collections.Generic.List<int>(state.ClaimedRewardLevels);
            Save();
            NotifyChanged();
        }

        public void ResetSummonData()
        {
            saveData = new SkillSummonSaveData();
            progressionService = new SkillProgressionService();
            service = new SkillSummonService(
                summonConfig,
                saveData,
                currencyGateway,
                progressionService);

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