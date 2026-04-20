using System;
using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Equipment.Runtime;
using Immortal_Switch.Scripts.Equipment.Services;
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
        
        public bool TryAutoEquipForHeroes(IEnumerable<PlayerHeroController> heroes, bool autoSave = true)
        {
            if (heroes == null)
                return false;

            var heroList = new List<PlayerHeroController>(heroes);
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