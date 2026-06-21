using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;

namespace Immortal_Switch.Scripts.Hero
{
    public class HeroProgressionManager : Singleton<HeroProgressionManager>
    {
        [SerializeField] private HeroProgressionDatabaseSO database;
        private const string saveKey = "hero_progression_save";

        private HeroCollectionSaveData saveData;
        private HeroProgressionService service;

        public HeroProgressionService Service => service;
        public HeroProgressionDatabaseSO Database => database;
        public event Action<HeroCollectionChangedArgs> OnHeroCollectionChanged;
        private readonly Dictionary<int, List<HeroProgressionRuntimeBridge>> heroBridges = new();

        protected override void Awake()
        {
            base.Awake();

            Load();
            service = new HeroProgressionService(database, saveData);
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        public void Save()
        {
            ES3.Save(saveKey, saveData);
        }

        public void Load()
        {
            if (ES3.KeyExists(saveKey))
                saveData = ES3.Load<HeroCollectionSaveData>(saveKey);
            else
                saveData = new HeroCollectionSaveData();
        }

        public void ResetData()
        {
            saveData = new HeroCollectionSaveData();
            service = new HeroProgressionService(database, saveData);

            if (ES3.KeyExists(saveKey))
                ES3.DeleteKey(saveKey);
        }

        public bool UnlockHero(HeroDataSO hero)
        {
            bool result = service.UnlockHero(hero);
            if (result) Save();
            return result;
        }

        public void AddShard(int heroId, int amount)
        {
            service.AddShard(heroId, amount);
            Save();
        }

        public bool UpgradeHero(int heroId)
        {
            if (service == null) return false;

            bool result = service.UpgradeHero(heroId);
            if (!result) return false;

            Save();
            NotifyHeroCollectionChanged(heroId, HeroCollectionChangeType.HeroUpgraded);
            RefreshHeroRuntime(heroId);

            return true;
        }
        
        public void ResetHero(int heroId)
        {
            if (service == null || database == null) return;

            var owned = service.GetOrCreateOwnedHero(heroId);
            var config = database.GetProgressionConfig(heroId);

            if (config == null)
            {
                Debug.LogError($"ResetHero failed: Missing HeroProgressionConfig for heroId = {heroId}");
                return;
            }

            owned.IsUnlocked = false;
            owned.CurrentTier = config.StartingTier;
            owned.CurrentStarInTier = config.StartingStarInTier;
            owned.CurrentShard = 0;

            Save();
            NotifyHeroCollectionChanged(heroId, HeroCollectionChangeType.HeroReset);
            RefreshHeroRuntime(heroId);
        }
        
        public void ResetHeroProgress(int heroId)
        {
            if (service == null || database == null) return;

            var owned = service.GetOrCreateOwnedHero(heroId);
            var config = database.GetProgressionConfig(heroId);

            if (config == null)
            {
                Debug.LogError($"ResetHeroProgress failed: Missing HeroProgressionConfig for heroId = {heroId}");
                return;
            }

            owned.IsUnlocked = true;
            owned.CurrentTier = config.StartingTier;
            owned.CurrentStarInTier = config.StartingStarInTier;
            owned.CurrentShard = 0;

            Save();
            NotifyHeroCollectionChanged(heroId, HeroCollectionChangeType.HeroReset);
            RefreshHeroRuntime(heroId);
        }
        
        public void ClearAllHeroData()
        {
            saveData = new HeroCollectionSaveData();
            service = new HeroProgressionService(database, saveData);

            if (ES3.KeyExists(saveKey))
                ES3.DeleteKey(saveKey);

            NotifyHeroCollectionChanged(-1, HeroCollectionChangeType.AllReset);

            var allLists = new List<List<HeroProgressionRuntimeBridge>>(heroBridges.Values);
            for (int i = 0; i < allLists.Count; i++)
            {
                var list = allLists[i];
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j] != null)
                        list[j].RefreshFromProgression();
                }
            }
        }
        
        private void NotifyHeroCollectionChanged(int heroId, HeroCollectionChangeType changeType)
        {
            OnHeroCollectionChanged?.Invoke(new HeroCollectionChangedArgs
            {
                HeroId = heroId,
                ChangeType = changeType
            });
        }
        
        public void RegisterBridge(int heroId, HeroProgressionRuntimeBridge bridge)
        {
            if (bridge == null) return;

            if (!heroBridges.TryGetValue(heroId, out var list))
            {
                list = new List<HeroProgressionRuntimeBridge>();
                heroBridges[heroId] = list;
            }

            if (!list.Contains(bridge))
                list.Add(bridge);
        }

        public void UnregisterBridge(int heroId, HeroProgressionRuntimeBridge bridge)
        {
            if (bridge == null) return;
            if (!heroBridges.TryGetValue(heroId, out var list)) return;

            list.Remove(bridge);

            if (list.Count == 0)
                heroBridges.Remove(heroId);
        }
        
        private void RefreshHeroRuntime(int heroId)
        {
            if (!heroBridges.TryGetValue(heroId, out var list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var bridge = list[i];
                if (bridge == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                bridge.RefreshFromProgression();
            }

            if (list.Count == 0)
                heroBridges.Remove(heroId);
        }
        
        public void AcquireHeroIfNeeded(HeroDataSO hero)
        {
            if (hero == null || service == null) return;

            bool unlocked = service.UnlockHero(hero);
            if (!unlocked) return;

            Save();
            NotifyHeroCollectionChanged(hero.Id, HeroCollectionChangeType.HeroUnlocked);
            RefreshHeroRuntime(hero.Id);
        }

        public void AddShardToHero(HeroDataSO hero, int amount, bool acquireIfMissing = true)
        {
            if (hero == null || amount <= 0 || service == null) return;

            bool justUnlocked = false;

            if (acquireIfMissing && !service.HasHero(hero.Id))
            {
                justUnlocked = service.UnlockHero(hero);
            }

            service.AddShard(hero.Id, amount);
            Save();

            if (justUnlocked)
                NotifyHeroCollectionChanged(hero.Id, HeroCollectionChangeType.HeroUnlocked);

            NotifyHeroCollectionChanged(hero.Id, HeroCollectionChangeType.ShardAdded);
            RefreshHeroRuntime(hero.Id);
        }
        
    }
    
    public enum HeroCollectionChangeType
    {
        HeroUnlocked,
        ShardAdded,
        HeroUpgraded,
        HeroReset,
        AllReset
    }

    public class HeroCollectionChangedArgs
    {
        public int HeroId;
        public HeroCollectionChangeType ChangeType;
    }
}