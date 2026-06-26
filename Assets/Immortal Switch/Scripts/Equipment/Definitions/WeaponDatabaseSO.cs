using System.Collections.Generic;
using Immortal_Switch.Scripts.Hero;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Definitions
{
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "ScriptableObjects/Equipment/WeaponDatabase")]
    public class WeaponDatabaseSO : ScriptableObject
    {
        public List<StandardWeaponDefinitionSO> StandardWeapons = new();
        public List<ExclusiveWeaponDefinitionSO> ExclusiveWeapons = new();
        public List<WeaponLevelConfigSO> LevelConfigs = new();
        public List<WeaponLimitBreakConfigSO> LimitBreakConfigs = new();

        public StandardWeaponDefinitionSO GetStandard(int weaponId)
        {
            return StandardWeapons.Find(x => x != null && x.WeaponId == weaponId);
        }

        public ExclusiveWeaponDefinitionSO GetExclusive(int exclusiveWeaponId)
        {
            return ExclusiveWeapons.Find(x => x != null && x.ExclusiveWeaponId == exclusiveWeaponId);
        }

        public ExclusiveWeaponDefinitionSO GetExclusiveByHeroId(int heroId)
        {
            return ExclusiveWeapons.Find(x => x != null && x.HeroId == heroId);
        }

        public List<StandardWeaponDefinitionSO> GetStandardsByClass(HeroClass heroClass)
        {
            return StandardWeapons.FindAll(x => x != null && x.WeaponClass == heroClass);
        }

        public List<ExclusiveWeaponDefinitionSO> GetExclusivesByClass(HeroClass heroClass)
        {
            return ExclusiveWeapons.FindAll(x => x != null && x.HeroClass == heroClass);
        }

        public WeaponLevelConfigSO GetLevelConfig(string configId)
        {
            return LevelConfigs.Find(x => x != null && x.ConfigId == configId);
        }

        public WeaponLimitBreakConfigSO GetLimitBreakConfig(string configId)
        {
            return LimitBreakConfigs.Find(x => x != null && x.ConfigId == configId);
        }
    }
}