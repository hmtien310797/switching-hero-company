using System;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Skill.UI;
using Immortal_Switch.Scripts.SkillSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill
{
    public class SkillSummonManager : Singleton<SkillSummonManager>
    {
        [ShowInInspector]
        private SkillSummonSaveData saveData;
        [ShowInInspector]
        private SkillSummonService service;
        private ISkillSummonCurrencyGateway currencyGateway;
        private SkillProgressionService progressionService;

        public SkillSummonService Service => service;
        public SkillSummonSaveData SaveData => saveData;
        public SkillSummonConfigSO Config => summonConfig;

        private SkillSummonConfigSO summonConfig;
        public event Action OnSummonDataChanged;

        public override UniTask InitializeAsync()
        {
            summonConfig = DatabaseManager.Instance.SkillSummonConfig;
            Load();
            Init(new GameSkillSummonCurrencyGateway());
            return UniTask.CompletedTask;
        }

        private void Init(ISkillSummonCurrencyGateway gateway)
        {
            currencyGateway = gateway;
            progressionService = new SkillProgressionService();

            service = new SkillSummonService(
                summonConfig,
                saveData,
                currencyGateway,
                progressionService);
        }

        private void Load()
        {
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

        public int GetCurrentSummonLevel()
        {
            return service != null ? service.GetCurrentSummonLevel() : 1;
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
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnSummonDataChanged?.Invoke();
        }
    }
}