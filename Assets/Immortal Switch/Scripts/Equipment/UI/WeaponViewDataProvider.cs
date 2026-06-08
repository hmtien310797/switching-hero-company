using System.Collections.Generic;
using Battle;
using Common;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.Equipment.UIRuntime;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UI
{
    public class WeaponViewDataProvider : MonoBehaviour
    {
        [Header("Scene Hero Runtime")] [SerializeField]
        private List<HeroActor> deployedHeroes = new();
        
        [Header("Visual Config")]
        [SerializeField] private CurrencyVisualConfigSO currencyVisualConfig;

        public void SetDeployedHeroes(List<HeroActor> heroes)
        {
            deployedHeroes = heroes ?? new List<HeroActor>();
        }

        public StandardWeaponTabViewModel BuildStandardTab(HeroClass selectedClass, int selectedWeaponId,
            int focusedHeroId = 0)
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
                    IsEquipped = IsFocusedHeroEquippingStandard(focusedHeroId, def.WeaponId),
                    IsSelected = def.WeaponId == resolvedSelectedWeaponId,
                    Level = state.Level,
                    LimitBreakStage = state.LimitBreakStage,
                    CurrentShard = state.CurrentShard,
                    MaxShard = def.FuseShardRequired,
                    ShardProgressNormalized = def.FuseShardRequired > 0
                        ? Mathf.Clamp01((float)state.CurrentShard / def.FuseShardRequired)
                        : 0f,
                    CanFuse = CanFuseStandard(def.WeaponId),
                    CanLevelUp = CanLevelUpStandard(def.WeaponId),
                    CanLimitBreak = CanLimitBreakStandard(def.WeaponId)
                });
            }
            
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
                IsEquipped = IsFocusedHeroEquippingExclusive(heroId, def.ExclusiveWeaponId),
                IsSelected = true,
                Level = state.Level,
                LimitBreakStage = state.LimitBreakStage,
                CurrentShard = state.CurrentShard,
                MaxShard = 0,
                ShardProgressNormalized = 0f,
                CurrentStar = state.CurrentStar,
                MaxStar = def.MaxStar,
                CanLevelUp = CanLevelUpExclusive(heroId),
                CanLimitBreak = CanLimitBreakExclusive(heroId),
                CanTranscend = false
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
            bool isEquippedByFocusedHero = IsFocusedHeroEquippingStandard(focusedHeroId, def.WeaponId);
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
                IsEquipped = isEquippedByFocusedHero,

                ShowEquip = state.IsUnlocked && !isEquippedByFocusedHero,
                ShowAutoEquip = true,
                ShowOpenUpgrade = true,
                ShowFusion = IsHighestTierAndHighestStar(def),
                ShowFuseAll = true,

                CanEquip = state.IsUnlocked && !isEquippedByFocusedHero,
                CanAutoEquip = HasAnyDeployedHeroOfClass(def.WeaponClass),
                CanOpenUpgrade = state.IsUnlocked,
                CanFusion = IsHighestTierAndHighestStar(def),
                CanFuseAll = true, // phase này để true, sau này có service global thì check thật

                EquipEffects = BuildStatLines(def.EquipStats),
                MaxShard = def.FuseShardRequired,
                ShardProgressNormalized = def.FuseShardRequired > 0
                    ? Mathf.Clamp01((float)state.CurrentShard / def.FuseShardRequired)
                    : 0f,
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
            bool isEquippedByFocusedHero = IsFocusedHeroEquippingExclusive(heroId, def.ExclusiveWeaponId);
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
                IsEquipped = isEquippedByFocusedHero,
                
                ShowEquip = state.IsUnlocked && !isEquippedByFocusedHero,
                CanEquip = state.IsUnlocked && !isEquippedByFocusedHero,
                ShowAutoEquip = true,
                ShowOpenUpgrade = true,
                ShowFusion = false,
                ShowFuseAll = false,
                
                CanAutoEquip = true,
                CanOpenUpgrade = state.IsUnlocked,
                CanFusion = false,
                CanFuseAll = false,

                EquipEffects = BuildStatLines(def.EquipStats)
            };

            vm.UpgradePanel = BuildExclusiveUpgradePanel(def, state, heroId);
            return vm;
        }
        
        public WeaponFusionPopupViewModel BuildFusionPopup(int weaponId)
        {
            if (WeaponManager.Instance == null)
                return null;

            var def = WeaponManager.Instance.Database.GetStandard(weaponId);
            if (def == null)
                return null;

            var state = WeaponManager.Instance.Inventory.GetOrCreateStandardState(weaponId);
            if (state == null || !state.IsUnlocked)
                return null;

            var currencyType = WeaponCurrencyHelper.GetClassStoneCurrency(def.ExclusivePoolClass);
            BigNumber currentCurrency = CurrencyManager.Instance != null
                ? CurrencyManager.Instance.Get(currencyType)
                : 0;

            int maxByShard = def.FuseShardRequired > 0
                ? state.CurrentShard / def.FuseShardRequired
                : 0;

            int maxByCurrency = def.ExclusiveClassStoneCost > 0
                ? (currentCurrency / def.ExclusiveClassStoneCost).FloorToIntSafe()
                : 0;

            int maxFusionCount = Mathf.Min(maxByShard, maxByCurrency);
            maxFusionCount = Mathf.Max(0, maxFusionCount);

            return new WeaponFusionPopupViewModel
            {
                WeaponId = def.WeaponId,
                WeaponName = def.WeaponName,
                WeaponIcon = def.Icon,
                Tier = def.Tier,
                Star = def.Star,
                CurrentShard = state.CurrentShard,
                RequiredShardPerFusion = def.FuseShardRequired,

                ConsumableCurrencyType = currencyType,
                ConsumableCurrencyIcon = currencyVisualConfig != null
                    ? currencyVisualConfig.GetIcon(currencyType)
                    : null,
                CurrentConsumableAmount = currentCurrency,
                ConsumableCostPerFusion = def.ExclusiveClassStoneCost,

                CurrentFusionCount = maxFusionCount > 0 ? 1 : 0,
                MaxFusionCount = maxFusionCount,

                CanFusion = maxFusionCount > 0
            };
        }

        private WeaponUpgradePanelViewModel BuildStandardUpgradePanel(StandardWeaponDefinitionSO def,
            StandardWeaponState state)
        {
            var panel = new WeaponUpgradePanelViewModel();

            int currentMaxLevel = GetCurrentMaxLevelForStandard(def.WeaponId);
            int nextLevel = state.Level + 1;
            int nextLevelCost = def.LevelConfig != null ? def.LevelConfig.GetCost(nextLevel) : 0;

            var nextBreakEntry = def.LimitBreakConfig != null
                ? def.LimitBreakConfig.GetEntryByStage(state.LimitBreakStage + 1)
                : null;

            bool isAtCurrentCap = state.Level >= currentMaxLevel;

            panel.WeaponName = def.WeaponName;
            panel.Icon = def.Icon;
            panel.CurrentShard = state.CurrentShard;
            panel.MaxShard = def.FuseShardRequired;
            panel.CurrentStar = def.Star;
            panel.MaxStar = def.Star;
            panel.CurrentLevel = state.Level;
            panel.CurrentMaxLevel = currentMaxLevel;

            panel.Mode = isAtCurrentCap ? WeaponUpgradePanelMode.LimitBreak : WeaponUpgradePanelMode.Upgrade;

            panel.ShowUpgradeMode = !isAtCurrentCap;
            panel.ShowLevelUp = !isAtCurrentCap;
            panel.ShowLevelUpAll = !isAtCurrentCap;
            panel.CanLevelUp = CanLevelUpStandard(def.WeaponId);
            panel.CanLevelUpAll = CalculateLevelUpAllCostStandard(def, state) > 0 &&
                                  CurrencyManager.Instance != null &&
                                  CurrencyManager.Instance.HasEnough(
                                      CurrencyType.WeaponEnhancementStone,
                                      CalculateLevelUpAllCostStandard(def, state)
                                  );

            panel.NextLevelCost = nextLevelCost;
            panel.LevelUpAllCost = CalculateLevelUpAllCostStandard(def, state);

            panel.ShowLimitBreakMode = isAtCurrentCap;
            panel.ShowLimitBreak = isAtCurrentCap && nextBreakEntry != null;
            panel.CanLimitBreak = CanLimitBreakStandard(def.WeaponId);
            panel.BreakThroughCost = nextBreakEntry != null ? nextBreakEntry.BreakThroughStoneCost : 0;
            panel.LimitBreakSuccessRate = nextBreakEntry != null ? nextBreakEntry.SuccessRate : 0f;
            panel.NextBreakRequiredLevel = nextBreakEntry != null ? nextBreakEntry.RequiredLevel : 0;
            panel.NextMaxLevel = def.LimitBreakConfig != null
                ? def.LimitBreakConfig.GetMaxLevel(state.LimitBreakStage + 1)
                : currentMaxLevel;
            panel.ShardProgressNormalized = def.FuseShardRequired > 0
                ? Mathf.Clamp01((float)state.CurrentShard / def.FuseShardRequired)
                : 0f;

            panel.StatPreviewLines = BuildStandardUpgradeStatPreview(def, state);

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

            var nextBreakEntry = def.LimitBreakConfig != null
                ? def.LimitBreakConfig.GetEntryByStage(state.LimitBreakStage + 1)
                : null;

            bool isAtCurrentCap = state.Level >= currentMaxLevel;

            panel.WeaponName = def.WeaponName;
            panel.Icon = def.Icon;
            panel.CurrentShard = state.CurrentShard;
            panel.MaxShard = 0;
            panel.CurrentStar = state.CurrentStar;
            panel.MaxStar = def.MaxStar;
            panel.CurrentLevel = state.Level;
            panel.CurrentMaxLevel = currentMaxLevel;

            panel.Mode = isAtCurrentCap ? WeaponUpgradePanelMode.LimitBreak : WeaponUpgradePanelMode.Upgrade;

            panel.ShowUpgradeMode = !isAtCurrentCap;
            panel.ShowLevelUp = !isAtCurrentCap;
            panel.ShowLevelUpAll = !isAtCurrentCap;
            panel.CanLevelUp = CanLevelUpExclusive(heroId);
            panel.CanLevelUpAll = CalculateLevelUpAllCostExclusive(def, state, heroId) > 0 &&
                                  CurrencyManager.Instance != null &&
                                  CurrencyManager.Instance.HasEnough(
                                      CurrencyType.WeaponEnhancementStone,
                                      CalculateLevelUpAllCostExclusive(def, state, heroId)
                                  );

            panel.NextLevelCost = nextLevelCost;
            panel.LevelUpAllCost = CalculateLevelUpAllCostExclusive(def, state, heroId);

            panel.ShowLimitBreakMode = isAtCurrentCap;
            panel.ShowLimitBreak = isAtCurrentCap && nextBreakEntry != null;
            panel.CanLimitBreak = CanLimitBreakExclusive(heroId);
            panel.BreakThroughCost = nextBreakEntry != null ? nextBreakEntry.BreakThroughStoneCost : 0;
            panel.LimitBreakSuccessRate = nextBreakEntry != null ? nextBreakEntry.SuccessRate : 0f;
            panel.NextBreakRequiredLevel = nextBreakEntry != null ? nextBreakEntry.RequiredLevel : 0;
            panel.NextMaxLevel = def.LimitBreakConfig != null
                ? def.LimitBreakConfig.GetMaxLevel(state.LimitBreakStage + 1)
                : currentMaxLevel;
            panel.CurrentShard = state.CurrentShard;
            panel.MaxShard = 0;
            panel.ShardProgressNormalized = 0f;

            panel.StatPreviewLines = BuildExclusiveUpgradeStatPreview(def, state);

            return panel;
        }
        
        private List<WeaponUpgradeStatPreviewViewModel> BuildStandardUpgradeStatPreview(
            StandardWeaponDefinitionSO def,
            StandardWeaponState state)
        {
            var result = new List<WeaponUpgradeStatPreviewViewModel>();
            if (def == null || def.EquipStats == null)
                return result;

            for (int i = 0; i < def.EquipStats.Length; i++)
            {
                var stat = def.EquipStats[i];
                float currentValue = GetScaledWeaponStatValue(stat.Value, state.Level);
                float nextValue = GetScaledWeaponStatValue(stat.Value, state.Level + 1);

                result.Add(new WeaponUpgradeStatPreviewViewModel
                {
                    StatName = GetStatDisplayName(stat.StatType),
                    CurrentValueText = GetStatDisplayValue(stat.Operation, currentValue),
                    NextValueText = GetStatDisplayValue(stat.Operation, nextValue)
                });
            }

            return result;
        }

        private List<WeaponUpgradeStatPreviewViewModel> BuildExclusiveUpgradeStatPreview(
            ExclusiveWeaponDefinitionSO def,
            ExclusiveWeaponState state)
        {
            var result = new List<WeaponUpgradeStatPreviewViewModel>();
            if (def == null || def.EquipStats == null)
                return result;

            for (int i = 0; i < def.EquipStats.Length; i++)
            {
                var stat = def.EquipStats[i];
                float currentValue = GetScaledWeaponStatValue(stat.Value, state.Level);
                float nextValue = GetScaledWeaponStatValue(stat.Value, state.Level + 1);

                result.Add(new WeaponUpgradeStatPreviewViewModel
                {
                    StatName = GetStatDisplayName(stat.StatType),
                    CurrentValueText = GetStatDisplayValue(stat.Operation, currentValue),
                    NextValueText = GetStatDisplayValue(stat.Operation, nextValue)
                });
            }

            return result;
        }

        private float GetScaledWeaponStatValue(float baseValue, int level)
        {
            if (level <= 1)
                return baseValue;

            return baseValue * (1f + (level - 1) * 0.05f);
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
                   CurrencyManager.Instance.HasEnough(CurrencyType.WeaponBreakThroughStone,
                       entry.BreakThroughStoneCost);
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
                   CurrencyManager.Instance.HasEnough(CurrencyType.WeaponBreakThroughStone,
                       entry.BreakThroughStoneCost);
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

        private int CalculateLevelUpAllCostExclusive(ExclusiveWeaponDefinitionSO def, ExclusiveWeaponState state,
            int heroId)
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

        private bool HasAnyDeployedHeroOfClass(HeroClass heroClass)
        {
            if (deployedHeroes == null || deployedHeroes.Count == 0)
                return false;

            for (int i = 0; i < deployedHeroes.Count; i++)
            {
                var hero = deployedHeroes[i];
                if (hero == null)
                    continue;

                if (hero.HeroClass == heroClass && hero.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private bool IsHighestTierAndHighestStar(StandardWeaponDefinitionSO def)
        {
            if (def == null)
                return false;

            // phase hiện tại rule là SS5
            return def.Tier == WeaponTier.SS && def.Star == 5;
        }

        private string GetStatDisplayName(StatType statType)
        {
            switch (statType)
            {
                case StatType.Atk: return "Tăng Công";
                case StatType.MaxHp: return "Tăng HP";
                case StatType.Def: return "Tăng Phòng thủ";
                case StatType.CritChance: return "Tăng Tỷ lệ Chí mạng";
                case StatType.CritDamage: return "Tăng Sát thương Chí mạng";
                case StatType.AttackSpeed: return "Tăng Tốc độ Đánh";
                case StatType.Accuracy: return "Tăng Chính xác";
                default: return statType.ToString();
            }
        }

        private string GetStatDisplayValue(ModifierOp op, float value)
        {
            if (op == ModifierOp.Multiply)
                return $"{value * 100f:0.##}%";

            return value.ToString("0.##");
        }
        
        private bool IsFocusedHeroEquippingStandard(int heroId, int weaponId)
        {
            if (WeaponManager.Instance == null || heroId <= 0 || weaponId <= 0)
                return false;

            var equip = WeaponManager.Instance.Inventory.GetOrCreateHeroEquip(heroId);
            if (equip == null)
                return false;

            return equip.EquippedStandardWeaponId == weaponId &&
                   WeaponManager.Instance.Inventory.ResolveActiveSource(heroId) == WeaponEquipSource.Standard;
        }

        private bool IsFocusedHeroEquippingExclusive(int heroId, int exclusiveWeaponId)
        {
            if (WeaponManager.Instance == null || heroId <= 0 || exclusiveWeaponId <= 0)
                return false;

            var equip = WeaponManager.Instance.Inventory.GetOrCreateHeroEquip(heroId);
            if (equip == null)
                return false;

            return equip.EquippedExclusiveWeaponId == exclusiveWeaponId &&
                   WeaponManager.Instance.Inventory.ResolveActiveSource(heroId) == WeaponEquipSource.Exclusive;
        }
    }
}