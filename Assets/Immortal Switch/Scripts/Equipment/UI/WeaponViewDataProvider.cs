using System.Collections.Generic;
using Battle;
using Common;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UI
{
    public class WeaponViewDataProvider : MonoBehaviour
    {
        [Header("Scene Hero Runtime")]
        [SerializeField] private List<PlayerHeroController> deployedHeroes = new();

        public void SetDeployedHeroes(List<PlayerHeroController> heroes)
        {
            deployedHeroes = heroes ?? new List<PlayerHeroController>();
        }

        public StandardWeaponTabViewModel BuildStandardTab(HeroClass selectedClass, int selectedWeaponId, int focusedHeroId = 0)
        {
            int resolvedSelectedWeaponId = selectedWeaponId;
            if (resolvedSelectedWeaponId <= 0)
            {
                var standardWeaponDefinitionSos = WeaponManager.Instance.Database.GetStandardsByClass(selectedClass);
                if (standardWeaponDefinitionSos != null && standardWeaponDefinitionSos.Count > 0)
                    resolvedSelectedWeaponId = standardWeaponDefinitionSos[0].WeaponId;
            }
            
            var vm = new StandardWeaponTabViewModel
            {
                SelectedClass = selectedClass
            };

            foreach (HeroClass heroClass in System.Enum.GetValues(typeof(HeroClass)))
            {
                vm.ClassTabs.Add(new WeaponClassTabViewModel
                {
                    HeroClass = heroClass,
                    IsSelected = heroClass == selectedClass,
                    HasDeployedHeroUsingStandard = HasDeployedHeroUsingStandard(heroClass)
                });
            }

            if (WeaponManager.Instance == null || WeaponManager.Instance.Database == null)
                return vm;

            var standards = WeaponManager.Instance.Database.GetStandardsByClass(selectedClass);
            for (int i = 0; i < standards.Count; i++)
            {
                var def = standards[i];
                var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(def.WeaponId);

                vm.Weapons.Add(new StandardWeaponCardViewModel
                {
                    WeaponId = def.WeaponId,
                    WeaponName = def.WeaponName,
                    HeroClass = def.WeaponClass,
                    Tier = def.Tier,
                    Star = def.Star,
                    Icon = def.Icon,
                    IsUnlocked = state.IsUnlocked,
                    IsEquipped = IsAnyHeroEquippingStandard(def.WeaponId),
                    Level = state.Level,
                    LimitBreakStage = state.LimitBreakStage,
                    CurrentShard = state.CurrentShard,
                    MaxShard = def.FuseShardRequired,
                    CanFuse = CanFuseStandard(def.WeaponId),
                    CanLevelUp = CanLevelUpStandard(def.WeaponId),
                    CanLimitBreak = CanLimitBreakStandard(def.WeaponId),
                    IsSelected = def.WeaponId == resolvedSelectedWeaponId,
                });
            }

            int resolvedWeaponId = selectedWeaponId;
            if (resolvedWeaponId <= 0 && vm.Weapons.Count > 0)
                resolvedWeaponId = vm.Weapons[0].WeaponId;

            if (resolvedWeaponId > 0)
                vm.SelectedDetail = BuildStandardDetail(resolvedWeaponId, focusedHeroId);
            
            if (resolvedSelectedWeaponId > 0)
                vm.SelectedDetail = BuildStandardDetail(resolvedSelectedWeaponId, focusedHeroId);

            return vm;
        }

        public ExclusiveWeaponTabViewModel BuildExclusiveTab(int heroId)
        {
            var vm = new ExclusiveWeaponTabViewModel
            {
                HeroId = heroId
            };

            if (WeaponManager.Instance == null || WeaponManager.Instance.Database == null)
                return vm;

            var hero = MasterDataCache.Instance != null ? MasterDataCache.Instance.GetHeroDataById(heroId) : null;
            if (hero != null)
                vm.HeroName = hero.Name;

            var def = WeaponManager.Instance.Database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return vm;

            var state = WeaponManager.Instance.Inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            var equip = WeaponManager.Instance.Inventory.GetOrCreateHeroEquip(heroId);

            vm.ExclusiveCard = new ExclusiveWeaponCardViewModel
            {
                ExclusiveWeaponId = def.ExclusiveWeaponId,
                HeroId = heroId,
                WeaponName = def.WeaponName,
                HeroClass = def.HeroClass,
                Icon = def.Icon,
                IsUnlocked = state.IsUnlocked,
                IsEquipped = equip.EquippedExclusiveWeaponId == def.ExclusiveWeaponId && equip.UseExclusive,
                Level = state.Level,
                LimitBreakStage = state.LimitBreakStage,
                CurrentShard = state.CurrentShard,
                MaxShard = 0, // phase hiện tại chưa có config shard requirement cho transcend
                CurrentStar = state.CurrentStar,
                MaxStar = def.MaxStar,
                CanLevelUp = CanLevelUpExclusive(heroId),
                CanLimitBreak = CanLimitBreakExclusive(heroId),
                CanTranscend = false,
                IsSelected = true
            };

            vm.SelectedDetail = BuildExclusiveDetail(heroId);
            return vm;
        }

        public WeaponDetailViewModel BuildStandardDetail(int weaponId, int focusedHeroId = 0)
        {
            var def = WeaponManager.Instance.Database.GetStandard(weaponId);
            if (def == null)
                return null;

            var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(weaponId);

            var vm = new WeaponDetailViewModel
            {
                ActiveSource = WeaponEquipSource.Standard,
                IsExclusive = false,
                WeaponId = def.WeaponId,
                WeaponName = def.WeaponName,
                Icon = def.Icon,
                HeroClass = def.WeaponClass,
                Tier = def.Tier,
                Star = def.Star,
                MaxStar = def.Star,
                Level = state.Level,
                LimitBreakStage = state.LimitBreakStage,
                CurrentShard = state.CurrentShard,
                CurrentMaxLevel = GetCurrentMaxLevelForStandard(def.WeaponId),
                IsUnlocked = state.IsUnlocked,
                IsEquipped = IsAnyHeroEquippingStandard(def.WeaponId),
                CanEquip = state.IsUnlocked,
                CanFuse = CanFuseStandard(def.WeaponId),
                CanLevelUp = CanLevelUpStandard(def.WeaponId),
                CanLimitBreak = CanLimitBreakStandard(def.WeaponId),
                EquipEffects = BuildStatLines(def.EquipStats)
            };

            vm.UpgradePanel = BuildStandardUpgradePanel(def, state);
            return vm;
        }

        public WeaponDetailViewModel BuildExclusiveDetail(int heroId)
        {
            var def = WeaponManager.Instance.Database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return null;

            var state = WeaponManager.Instance.Inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            var equip = WeaponManager.Instance.Inventory.GetOrCreateHeroEquip(heroId);

            var vm = new WeaponDetailViewModel
            {
                ActiveSource = WeaponEquipSource.Exclusive,
                IsExclusive = true,
                WeaponId = def.ExclusiveWeaponId,
                HeroId = heroId,
                WeaponName = def.WeaponName,
                Icon = def.Icon,
                HeroClass = def.HeroClass,
                Tier = WeaponTier.SS,
                Star = state.CurrentStar,
                MaxStar = def.MaxStar,
                Level = state.Level,
                LimitBreakStage = state.LimitBreakStage,
                CurrentShard = state.CurrentShard,
                CurrentMaxLevel = GetCurrentMaxLevelForExclusive(heroId),
                IsUnlocked = state.IsUnlocked,
                IsEquipped = equip.EquippedExclusiveWeaponId == def.ExclusiveWeaponId && equip.UseExclusive,
                CanEquip = state.IsUnlocked,
                CanFuse = false,
                CanLevelUp = CanLevelUpExclusive(heroId),
                CanLimitBreak = CanLimitBreakExclusive(heroId),
                EquipEffects = BuildStatLines(def.EquipStats)
            };

            vm.UpgradePanel = BuildExclusiveUpgradePanel(def, state, heroId);
            return vm;
        }

        private WeaponUpgradePanelViewModel BuildStandardUpgradePanel(StandardWeaponDefinitionSO def, StandardWeaponState state)
        {
            var panel = new WeaponUpgradePanelViewModel();

            int currentMaxLevel = GetCurrentMaxLevelForStandard(def.WeaponId);
            int nextLevel = state.Level + 1;
            int nextLevelCost = def.LevelConfig != null ? def.LevelConfig.GetCost(nextLevel) : 0;

            panel.CurrentLevel = state.Level;
            panel.CurrentMaxLevel = currentMaxLevel;
            panel.NextLevelCost = nextLevelCost;
            panel.LevelUpAllCost = CalculateLevelUpAllCostStandard(def, state);
            panel.ShowLevelUp = state.Level < currentMaxLevel;
            panel.ShowLevelUpAll = state.Level < currentMaxLevel;
            panel.ShowLimitBreak = state.Level >= currentMaxLevel;
            panel.CanLevelUp = CanLevelUpStandard(def.WeaponId);
            panel.CanLevelUpAll = panel.LevelUpAllCost > 0 && CurrencyManager.Instance != null &&
                                  CurrencyManager.Instance.HasEnough(CurrencyType.WeaponEnhancementStone, panel.LevelUpAllCost);

            var nextBreakEntry = def.LimitBreakConfig != null
                ? def.LimitBreakConfig.GetEntryByStage(state.LimitBreakStage + 1)
                : null;

            panel.CanLimitBreak = CanLimitBreakStandard(def.WeaponId);
            panel.BreakThroughCost = nextBreakEntry != null ? nextBreakEntry.BreakThroughStoneCost : 0;
            panel.LimitBreakSuccessRate = nextBreakEntry != null ? nextBreakEntry.SuccessRate : 0f;
            panel.NextBreakRequiredLevel = nextBreakEntry != null ? nextBreakEntry.RequiredLevel : 0;

            return panel;
        }

        private WeaponUpgradePanelViewModel BuildExclusiveUpgradePanel(
            ExclusiveWeaponDefinitionSO def,
            ExclusiveWeaponState state,
            int heroId)
        {
            var panel = new WeaponUpgradePanelViewModel();

            int currentMaxLevel = GetCurrentMaxLevelForExclusive(heroId);
            int nextLevel = state.Level + 1;
            int nextLevelCost = def.LevelConfig != null ? def.LevelConfig.GetCost(nextLevel) : 0;

            panel.CurrentLevel = state.Level;
            panel.CurrentMaxLevel = currentMaxLevel;
            panel.NextLevelCost = nextLevelCost;
            panel.LevelUpAllCost = CalculateLevelUpAllCostExclusive(def, state, heroId);
            panel.ShowLevelUp = state.Level < currentMaxLevel;
            panel.ShowLevelUpAll = state.Level < currentMaxLevel;
            panel.ShowLimitBreak = state.Level >= currentMaxLevel;
            panel.CanLevelUp = CanLevelUpExclusive(heroId);
            panel.CanLevelUpAll = panel.LevelUpAllCost > 0 && CurrencyManager.Instance != null &&
                                  CurrencyManager.Instance.HasEnough(CurrencyType.WeaponEnhancementStone, panel.LevelUpAllCost);

            var nextBreakEntry = def.LimitBreakConfig != null
                ? def.LimitBreakConfig.GetEntryByStage(state.LimitBreakStage + 1)
                : null;

            panel.CanLimitBreak = CanLimitBreakExclusive(heroId);
            panel.BreakThroughCost = nextBreakEntry != null ? nextBreakEntry.BreakThroughStoneCost : 0;
            panel.LimitBreakSuccessRate = nextBreakEntry != null ? nextBreakEntry.SuccessRate : 0f;
            panel.NextBreakRequiredLevel = nextBreakEntry != null ? nextBreakEntry.RequiredLevel : 0;

            return panel;
        }

        private List<WeaponStatLineViewModel> BuildStatLines(WeaponStatBlock[] stats)
        {
            var result = new List<WeaponStatLineViewModel>();
            if (stats == null)
                return result;

            for (int i = 0; i < stats.Length; i++)
            {
                var item = stats[i];
                result.Add(new WeaponStatLineViewModel
                {
                    StatType = item.StatType,
                    Operation = item.Operation,
                    Value = item.Value,
                    DisplayName = GetStatDisplayName(item.StatType),
                    DisplayValue = GetStatDisplayValue(item.Operation, item.Value)
                });
            }

            return result;
        }

        private bool HasDeployedHeroUsingStandard(HeroClass heroClass)
        {
            if (deployedHeroes == null || deployedHeroes.Count == 0 || WeaponManager.Instance == null)
                return false;

            for (int i = 0; i < deployedHeroes.Count; i++)
            {
                var hero = deployedHeroes[i];
                if (hero == null)
                    continue;

                if (hero.HeroClass != heroClass)
                    continue;

                var source = WeaponManager.Instance.Inventory.ResolveActiveSource(hero.GetHeroId());
                if (source == WeaponEquipSource.Standard)
                    return true;
            }

            return false;
        }

        private bool IsAnyHeroEquippingStandard(int weaponId)
        {
            if (deployedHeroes == null || WeaponManager.Instance == null)
                return false;

            for (int i = 0; i < deployedHeroes.Count; i++)
            {
                var hero = deployedHeroes[i];
                if (hero == null)
                    continue;

                var equip = WeaponManager.Instance.Inventory.GetOrCreateHeroEquip(hero.GetHeroId());
                if (equip.EquippedStandardWeaponId == weaponId)
                    return true;
            }

            return false;
        }

        private bool CanLevelUpStandard(int weaponId)
        {
            if (WeaponManager.Instance == null)
                return false;

            var def = WeaponManager.Instance.Database.GetStandard(weaponId);
            if (def == null)
                return false;

            var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked)
                return false;

            int currentMax = GetCurrentMaxLevelForStandard(weaponId);
            if (state.Level >= currentMax)
                return false;

            int cost = def.LevelConfig != null ? def.LevelConfig.GetCost(state.Level + 1) : 0;
            return CurrencyManager.Instance != null &&
                   cost > 0 &&
                   CurrencyManager.Instance.HasEnough(CurrencyType.WeaponEnhancementStone, cost);
        }

        private bool CanLevelUpExclusive(int heroId)
        {
            if (WeaponManager.Instance == null)
                return false;

            var def = WeaponManager.Instance.Database.GetExclusiveByHeroId(heroId);
            if (def == null)
                return false;

            var state = WeaponManager.Instance.Inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            if (!state.IsUnlocked)
                return false;

            int currentMax = GetCurrentMaxLevelForExclusive(heroId);
            if (state.Level >= currentMax)
                return false;

            int cost = def.LevelConfig != null ? def.LevelConfig.GetCost(state.Level + 1) : 0;
            return CurrencyManager.Instance != null &&
                   cost > 0 &&
                   CurrencyManager.Instance.HasEnough(CurrencyType.WeaponEnhancementStone, cost);
        }

        private bool CanLimitBreakStandard(int weaponId)
        {
            if (WeaponManager.Instance == null)
                return false;

            var def = WeaponManager.Instance.Database.GetStandard(weaponId);
            if (def == null || def.LimitBreakConfig == null)
                return false;

            var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked)
                return false;

            var entry = def.LimitBreakConfig.GetEntryByStage(state.LimitBreakStage + 1);
            if (entry == null)
                return false;

            return state.Level >= entry.RequiredLevel &&
                   CurrencyManager.Instance != null &&
                   CurrencyManager.Instance.HasEnough(CurrencyType.WeaponBreakThroughStone, entry.BreakThroughStoneCost);
        }

        private bool CanLimitBreakExclusive(int heroId)
        {
            if (WeaponManager.Instance == null)
                return false;

            var def = WeaponManager.Instance.Database.GetExclusiveByHeroId(heroId);
            if (def == null || def.LimitBreakConfig == null)
                return false;

            var state = WeaponManager.Instance.Inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            if (!state.IsUnlocked)
                return false;

            var entry = def.LimitBreakConfig.GetEntryByStage(state.LimitBreakStage + 1);
            if (entry == null)
                return false;

            return state.Level >= entry.RequiredLevel &&
                   CurrencyManager.Instance != null &&
                   CurrencyManager.Instance.HasEnough(CurrencyType.WeaponBreakThroughStone, entry.BreakThroughStoneCost);
        }

        private bool CanFuseStandard(int weaponId)
        {
            if (WeaponManager.Instance == null)
                return false;

            var def = WeaponManager.Instance.Database.GetStandard(weaponId);
            if (def == null)
                return false;

            var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(weaponId);
            if (!state.IsUnlocked)
                return false;

            if (state.CurrentShard < def.FuseShardRequired)
                return false;

            if (def.FuseMode == WeaponFuseMode.ToRandomExclusive)
            {
                var currencyType = WeaponCurrencyHelper.GetClassStoneCurrency(def.ExclusivePoolClass);
                if (def.ExclusiveClassStoneCost > 0 && CurrencyManager.Instance != null)
                {
                    return CurrencyManager.Instance.HasEnough(currencyType, def.ExclusiveClassStoneCost);
                }
            }

            return true;
        }

        private int GetCurrentMaxLevelForStandard(int weaponId)
        {
            var def = WeaponManager.Instance.Database.GetStandard(weaponId);
            if (def == null || def.LimitBreakConfig == null)
                return 25;

            var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(weaponId);
            return def.LimitBreakConfig.GetMaxLevel(state.LimitBreakStage);
        }

        private int GetCurrentMaxLevelForExclusive(int heroId)
        {
            var def = WeaponManager.Instance.Database.GetExclusiveByHeroId(heroId);
            if (def == null || def.LimitBreakConfig == null)
                return 25;

            var state = WeaponManager.Instance.Inventory.GetOrCreateExclusiveState(def.ExclusiveWeaponId, heroId);
            return def.LimitBreakConfig.GetMaxLevel(state.LimitBreakStage);
        }

        private int CalculateLevelUpAllCostStandard(StandardWeaponDefinitionSO def, StandardWeaponState state)
        {
            if (def == null || def.LevelConfig == null)
                return 0;

            int maxLevel = GetCurrentMaxLevelForStandard(def.WeaponId);
            int total = 0;

            for (int lv = state.Level + 1; lv <= maxLevel; lv++)
            {
                total += def.LevelConfig.GetCost(lv);
            }

            return total;
        }

        private int CalculateLevelUpAllCostExclusive(ExclusiveWeaponDefinitionSO def, ExclusiveWeaponState state, int heroId)
        {
            if (def == null || def.LevelConfig == null)
                return 0;

            int maxLevel = GetCurrentMaxLevelForExclusive(heroId);
            int total = 0;

            for (int lv = state.Level + 1; lv <= maxLevel; lv++)
            {
                total += def.LevelConfig.GetCost(lv);
            }

            return total;
        }

        private string GetStatDisplayName(StatType statType)
        {
            switch (statType)
            {
                case StatType.Atk: return "Attack Power Increase";
                case StatType.MaxHp: return "HP Increase";
                case StatType.Def: return "Defense Increase";
                case StatType.CritChance: return "Crit Chance Increase";
                case StatType.CritDamage: return "Crit Damage Increase";
                case StatType.AttackSpeed: return "Attack Speed Increase";
                case StatType.Accuracy: return "Accuracy Increase";
                default: return statType.ToString();
            }
        }

        private string GetStatDisplayValue(ModifierOp op, float value)
        {
            if (op == ModifierOp.Multiply)
                return $"{value * 100f:0.##}%";

            return value.ToString("0.##");
        }
    }
}