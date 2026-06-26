using System;
using System.Collections.Generic;
using Common;
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
            //ES3.Save(saveKey, saveData);
        }

        public void Load()
        {
            saveData = new HeroCollectionSaveData();
            // if (ES3.KeyExists(saveKey))
            //     saveData = ES3.Load<HeroCollectionSaveData>(saveKey);
            // else
            //     saveData = new HeroCollectionSaveData();
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

        /// <summary>Sync shard count tuyệt đối từ server (hero/list, player/me).</summary>
        public void SetShard(int heroId, int amount)
        {
            if (service == null) return;

            service.SetShard(heroId, amount);
            Save();
            NotifyHeroCollectionChanged(heroId, HeroCollectionChangeType.ShardAdded);
            RefreshHeroRuntime(heroId);
        }

        /// <summary>Sync tier + star tuyệt đối từ server (hero/list, player/me — HeroInstance.Rarity/Star).</summary>
        public void SetProgress(int heroId, HeroProgressTier tier, int starInTier)
        {
            if (service == null) return;

            service.SetProgress(heroId, tier, starInTier);
            Save();
            NotifyHeroCollectionChanged(heroId, HeroCollectionChangeType.HeroUpgraded);
            RefreshHeroRuntime(heroId);
        }

        /// <summary>
        /// Sync toàn bộ collection từ server (hero/list, player/me) — nguồn sự thật.
        /// Acquire/update các hero đang sở hữu, đồng thời reset các hero không còn trong danh sách server
        /// (tránh leak dữ liệu hero cũ từ tài khoản/thiết bị khác còn sót trong save local).
        /// </summary>
        public void SyncFromServer(HeroInstance[] ownedHeroes, Dictionary<string, int> shards)
        {
            if (service == null) return;

            var ownedHeroIds = new List<int>();

            if (ownedHeroes != null)
            {
                foreach (var heroInstance in ownedHeroes)
                {
                    ownedHeroIds.Add(heroInstance.HeroId);

                    var heroData = MasterDataCache.Instance.GetHeroDataById(heroInstance.HeroId);
                    if (heroData == null) continue;

                    AcquireHeroIfNeeded(heroData);

                    if (Enum.TryParse<HeroProgressTier>(heroInstance.Rarity, true, out var tier))
                        SetProgress(heroInstance.HeroId, tier, heroInstance.Star);
                    else
                        Debug.LogWarning($"[HeroProgressionManager] Unknown hero rarity '{heroInstance.Rarity}' for hero_id={heroInstance.HeroId}");
                }
            }

            ReconcileOwnedHeroes(ownedHeroIds);

            if (shards != null)
            {
                foreach (var kv in shards)
                {
                    if (int.TryParse(kv.Key, out int heroId))
                        SetShard(heroId, kv.Value);
                }
            }
        }

        /// <summary>Reset (lock lại) các hero đang unlocked ở local nhưng không còn trong danh sách owned của server.</summary>
        private void ReconcileOwnedHeroes(List<int> serverOwnedHeroIds)
        {
            var ownedSet = new HashSet<int>(serverOwnedHeroIds);

            var staleHeroIds = new List<int>();
            foreach (var owned in saveData.OwnedHeroes)
            {
                if (owned.IsUnlocked && !ownedSet.Contains(owned.HeroId))
                    staleHeroIds.Add(owned.HeroId);
            }

            foreach (var heroId in staleHeroIds)
                ResetHero(heroId);
        }

        /// <summary>Nâng sao 1 hero — fire-and-forget wrapper cho UI (Button.onClick). Dùng <see cref="UpgradeHeroAsync"/> nếu cần biết kết quả.</summary>
        public void UpgradeHero(int heroId)
        {
            UpgradeHeroAsync(heroId).Forget();
        }

        /// <summary>
        /// Nâng sao 1 hero qua RPC hero/upgrade. Server là nguồn sự thật — trừ shard + set rarity/star,
        /// client chỉ apply lại kết quả trả về (xem Docs/be-hero-upgrade-rpc-spec.md).
        /// </summary>
        public async UniTask<bool> UpgradeHeroAsync(int heroId)
        {
            if (service == null) return false;

            HeroUpgradeResponse response;
            try
            {
                response = await NakamaClient.Instance.UpgradeHeroAsync(heroId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[HeroProgressionManager] hero/upgrade RPC failed for heroId={heroId}: {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[HeroProgressionManager] hero/upgrade rejected for heroId={heroId}: {response.Error}");
                return false;
            }

            ApplyUpgradeResult(response);
            return true;
        }

        private void ApplyUpgradeResult(HeroUpgradeResponse response)
        {
            if (!Enum.TryParse<HeroProgressTier>(response.NewTier, true, out var tier))
            {
                Debug.LogWarning($"[HeroProgressionManager] Unknown tier '{response.NewTier}' for heroId={response.HeroId}");
                return;
            }

            service.SetProgress(response.HeroId, tier, response.NewStar);
            service.SetShard(response.HeroId, response.ShardBalance);

            Save();
            NotifyHeroCollectionChanged(response.HeroId, HeroCollectionChangeType.HeroUpgraded);
            RefreshHeroRuntime(response.HeroId);
        }

        /// <summary>Nâng sao toàn bộ hero đang sở hữu — fire-and-forget wrapper cho UI (Button.onClick).</summary>
        public void UpgradeAllHeroes()
        {
            UpgradeAllHeroesAsync().Forget();
        }

        /// <summary>Gọi hero/upgrade tuần tự cho mỗi hero đang sở hữu (1 lần nâng/hero/lần bấm). Hero chưa sở hữu hoặc chưa đủ shard sẽ bị server từ chối và bỏ qua.</summary>
        public async UniTask UpgradeAllHeroesAsync()
        {
            if (service == null) return;

            List<HeroDataSO> allHeroData = MasterDataCache.Instance.GetAllHeroData();
            for (int i = 0; i < allHeroData.Count; i++)
            {
                int currentHeroId = allHeroData[i].Id;
                if (!service.HasHero(currentHeroId)) continue;

                await UpgradeHeroAsync(currentHeroId);
            }
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