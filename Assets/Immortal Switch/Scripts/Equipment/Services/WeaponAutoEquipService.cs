using System.Collections.Generic;
using System.Linq;
using Battle;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Hero;

namespace Immortal_Switch.Scripts.Equipment.Services
{
    public class WeaponAutoEquipService
    {
        private readonly WeaponDatabaseSO database;
        private readonly WeaponInventoryService inventory;
        private readonly WeaponEquipService equipService;

        public WeaponAutoEquipService(
            WeaponDatabaseSO database,
            WeaponInventoryService inventory,
            WeaponEquipService equipService)
        {
            this.database = database;
            this.inventory = inventory;
            this.equipService = equipService;
        }

        public bool AutoEquip(int heroId, HeroClass heroClass)
        {
            if (heroId <= 0)
                return false;

            var exclusive = database.GetExclusiveByHeroId(heroId);
            if (exclusive != null)
            {
                var exState = inventory.GetOrCreateExclusiveState(exclusive.ExclusiveWeaponId, heroId);
                if (exState.IsUnlocked)
                {
                    return equipService.EquipExclusive(heroId);
                }
            }

            var unlockedStandards = inventory.GetUnlockedStandardsByClass(heroClass);
            if (unlockedStandards == null || unlockedStandards.Count == 0)
                return false;

            var best = GetBestStandard(unlockedStandards);
            if (best == null)
                return false;

            return equipService.EquipStandard(heroId, heroClass, best.WeaponId);
        }

        private StandardWeaponDefinitionSO GetBestStandard(List<StandardWeaponDefinitionSO> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            return candidates
                .Where(x => x != null)
                .OrderByDescending(GetTierRank)
                .ThenByDescending(x => x.Star)
                .ThenByDescending(GetLevel)
                .ThenByDescending(GetLimitBreakStage)
                .ThenByDescending(x => x.WeaponId)
                .FirstOrDefault();
        }

        private int GetTierRank(StandardWeaponDefinitionSO def)
        {
            return def != null ? (int)def.Tier : -1;
        }

        private int GetLevel(StandardWeaponDefinitionSO def)
        {
            if (def == null)
                return -1;

            var state = inventory.GetOrCreateStandardState(def.WeaponId);
            return state.Level;
        }

        private int GetLimitBreakStage(StandardWeaponDefinitionSO def)
        {
            if (def == null)
                return -1;

            var state = inventory.GetOrCreateStandardState(def.WeaponId);
            return state.LimitBreakStage;
        }
    }
}