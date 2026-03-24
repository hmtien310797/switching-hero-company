using UnityEngine;

namespace Immortal_Switch.Hero
{
    public class HeroProgressionManager : MonoBehaviour
    {
        public static HeroProgressionManager Instance { get; private set; }

        [SerializeField] private HeroProgressionDatabaseSO database;
        [SerializeField] private string saveKey = "hero_progression_save";

        private HeroCollectionSaveData saveData;
        private HeroProgressionService service;

        public HeroProgressionService Service => service;
        public HeroProgressionDatabaseSO Database => database;

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
            if (result) Save();
            return result;
        }
    }
}