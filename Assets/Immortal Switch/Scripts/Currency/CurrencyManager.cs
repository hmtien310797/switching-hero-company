using System;
using System.Collections.Generic;
using Scripts.Common;
using UnityEngine;

namespace Immortal_Switch.Scripts.Currency
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private string saveKey = "currency_save_data";

        private CurrencySaveData saveData;
        private Dictionary<CurrencyType, CurrencyEntry> entryMap;

        public event Action<CurrencyChangedArgs> OnCurrencyChanged;
        public event Action OnAnyCurrencyChanged;

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
            BuildMap();
            EnsureAllCurrencyTypesExist();
        }

        private void BuildMap()
        {
            entryMap = new Dictionary<CurrencyType, CurrencyEntry>();

            if (saveData == null)
                saveData = new CurrencySaveData();

            for (int i = 0; i < saveData.Entries.Count; i++)
            {
                var entry = saveData.Entries[i];
                if (entry == null) continue;

                if (!entryMap.ContainsKey(entry.CurrencyType))
                    entryMap.Add(entry.CurrencyType, entry);
            }
        }

        private void EnsureAllCurrencyTypesExist()
        {
            var allTypes = Enum.GetValues(typeof(CurrencyType));
            for (int i = 0; i < allTypes.Length; i++)
            {
                var type = (CurrencyType)allTypes.GetValue(i);
                if (!entryMap.ContainsKey(type))
                {
                    var entry = new CurrencyEntry
                    {
                        CurrencyType = type,
                        Amount = 0
                    };

                    saveData.Entries.Add(entry);
                    entryMap.Add(type, entry);
                }
            }

            Save();
        }

        public int Get(CurrencyType currencyType)
        {
            return GetEntry(currencyType).Amount;
        }

        public bool HasEnough(CurrencyType currencyType, int amount)
        {
            if (amount <= 0) return true;
            return Get(currencyType) >= amount;
        }

        public void Set(CurrencyType currencyType, int amount)
        {
            if (amount < 0) amount = 0;

            var entry = GetEntry(currencyType);
            int oldAmount = entry.Amount;

            if (oldAmount == amount)
                return;

            entry.Amount = amount;
            Save();
            NotifyChanged(currencyType, oldAmount, entry.Amount);
        }

        public void Add(CurrencyType currencyType, int amount)
        {
            if (amount <= 0) return;

            var entry = GetEntry(currencyType);
            int oldAmount = entry.Amount;

            entry.Amount += amount;

            Save();
            NotifyChanged(currencyType, oldAmount, entry.Amount);
        }

        public bool Spend(CurrencyType currencyType, int amount)
        {
            if (amount <= 0) return true;

            var entry = GetEntry(currencyType);
            if (entry.Amount < amount)
                return false;

            int oldAmount = entry.Amount;
            entry.Amount -= amount;

            Save();
            NotifyChanged(currencyType, oldAmount, entry.Amount);

            return true;
        }

        public void ResetAll()
        {
            if (saveData == null)
                saveData = new CurrencySaveData();

            for (int i = 0; i < saveData.Entries.Count; i++)
            {
                saveData.Entries[i].Amount = 0;
            }

            Save();

            var allTypes = Enum.GetValues(typeof(CurrencyType));
            for (int i = 0; i < allTypes.Length; i++)
            {
                var type = (CurrencyType)allTypes.GetValue(i);
                NotifyChanged(type, 0, 0);
            }
        }

        public void Save()
        {
            ES3.Save(saveKey, saveData);
        }

        public void Load()
        {
            
            if (ES3.KeyExists(saveKey))
                saveData = ES3.Load<CurrencySaveData>(saveKey);
            else
                saveData = new CurrencySaveData
                {
                    Entries = new List<CurrencyEntry>
                    {
                        new CurrencyEntry
                        {
                            CurrencyType = CurrencyType.HeroTicket,
                            Amount = UserDataCache.Instance.initialHeroTicket
                        },
                        new CurrencyEntry
                        {
                            CurrencyType = CurrencyType.Diamond,
                            Amount = UserDataCache.Instance.initialDiamond
                        },
                        new CurrencyEntry
                        {
                            CurrencyType = CurrencyType.Gold,
                            Amount = UserDataCache.Instance.initialGold
                        }
                    }
                };
        }

        private CurrencyEntry GetEntry(CurrencyType currencyType)
        {
            if (entryMap == null)
                BuildMap();

            if (!entryMap.TryGetValue(currencyType, out var entry))
            {
                entry = new CurrencyEntry
                {
                    CurrencyType = currencyType,
                    Amount = 0
                };

                saveData.Entries.Add(entry);
                entryMap.Add(currencyType, entry);
            }

            return entry;
        }

        private void NotifyChanged(CurrencyType currencyType, int oldAmount, int newAmount)
        {
            OnCurrencyChanged?.Invoke(new CurrencyChangedArgs
            {
                CurrencyType = currencyType,
                OldAmount = oldAmount,
                NewAmount = newAmount,
                Delta = newAmount - oldAmount
            });

            OnAnyCurrencyChanged?.Invoke();
        }
    }
}