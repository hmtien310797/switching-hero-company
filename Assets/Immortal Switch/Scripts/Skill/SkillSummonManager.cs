using System;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.SkillSummon
{
    public class SkillSummonManager : MonoBehaviour
    {
        public static SkillSummonManager Instance { get; private set; }

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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
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

        public bool CanSummon(string optionId, out SkillSummonPaymentType paymentType, out int paidAmount)
        {
            paymentType = SkillSummonPaymentType.Ticket;
            paidAmount = 0;

            if (service == null)
                return false;

            var option = service.GetOption(optionId);
            return service.CanSummon(option, out paymentType, out paidAmount);
        }

        public SkillSummonResult ExecuteSummon(string optionId, SkillSummonPaymentType paymentType)
        {
            if (service == null)
                return null;

            var option = service.GetOption(optionId);
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

        public bool ClaimReward(int summonLevel, ISkillSummonRewardReceiver rewardReceiver)
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