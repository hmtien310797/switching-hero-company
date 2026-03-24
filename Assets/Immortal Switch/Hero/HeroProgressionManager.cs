using System;
using UnityEngine;

namespace Immortal_Switch.Hero
{
    public class HeroProgressionManager : MonoBehaviour
    {
        public static HeroProgressionManager Instance { get; private set; }

        [SerializeField] private HeroProgressionDatabaseSO database;
        private const string saveKey = "hero_progression_save";

        private HeroCollectionSaveData saveData;
        private HeroProgressionService service;

        public HeroProgressionService Service => service;
        public HeroProgressionDatabaseSO Database => database;
        public event Action<HeroCollectionChangedArgs> OnHeroCollectionChanged;

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
            service = new HeroProgressionService(database, saveData);
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
            bool result = service.UpgradeHero(heroId);
            if (result)
            {
                Save();
                NotifyHeroCollectionChanged(heroId, HeroCollectionChangeType.HeroUpgraded);
            }

            return result;
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
        }
        
        public void ClearAllHeroData()
        {
            saveData = new HeroCollectionSaveData();
            service = new HeroProgressionService(database, saveData);

            if (ES3.KeyExists(saveKey))
                ES3.DeleteKey(saveKey);

            Debug.Log("All hero progression data cleared.");

            NotifyHeroCollectionChanged(-1, HeroCollectionChangeType.AllReset);
        }
        
        private void NotifyHeroCollectionChanged(int heroId, HeroCollectionChangeType changeType)
        {
            OnHeroCollectionChanged?.Invoke(new HeroCollectionChangedArgs
            {
                HeroId = heroId,
                ChangeType = changeType
            });
        }
        
        public void AcquireHeroIfNeeded(HeroDataSO hero)
        {
            if (hero == null) return;

            bool unlocked = service.UnlockHero(hero);
            if (unlocked)
            {
                Save();
                NotifyHeroCollectionChanged(hero.Id, HeroCollectionChangeType.HeroUnlocked);
            }
        }

        public void AddShardToHero(HeroDataSO hero, int amount, bool acquireIfMissing = true)
        {
            if (hero == null || amount <= 0) return;

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