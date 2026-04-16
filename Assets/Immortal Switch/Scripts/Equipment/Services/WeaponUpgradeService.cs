using UnityEngine;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;

namespace Immortal_Switch.Scripts.Equipment.Services
{
    public class WeaponUpgradeService
    {
        private readonly WeaponDatabaseSO database;
        private readonly WeaponInventoryService inventory;

        public WeaponUpgradeService(WeaponDatabaseSO database, WeaponInventoryService inventory)
        {
            this.database = database;
            this.inventory = inventory;
        }

        public int GetCurrentMaxLevelForStandard(int weaponId)
        {
            var def = database.GetStandard(weaponId);
            var state = inventory.GetOrCreateStandardState(weaponId);

            if (def == null || def.LimitBreakConfig == null)
                return 10;

            return def.LimitBreakConfig.GetMaxLevel(state.LimitBreakStage);
        }

        public int GetCurrentMaxLevelForExclusive(int exclusiveWeaponId, int heroId)
        {
            var def = database.GetExclusive(exclusiveWeaponId);
            var state = inventory.GetOrCreateExclusiveState(exclusiveWeaponId, heroId);

            if (def == null || def.LimitBreakConfig == null)
                return 10;

            return def.LimitBreakConfig.GetMaxLevel(state.LimitBreakStage);
        }

        public bool TryLevelUpStandard(int weaponId)
        {
            var def = database.GetStandard(weaponId);
            if (def == null) return false;

            var state = inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked) return false;

            int maxLevel = GetCurrentMaxLevelForStandard(weaponId);
            if (state.Level >= maxLevel) return false;

            int nextLevel = state.Level + 1;
            int cost = def.LevelConfig != null ? def.LevelConfig.GetCost(nextLevel) : 0;
            if (cost <= 0) return false;

            if (!CurrencyManager.Instance.Spend(CurrencyType.WeaponEnhancementStone, cost))
                return false;

            state.Level = nextLevel;
            return true;
        }

        public bool TryLevelUpExclusive(int exclusiveWeaponId, int heroId)
        {
            var def = database.GetExclusive(exclusiveWeaponId);
            if (def == null) return false;

            var state = inventory.GetOrCreateExclusiveState(exclusiveWeaponId, heroId);
            if (!state.IsUnlocked) return false;

            int maxLevel = GetCurrentMaxLevelForExclusive(exclusiveWeaponId, heroId);
            if (state.Level >= maxLevel) return false;

            int nextLevel = state.Level + 1;
            int cost = def.LevelConfig != null ? def.LevelConfig.GetCost(nextLevel) : 0;
            if (cost <= 0) return false;

            if (!CurrencyManager.Instance.Spend(CurrencyType.WeaponEnhancementStone, cost))
                return false;

            state.Level = nextLevel;
            return true;
        }

        public WeaponLimitBreakResult TryLimitBreakStandard(int weaponId)
        {
            var def = database.GetStandard(weaponId);
            if (def == null || def.LimitBreakConfig == null)
                return WeaponLimitBreakResult.Invalid;

            var state = inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked)
                return WeaponLimitBreakResult.Invalid;

            int nextStage = state.LimitBreakStage + 1;
            var entry = def.LimitBreakConfig.GetEntryByStage(nextStage);
            if (entry == null)
                return WeaponLimitBreakResult.Maxed;

            if (state.Level < entry.RequiredLevel)
                return WeaponLimitBreakResult.RequiredLevelNotReached;

            if (!CurrencyManager.Instance.Spend(CurrencyType.WeaponBreakThroughStone, entry.BreakThroughStoneCost))
                return WeaponLimitBreakResult.NotEnoughCurrency;

            if (Random.value <= entry.SuccessRate)
            {
                state.LimitBreakStage = nextStage;
                return WeaponLimitBreakResult.Success;
            }

            return WeaponLimitBreakResult.Failed;
        }

        public WeaponLimitBreakResult TryLimitBreakExclusive(int exclusiveWeaponId, int heroId)
        {
            var def = database.GetExclusive(exclusiveWeaponId);
            if (def == null || def.LimitBreakConfig == null)
                return WeaponLimitBreakResult.Invalid;

            var state = inventory.GetOrCreateExclusiveState(exclusiveWeaponId, heroId);
            if (!state.IsUnlocked)
                return WeaponLimitBreakResult.Invalid;

            int nextStage = state.LimitBreakStage + 1;
            var entry = def.LimitBreakConfig.GetEntryByStage(nextStage);
            if (entry == null)
                return WeaponLimitBreakResult.Maxed;

            if (state.Level < entry.RequiredLevel)
                return WeaponLimitBreakResult.RequiredLevelNotReached;

            if (!CurrencyManager.Instance.Spend(CurrencyType.WeaponBreakThroughStone, entry.BreakThroughStoneCost))
                return WeaponLimitBreakResult.NotEnoughCurrency;

            if (Random.value <= entry.SuccessRate)
            {
                state.LimitBreakStage = nextStage;
                return WeaponLimitBreakResult.Success;
            }

            return WeaponLimitBreakResult.Failed;
        }
    }
}