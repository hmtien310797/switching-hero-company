using System.Collections.Generic;
using System.Linq;
using Battle;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Hero;

namespace Immortal_Switch.Scripts.Equipment.Services
{
    public class WeaponInventoryService
    {
        private readonly WeaponDatabaseSO database;
        private readonly WeaponSaveData saveData;

        public WeaponInventoryService(WeaponDatabaseSO database, WeaponSaveData saveData)
        {
            this.database = database;
            this.saveData = saveData;
        }

        public StandardWeaponState GetOrCreateStandardState(int weaponId)
        {
            var state = saveData.StandardWeapons.Find(x => x.WeaponId == weaponId);
            if (state != null)
                return state;

            state = new StandardWeaponState
            {
                WeaponId = weaponId,
                IsUnlocked = false,
                Level = 1,
                LimitBreakStage = 0,
                CurrentShard = 0
            };

            saveData.StandardWeapons.Add(state);
            return state;
        }

        public ExclusiveWeaponState GetOrCreateExclusiveState(int exclusiveWeaponId, int heroId)
        {
            var state = saveData.ExclusiveWeapons.Find(x => x.ExclusiveWeaponId == exclusiveWeaponId);
            if (state != null)
                return state;

            state = new ExclusiveWeaponState
            {
                ExclusiveWeaponId = exclusiveWeaponId,
                HeroId = heroId,
                IsUnlocked = false,
                Level = 1,
                LimitBreakStage = 0,
                CurrentShard = 0
            };

            saveData.ExclusiveWeapons.Add(state);
            return state;
        }

        public HeroWeaponEquipEntry GetOrCreateHeroEquip(int heroId)
        {
            var entry = saveData.HeroEquips.Find(x => x.HeroId == heroId);
            if (entry != null)
                return entry;

            entry = new HeroWeaponEquipEntry
            {
                HeroId = heroId,
                EquippedStandardWeaponId = 0,
                EquippedExclusiveWeaponId = 0,
                UseExclusive = true
            };

            saveData.HeroEquips.Add(entry);
            return entry;
        }

        public bool IsStandardUnlocked(int weaponId)
        {
            return GetOrCreateStandardState(weaponId).IsUnlocked;
        }

        public bool IsExclusiveUnlocked(int exclusiveWeaponId, int heroId)
        {
            return GetOrCreateExclusiveState(exclusiveWeaponId, heroId).IsUnlocked;
        }

        public List<StandardWeaponDefinitionSO> GetUnlockedStandardsByClass(HeroClass heroClass)
        {
            return database.GetStandardsByClass(heroClass)
                .Where(x => GetOrCreateStandardState(x.WeaponId).IsUnlocked)
                .ToList();
        }

        public WeaponEquipSource ResolveActiveSource(int heroId)
        {
            var equip = GetOrCreateHeroEquip(heroId);
            if (equip.UseExclusive && equip.EquippedExclusiveWeaponId > 0)
            {
                var exDef = database.GetExclusive(equip.EquippedExclusiveWeaponId);
                if (exDef != null)
                {
                    var exState = GetOrCreateExclusiveState(exDef.ExclusiveWeaponId, exDef.HeroId);
                    if (exState.IsUnlocked)
                        return WeaponEquipSource.Exclusive;
                }
            }

            if (equip.EquippedStandardWeaponId > 0)
            {
                var stdState = GetOrCreateStandardState(equip.EquippedStandardWeaponId);
                if (stdState.IsUnlocked)
                    return WeaponEquipSource.Standard;
            }

            return WeaponEquipSource.None;
        }
    }
}