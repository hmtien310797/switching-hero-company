using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Definitions
{
    [CreateAssetMenu(fileName = "ExclusiveWeaponDefinition", menuName = "ScriptableObjects/Equipment/ExclusiveWeaponDefinition")]
    public class ExclusiveWeaponDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public int ExclusiveWeaponId;
        public string WeaponName;
        public int HeroId;
        public HeroClass HeroClass;

        [Header("Visual")]
        public Sprite Icon;

        [Header("Star")]
        [Min(1)] public int StartingStar = 1;
        [Min(1)] public int MaxStar = 15;

        [Header("Equip Stats")]
        public WeaponStatBlock[] EquipStats;

        [Header("Shared Config")]
        public WeaponLevelConfigSO LevelConfig;
        public WeaponLimitBreakConfigSO LimitBreakConfig;
    }
}