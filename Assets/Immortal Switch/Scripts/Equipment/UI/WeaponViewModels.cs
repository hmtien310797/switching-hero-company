using System.Collections.Generic;
using Battle;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.UI
{
    public class WeaponClassTabViewModel
    {
        public HeroClass HeroClass;
        public bool IsSelected;
        public bool HasDeployedHeroUsingStandard;
    }

    public class WeaponStatLineViewModel
    {
        public StatType StatType;
        public ModifierOp Operation;
        public float Value;
        public string DisplayName;
        public string DisplayValue;
    }

    public class StandardWeaponCardViewModel
    {
        public int WeaponId;
        public string WeaponName;
        public HeroClass HeroClass;
        public WeaponTier Tier;
        public int Star;
        public Sprite Icon;

        public bool IsUnlocked;
        public bool IsEquipped;
        public int Level;
        public int LimitBreakStage;
        public int CurrentShard;
        public int MaxShard;
        public float ShardProgressNormalized;

        public bool CanFuse;
        public bool CanLevelUp;
        public bool CanLimitBreak;
        public bool IsSelected;
        public bool IsExclusive => false;
    }

    public class ExclusiveWeaponCardViewModel
    {
        public int ExclusiveWeaponId;
        public int HeroId;
        public string WeaponName;
        public HeroClass HeroClass;
        public Sprite Icon;

        public bool IsUnlocked;
        public bool IsEquipped;
        public int Level;
        public int LimitBreakStage;
        public int CurrentShard;
        public int MaxShard;
        public int CurrentStar;
        public int MaxStar;
        public float ShardProgressNormalized;

        public bool CanLevelUp;
        public bool CanLimitBreak;
        public bool CanTranscend;
        public bool IsSelected;
        public bool IsExclusive => true;
    }

    public class WeaponUpgradePanelViewModel
    {
        public WeaponUpgradePanelMode Mode;

        [Header("Weapon Preview")]
        public string WeaponName;
        public Sprite Icon;
        public int CurrentShard;
        public int MaxShard;
        public int CurrentStar;
        public int MaxStar;
        public int CurrentLevel;
        public int CurrentMaxLevel;
        public float ShardProgressNormalized;

        [Header("Upgrade Mode")]
        public bool ShowUpgradeMode;
        public bool ShowLevelUp;
        public bool ShowLevelUpAll;
        public bool CanLevelUp;
        public bool CanLevelUpAll;
        public int NextLevelCost;
        public int LevelUpAllCost;

        [Header("Limit Break Mode")]
        public bool ShowLimitBreakMode;
        public bool ShowLimitBreak;
        public bool CanLimitBreak;
        public int BreakThroughCost;
        public float LimitBreakSuccessRate;
        public int NextBreakRequiredLevel;
        public int NextMaxLevel;

        [Header("Stat Preview")]
        public List<WeaponUpgradeStatPreviewViewModel> StatPreviewLines = new();
    }

    public class WeaponUpgradeStatPreviewViewModel
    {
        public string StatName;
        public string CurrentValueText;
        public string NextValueText;
    }

    public class WeaponDetailViewModel
    {
        public WeaponEquipSource ActiveSource;
        public bool IsExclusive;

        public int WeaponId;
        public int HeroId;
        public string WeaponName;
        public Sprite Icon;

        public HeroClass HeroClass;
        public WeaponTier Tier;
        public int Star;
        public int MaxStar;

        public int Level;
        public int LimitBreakStage;
        public int CurrentShard;
        public int CurrentMaxLevel;
        public int MaxShard;
        public float ShardProgressNormalized;

        public bool IsUnlocked;
        public bool IsEquipped;

        public bool ShowEquip;
        public bool ShowAutoEquip;
        public bool ShowOpenUpgrade;
        public bool ShowFusion;
        public bool ShowFuseAll;

        public bool CanEquip;
        public bool CanAutoEquip;
        public bool CanOpenUpgrade;
        public bool CanFusion;
        public bool CanFuseAll;

        public List<WeaponStatLineViewModel> EquipEffects = new();
        public WeaponUpgradePanelViewModel UpgradePanel = new();
    }

    public class StandardWeaponTabViewModel
    {
        public HeroClass SelectedClass;
        public List<WeaponClassTabViewModel> ClassTabs = new();
        public List<StandardWeaponCardViewModel> Weapons = new();
        public WeaponDetailViewModel SelectedDetail;
    }

    public class ExclusiveWeaponTabViewModel
    {
        public int HeroId;
        public string HeroName;
        public ExclusiveWeaponCardViewModel ExclusiveCard;
        public WeaponDetailViewModel SelectedDetail;

        public bool ShowBaseStatsTab = true;
        public bool ShowOptionTab = true;
        public bool ShowExclusiveEffectTab = true;
        public bool ShowTranscendenceTab = true;
    }
}