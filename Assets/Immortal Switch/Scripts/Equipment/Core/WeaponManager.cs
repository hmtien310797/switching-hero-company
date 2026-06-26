using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Equipment.Runtime;
using Immortal_Switch.Scripts.Equipment.Services;
using Immortal_Switch.Scripts.Equipment.UI;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Core
{
    public class WeaponManager : MonoBehaviour
    {
        public static WeaponManager Instance { get; private set; }

        [SerializeField] private WeaponDatabaseSO database;
        private const string SaveKey = "weapon_save_data";

        private WeaponSaveData saveData;

        private WeaponInventoryService inventory;
        private WeaponEquipService equip;
        private WeaponAutoEquipService autoEquip;
        private WeaponUpgradeService upgrade;
        private WeaponFuseService fuse;

        public WeaponDatabaseSO Database => database;
        public WeaponSaveData SaveData => saveData;
        public WeaponInventoryService Inventory => inventory;
        public WeaponEquipService Equip => equip;
        public WeaponAutoEquipService AutoEquipService => autoEquip;
        public WeaponUpgradeService Upgrade => upgrade;
        public WeaponFuseService Fuse => fuse;

        public event Action<int> OnHeroWeaponChanged;
        public event Action<int> OnStandardWeaponStateChanged;
        public event Action<int> OnExclusiveWeaponStateChanged;
        
        private UserDataCache userDataCache;

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
            userDataCache = UserDataCache.Instance;
            BuildServices();
        }

        private void BuildServices()
        {
            inventory = new WeaponInventoryService(database, saveData);
            equip = new WeaponEquipService(database, inventory);
            autoEquip = new WeaponAutoEquipService(database, inventory, equip);
            upgrade = new WeaponUpgradeService(database, inventory);
            fuse = new WeaponFuseService(database, inventory);
        }

        public void Save()
        {
            ES3.Save(SaveKey, saveData);
        }

        public void Load()
        {
            saveData = ES3.KeyExists(SaveKey)
                ? ES3.Load<WeaponSaveData>(SaveKey)
                : new WeaponSaveData();
        }

        public void UnlockStandard(int weaponId, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked)
            {
                state.IsUnlocked = true;
                state.Level = Mathf.Max(1, state.Level);
            }

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);
        }

        public void AddStandardShard(int weaponId, int amount, bool autoSave = true)
        {
            if (amount <= 0)
                return;

            var state = inventory.GetOrCreateStandardState(weaponId);
            state.CurrentShard += amount;

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);
        }

        public void UnlockExclusive(int heroId, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            if (!state.IsUnlocked)
            {
                state.IsUnlocked = true;
                state.Level = Mathf.Max(1, state.Level);
            }

            if (autoSave)
                Save();

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }
        
        public bool AddStandardShardFromSummon(
            int weaponId,
            int amount,
            out bool isNewWeapon,
            out int totalShardAfter,
            bool autoSave = true)
        {
            isNewWeapon = false;
            totalShardAfter = 0;

            if (weaponId <= 0 || amount <= 0)
                return false;

            var state = inventory.GetOrCreateStandardState(weaponId);
            if (state == null)
                return false;

            isNewWeapon = !state.IsUnlocked;

            // Rule weapon summon giống skill:
            // roll ra shard là unlock luôn, shard vẫn giữ nguyên.
            state.IsUnlocked = true;
            state.Level = Mathf.Max(1, state.Level);
            state.CurrentShard = Mathf.Max(0, state.CurrentShard + amount);

            totalShardAfter = state.CurrentShard;

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);
            return true;
        }

        public void AddExclusiveShard(int heroId, int amount, bool autoSave = true)
        {
            if (amount <= 0)
                return;

            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.CurrentShard += amount;

            if (autoSave)
                Save();

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        public bool EquipStandard(int heroId, HeroClass heroClass, int weaponId, bool autoSave = true)
        {
            bool result = equip.EquipStandard(heroId, heroClass, weaponId);
            if (!result)
                return false;

            if (autoSave)
                Save();

            NotifyHeroWeaponChanged(heroId);
            return true;
        }

        public bool EquipExclusive(int heroId, bool autoSave = true)
        {
            bool result = equip.EquipExclusive(heroId);
            if (!result)
                return false;

            if (autoSave)
                Save();

            NotifyHeroWeaponChanged(heroId);
            return true;
        }

        public bool TryAutoEquip(int heroId, HeroClass heroClass, bool autoSave = true)
        {
            bool result = autoEquip.AutoEquip(heroId, heroClass);
            if (!result)
                return false;

            if (autoSave)
                Save();

            NotifyHeroWeaponChanged(heroId);
            return true;
        }
        
        public bool TryAutoEquipForHeroes(IEnumerable<HeroActor> heroes, bool autoSave = true)
        {
            if (heroes == null)
                return false;

            var heroList = new List<HeroActor>(heroes);
            bool result = autoEquip.AutoEquipForHeroes(heroList);
            if (!result)
                return false;

            if (autoSave)
                Save();

            for (int i = 0; i < heroList.Count; i++)
            {
                var hero = heroList[i];
                if (hero == null || !hero.gameObject.activeInHierarchy)
                    continue;

                NotifyHeroWeaponChanged(hero.GetHeroId());
            }

            return true;
        }
        
        /// <summary>
        /// Fuse toàn bộ vũ khí standard đang sở hữu — mỗi vũ khí đi tiếp theo chuỗi (qua RPC weapon/fuse,
        /// xem TryFuseStandardAsync) cho tới khi không đủ shard hoặc gặp max node.
        /// </summary>
        public async UniTask<WeaponFuseAllResult> TryFuseAllStandardWeaponsAsync()
        {
            var result = new WeaponFuseAllResult();

            if (database == null || inventory == null || database.StandardWeapons == null)
                return result;

            var ownedWeaponIds = new List<int>();
            foreach (var def in database.StandardWeapons)
            {
                if (def == null) continue;

                var state = inventory.GetOrCreateStandardState(def.WeaponId);
                if (state != null && state.IsUnlocked)
                    ownedWeaponIds.Add(def.WeaponId);
            }

            bool changedAny = false;

            foreach (var startWeaponId in ownedWeaponIds)
            {
                int currentWeaponId = startWeaponId;

                while (true)
                {
                    var fuseResult = await TryFuseStandardAsync(currentWeaponId, false);
                    if (!fuseResult.Success)
                        break;

                    changedAny = true;

                    if (fuseResult.TargetStandardWeaponId > 0)
                    {
                        AddStandardReward(result, fuseResult.TargetStandardWeaponId, 1);
                        currentWeaponId = fuseResult.TargetStandardWeaponId;
                    }
                    else if (fuseResult.TargetExclusiveWeaponId > 0)
                    {
                        AddExclusiveReward(result, fuseResult.TargetExclusiveWeaponId, 1);
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (changedAny)
            {
                Save();
                NotifyAllRelevantWeaponChangesAfterFuseAll(result);
            }

            return result;
        }

        private void AddStandardReward(WeaponFuseAllResult result, int weaponId, int amount)
        {
            if (result == null || weaponId <= 0 || amount <= 0)
                return;

            var def = database.GetStandard(weaponId);
            if (def == null)
                return;

            var existing = result.Rewards.Find(x => !x.IsExclusive && x.WeaponId == weaponId);
            if (existing != null)
            {
                existing.Amount += amount;
                return;
            }

            result.Rewards.Add(new WeaponFuseAllRewardEntry
            {
                WeaponId = def.WeaponId,
                WeaponName = def.WeaponName,
                HeroClass = def.WeaponClass,
                Tier = def.Tier,
                Star = def.Star,
                IsExclusive = false,
                Amount = amount,
                Icon = def.Icon
            });
        }
        
        private void AddExclusiveReward(WeaponFuseAllResult result, int exclusiveWeaponId, int amount)
        {
            if (result == null || exclusiveWeaponId <= 0 || amount <= 0)
                return;

            var def = database.GetExclusive(exclusiveWeaponId);
            if (def == null)
                return;

            var existing = result.Rewards.Find(x => x.IsExclusive && x.WeaponId == exclusiveWeaponId);
            if (existing != null)
            {
                existing.Amount += amount;
                return;
            }

            result.Rewards.Add(new WeaponFuseAllRewardEntry
            {
                WeaponId = def.ExclusiveWeaponId,
                WeaponName = def.WeaponName,
                HeroClass = def.HeroClass,
                Tier = WeaponTier.SS,
                Star = def.StartingStar,
                IsExclusive = true,
                Amount = amount,
                Icon = def.Icon
            });
        }
        
        private void NotifyAllRelevantWeaponChangesAfterFuseAll(WeaponFuseAllResult result)
        {
            if (result == null || result.Rewards == null)
                return;

            // refresh runtime heroes
            if (Battle.PvEBattleController.Instance != null)
            {
                var activeHeroes = userDataCache.inBattleHeroes;
                for (int i = 0; i < activeHeroes.Length; i++)
                {
                    var hero = activeHeroes[i];
                    if (hero == null)
                        continue;

                    NotifyHeroWeaponChanged(hero.GetHeroId());
                }
            }

            // refresh changed weapon states for UI/data listeners
            for (int i = 0; i < result.Rewards.Count; i++)
            {
                var reward = result.Rewards[i];
                if (reward == null)
                    continue;

                if (reward.IsExclusive)
                {
                    var exDef = database.GetExclusive(reward.WeaponId);
                    if (exDef != null)
                        NotifyExclusiveWeaponChanged(exDef.ExclusiveWeaponId, exDef.HeroId);
                }
                else
                {
                    NotifyStandardWeaponChanged(reward.WeaponId);
                }
            }
        }
        
        /// <summary>
        /// Fuse liên tiếp tối đa <paramref name="count"/> lần qua RPC weapon/fuse. Mỗi lần thành công
        /// server đổi identity sang weapon_id kế tiếp, nên lần fuse sau sẽ tiếp tục từ id mới đó.
        /// </summary>
        public async UniTask<WeaponFuseAllResult> TryFusionForSelectedWeaponAsync(int weaponId, int count)
        {
            var uiResult = new WeaponFuseAllResult();

            if (weaponId <= 0 || count <= 0)
                return uiResult;

            int currentWeaponId = weaponId;
            for (int i = 0; i < count; i++)
            {
                var result = await TryFuseStandardAsync(currentWeaponId, false);
                if (!result.Success)
                    break;

                if (result.TargetStandardWeaponId > 0)
                {
                    AddStandardReward(uiResult, result.TargetStandardWeaponId, 1);
                    currentWeaponId = result.TargetStandardWeaponId;
                }
                else if (result.TargetExclusiveWeaponId > 0)
                {
                    AddExclusiveReward(uiResult, result.TargetExclusiveWeaponId, 1);
                    break;
                }
                else
                {
                    break;
                }
            }

            if (uiResult.HasAnyReward)
            {
                Save();
                NotifyAllRelevantWeaponChangesAfterFuseAll(uiResult);
            }

            return uiResult;
        }

        public bool TryLevelUpStandard(int weaponId, bool autoSave = true)
        {
            bool result = upgrade.TryLevelUpStandard(weaponId);
            if (!result)
                return false;

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);
            return true;
        }

        public bool TryLevelUpExclusive(int heroId, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return false;

            bool result = upgrade.TryLevelUpExclusive(def.ExclusiveWeaponId, heroId);
            if (!result)
                return false;

            if (autoSave)
                Save();

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
            return true;
        }

        public WeaponLimitBreakResult TryLimitBreakStandard(int weaponId, bool autoSave = true)
        {
            var result = upgrade.TryLimitBreakStandard(weaponId);

            if (result == WeaponLimitBreakResult.Success || result == WeaponLimitBreakResult.Failed)
            {
                if (autoSave)
                    Save();

                NotifyStandardWeaponChanged(weaponId);
            }

            return result;
        }

        public WeaponLimitBreakResult TryLimitBreakExclusive(int heroId, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return WeaponLimitBreakResult.Invalid;

            var result = upgrade.TryLimitBreakExclusive(def.ExclusiveWeaponId, heroId);

            if (result == WeaponLimitBreakResult.Success || result == WeaponLimitBreakResult.Failed)
            {
                if (autoSave)
                    Save();

                NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
            }

            return result;
        }

        /// <summary>Fuse local-only (không gọi server) — dùng cho Editor debug và nhánh ToRandomExclusive (spec server chưa cover).</summary>
        public WeaponFuseResult TryFuseStandard(int weaponId, bool autoSave = true)
        {
            var result = fuse.TryFuseStandard(weaponId);
            if (!result.Success)
                return result;

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);

            if (result.TargetStandardWeaponId > 0)
                NotifyStandardWeaponChanged(result.TargetStandardWeaponId);

            if (result.TargetExclusiveWeaponId > 0)
            {
                NotifyExclusiveWeaponChanged(result.TargetExclusiveWeaponId, result.TargetHeroId);

                // auto equip exclusive ngay khi có
                EquipExclusive(result.TargetHeroId, autoSave);
            }

            return result;
        }

        /// <summary>
        /// Fuse 1 lần qua RPC weapon/fuse (nhánh ToNextStandard) — server là nguồn sự thật, trừ shard
        /// + đổi weapon_id/grade/star, client chỉ apply lại kết quả (xem Docs/be-weapon-fuse-rpc-spec.md).
        /// Nhánh ToRandomExclusive vẫn rơi về <see cref="TryFuseStandard"/> local vì spec server chưa cover
        /// (server trả MAX_NODE_REACHED cho nhánh này).
        /// </summary>
        public async UniTask<WeaponFuseResult> TryFuseStandardAsync(int weaponId, bool autoSave = true)
        {
            var def = database.GetStandard(weaponId);
            if (def == null)
                return new WeaponFuseResult { SourceWeaponId = weaponId, Success = false };

            if (def.FuseMode == WeaponFuseMode.ToRandomExclusive)
                return TryFuseStandard(weaponId, autoSave);

            var result = new WeaponFuseResult { SourceWeaponId = weaponId, Success = false };

            WeaponFuseResponse response;
            try
            {
                response = await NakamaClient.Instance.FuseWeaponAsync(weaponId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WeaponManager] weapon/fuse RPC failed for weaponId={weaponId}: {e.Message}");
                return result;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                    Debug.LogWarning($"[WeaponManager] weapon/fuse rejected for weaponId={weaponId}: {response.Error}");
                return result;
            }

            ApplyFuseResult(response);

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(response.OldWeaponId);
            NotifyStandardWeaponChanged(response.NewWeaponId);

            result.Success = true;
            result.TargetStandardWeaponId = response.NewWeaponId;
            return result;
        }

        private void ApplyFuseResult(WeaponFuseResponse response)
        {
            var oldState = inventory.GetOrCreateStandardState(response.OldWeaponId);
            oldState.CurrentShard = Mathf.Max(0, response.ShardBalance);

            var newState = inventory.GetOrCreateStandardState(response.NewWeaponId);
            if (!newState.IsUnlocked)
            {
                newState.IsUnlocked = true;
                newState.Level = Mathf.Max(1, oldState.Level);
                newState.LimitBreakStage = 0;
            }
        }

        public void NotifyHeroWeaponChanged(int heroId)
        {
            OnHeroWeaponChanged?.Invoke(heroId);
            HeroEquipmentRuntimeRegistry.RefreshHero(heroId);
        }

        public void NotifyStandardWeaponChanged(int weaponId)
        {
            OnStandardWeaponStateChanged?.Invoke(weaponId);
            HeroEquipmentRuntimeRegistry.RefreshHeroesUsingStandard(weaponId);
        }

        public void NotifyExclusiveWeaponChanged(int exclusiveWeaponId, int heroId)
        {
            OnExclusiveWeaponStateChanged?.Invoke(exclusiveWeaponId);
            NotifyHeroWeaponChanged(heroId);
        }

        public void DebugSetStandardLevel(int weaponId, int level, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            state.Level = Mathf.Max(1, level);

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);
        }

        public void DebugSetStandardLimitBreakStage(int weaponId, int stage, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            state.LimitBreakStage = Mathf.Max(0, stage);

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);
        }

        public void DebugSetStandardShard(int weaponId, int shard, bool autoSave = true)
        {
            var state = inventory.GetOrCreateStandardState(weaponId);
            state.CurrentShard = Mathf.Max(0, shard);

            if (autoSave)
                Save();

            NotifyStandardWeaponChanged(weaponId);
        }

        public void DebugSetExclusiveLevel(int heroId, int level, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.Level = Mathf.Max(1, level);

            if (autoSave)
                Save();

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        public void DebugSetExclusiveLimitBreakStage(int heroId, int stage, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.LimitBreakStage = Mathf.Max(0, stage);

            if (autoSave)
                Save();

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        public void DebugSetExclusiveShard(int heroId, int shard, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.CurrentShard = Mathf.Max(0, shard);

            if (autoSave)
                Save();

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }

        public void DebugSetExclusiveStar(int heroId, int star, bool autoSave = true)
        {
            var def = database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return;

            var state = inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            state.CurrentStar = Mathf.Max(1, star);

            if (autoSave)
                Save();

            NotifyExclusiveWeaponChanged(def.ExclusiveWeaponId, heroId);
        }
    }
}