using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Hero;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Definitions
{
    [CreateAssetMenu(fileName = "StandardWeaponDefinition", menuName = "ScriptableObjects/Equipment/StandardWeaponDefinition")]
    public class StandardWeaponDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public int WeaponId;
        public string WeaponName;
        public HeroClass WeaponClass;
        public WeaponTier Tier;
        [Range(1, 5)] public int Star = 1;

        [Header("Visual")]
        [PreviewField] public Sprite Icon;

        [Header("Equip Stats")]
        public WeaponStatBlock[] EquipStats;

        [Header("Fuse")]
        public WeaponFuseMode FuseMode = WeaponFuseMode.None;
        public int NextWeaponId;
        public int FuseShardRequired = 3;
        public HeroClass ExclusivePoolClass;
        public BigNumber ExclusiveClassStoneCost;

        [Header("Shared Config")]
        public WeaponLevelConfigSO LevelConfig;
        public WeaponLimitBreakConfigSO LimitBreakConfig;
    }
}