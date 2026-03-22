﻿using System;
using UnityEngine;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.PowerUpSystem;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GrowthDatabaseSO growthDatabase;
        [SerializeField] private int defaultGold = 100000;

        private const string SAVE_KEY = "GROWTH_SAVE";
        private const string GOLD_KEY = "GROWTH_GOLD";

        public GrowthSaveData SaveData { get; private set; }
        public GrowthSystemService Service { get; private set; }

        private int playerGold;
        public int PlayerGold => playerGold;

        public event Action OnGrowthChanged;
        public event Action<int> OnGoldChanged;
        public event Action<int, int, bool> OnTierReadyToUpgradePopup;

        private void Awake()
        {
            Load();
        }

        public void Save()
        {
            ES3.Save(SAVE_KEY, SaveData);
            ES3.Save(GOLD_KEY, playerGold);
        }

        public void Load()
        {
            if (ES3.KeyExists(SAVE_KEY))
            {
                SaveData = ES3.Load<GrowthSaveData>(SAVE_KEY);
                playerGold = ES3.Load<int>(GOLD_KEY, defaultGold);
            }
            else
            {
                SaveData = new GrowthSaveData();
                playerGold = defaultGold;
            }

            Service = new GrowthSystemService(growthDatabase, SaveData);

            OnGoldChanged?.Invoke(playerGold);
            OnGrowthChanged?.Invoke();

            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.TryInitializeSources();
                PowerUpManager.Instance.RebuildAndApply();
            }
        }

        public bool TryUpgrade(StatType stat, int amount)
        {
            int currentTier = SaveData.CurrentUnlockedTier;
            int nextTier = currentTier + 1;

            bool wasCurrentTierFullyMaxed = Service.IsTierFullyMaxed(currentTier);

            int gold = playerGold;
            int bought = Service.Upgrade(stat, amount, ref gold);
            if (bought <= 0)
                return false;

            playerGold = gold;
            Save();

            bool isCurrentTierFullyMaxedNow = Service.IsTierFullyMaxed(currentTier);
            bool canOpenNextTierPopup = Service.HasTier(nextTier);

            OnGoldChanged?.Invoke(playerGold);
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

        public void AddGold(int amount)
        {
            playerGold += amount;
            Save();

            OnGoldChanged?.Invoke(playerGold);
            OnGrowthChanged?.Invoke();
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

            if (ES3.KeyExists(GOLD_KEY))
                ES3.DeleteKey(GOLD_KEY);

            SaveData = new GrowthSaveData();
            playerGold = defaultGold;
            Service = new GrowthSystemService(growthDatabase, SaveData);

            Debug.Log("[Growth] DATA CLEARED");

            OnGoldChanged?.Invoke(playerGold);
            OnGrowthChanged?.Invoke();
        }

        [ContextMenu("DEBUG / Add 10000 Gold")]
        public void DebugAddGold()
        {
            AddGold(10000);
            Debug.Log("[Growth] Add Gold");
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