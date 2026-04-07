using System;
using Immortal_Switch.Hero;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem
{
    public class HeroSummonManager : MonoBehaviour
    {
        public static HeroSummonManager Instance { get; private set; }

        [SerializeField] private HeroSummonConfigSO summonConfig;
        [SerializeField] private string saveKey = "hero_summon_save";

        private HeroSummonSaveData saveData;
        [ShowInInspector]
        private HeroSummonService service;
        private IHeroSummonCurrencyGateway currencyGateway;

        public HeroSummonService Service => service;
        public HeroSummonSaveData SaveData => saveData;
        public HeroSummonConfigSO Config => summonConfig;

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
            Init(new GameCurrencyGateway());
        }

        public void Init(IHeroSummonCurrencyGateway gateway)
        {
            currencyGateway = gateway;

            service = new HeroSummonService(
                summonConfig,
                saveData,
                currencyGateway,
                HeroProgressionManager.Instance);
        }

        public void Save()
        {
            ES3.Save(saveKey, saveData);
        }

        public void Load()
        {
            if (ES3.KeyExists(saveKey))
                saveData = ES3.Load<HeroSummonSaveData>(saveKey);
            else
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
                Save();
                NotifyChanged();
            }

            return result;
        }

        public int GetCurrentSummonLevel()
        {
            return service != null ? service.GetCurrentSummonLevel() : 1;
        }

        public bool ClaimReward(int summonLevel, IHeroSummonRewardReceiver rewardReceiver)
        {
            if (service == null) return false;

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