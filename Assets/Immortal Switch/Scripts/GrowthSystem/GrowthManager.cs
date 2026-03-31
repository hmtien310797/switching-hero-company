﻿using System;
 using Cysharp.Threading.Tasks;
 using Immortal_Switch.Scripts.Core;
 using UnityEngine;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthManager : Singleton<GrowthManager>
    {
        [Header("Config")]
        [SerializeField] private GrowthDatabaseSO growthDatabase;
        [SerializeField] private int defaultGold = 100000;

        private const string SAVE_KEY = "GROWTH_SAVE";

        public GrowthSaveData SaveData { get; private set; }
        public GrowthSystemService Service { get; private set; }

        public event Action OnGrowthChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int, int, bool> OnTierReadyToUpgradePopup;

        public override UniTask InitializeAsync()
        {
            Load();
            return UniTask.CompletedTask;
        }

        public void Save()
        {
            ES3.Save(SAVE_KEY, SaveData);
        }

        public void Load()
        {
            if (ES3.KeyExists(SAVE_KEY))
            {
                SaveData = ES3.Load<GrowthSaveData>(SAVE_KEY);
            }
            else
            {
                SaveData = new GrowthSaveData();
            }

            Service = new GrowthSystemService(growthDatabase, SaveData);
            OnGrowthChanged?.Invoke();
        }

        public bool TryUpgrade(StatType stat, int amount)
        {
            int currentTier = SaveData.CurrentUnlockedTier;
            int nextTier = currentTier + 1;

            bool wasCurrentTierFullyMaxed = Service.IsTierFullyMaxed(currentTier);
            
            int bought = Service.Upgrade(stat, amount);
            if (bought <= 0)
                return false;
            
            Save();

            bool isCurrentTierFullyMaxedNow = Service.IsTierFullyMaxed(currentTier);
            bool canOpenNextTierPopup = Service.HasTier(nextTier);
            
            OnGrowthChanged?.Invoke();

            if (!wasCurrentTierFullyMaxed && isCurrentTierFullyMaxedNow && canOpenNextTierPopup)
            {
                OnTierReadyToUpgradePopup?.Invoke(currentTier, nextTier, true);
            }

            return true;
        }

        public void UnlockTier(int tier)
        {
            int oldTier = SaveData.CurrentUnlockedTier;

            Service.UnlockTier(tier);

            if (SaveData.CurrentUnlockedTier != oldTier)
            {
                Save();
                OnGrowthChanged?.Invoke();
            }
        }

        public void CheckAndNotifyTierReady()
        {
            if (SaveData == null || Service == null)
                return;

            int currentTier = SaveData.CurrentUnlockedTier;
            int nextTier = currentTier + 1;

            bool isCurrentTierFullyMaxed = Service.IsTierFullyMaxed(currentTier);
            bool canOpenNextTierPopup = Service.HasTier(nextTier);

            OnTierReadyToUpgradePopup?.Invoke(currentTier, nextTier, isCurrentTierFullyMaxed && canOpenNextTierPopup);
        }

        public void ClearData()
        {
            if (ES3.KeyExists(SAVE_KEY))
                ES3.DeleteKey(SAVE_KEY);

            SaveData = new GrowthSaveData();
            Service = new GrowthSystemService(growthDatabase, SaveData);

            Debug.Log("[Growth] DATA CLEARED");
            OnGrowthChanged?.Invoke();
        }

        [ContextMenu("DEBUG / Unlock Next Tier")]
        public void DebugUnlockNextTier()
        {
            UnlockTier(SaveData.CurrentUnlockedTier + 1);
            Debug.Log("[Growth] Unlock Tier");
        }

        [ContextMenu("DEBUG / Save")]
        public void DebugSave()
        {
            Save();
            Debug.Log("[Growth] Saved");
        }

        [ContextMenu("DEBUG / Load")]
        public void DebugLoad()
        {
            Load();
            Debug.Log("[Growth] Loaded");
        }

        [ContextMenu("DEBUG / CLEAR ALL DATA")]
        public void DebugClearData()
        {
            ClearData();
        }
    }
}