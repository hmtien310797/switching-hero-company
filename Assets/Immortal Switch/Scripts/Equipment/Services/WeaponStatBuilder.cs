using System.Collections.Generic;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Equipment.Models;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Equipment.Services
{
    public static class WeaponStatBuilder
    {
        public static List<StatModifier> BuildForStandard(
            StandardWeaponDefinitionSO def,
            StandardWeaponState state,
            string sourceId)
        {
            var result = new List<StatModifier>();
            if (def == null || state == null || def.EquipStats == null)
                return result;

            float levelMultiplier = GetLevelMultiplier(state.Level);

            for (int i = 0; i < def.EquipStats.Length; i++)
            {
                var item = def.EquipStats[i];
                float finalValue = item.Value * levelMultiplier;

                result.Add(new StatModifier(
                    item.StatType,
                    item.Operation,
                    finalValue,
                    sourceId
                ));
            }

            return result;
        }

        public static List<StatModifier> BuildForExclusive(
            ExclusiveWeaponDefinitionSO def,
            ExclusiveWeaponState state,
            string sourceId)
        {
            var result = new List<StatModifier>();
            if (def == null || state == null || def.EquipStats == null)
                return result;

            float levelMultiplier = GetLevelMultiplier(state.Level);

            for (int i = 0; i < def.EquipStats.Length; i++)
            {
                var item = def.EquipStats[i];
                float finalValue = item.Value * levelMultiplier;

                result.Add(new StatModifier(
                    item.StatType,
                    item.Operation,
                    finalValue,
                    sourceId
                ));
            }

            return result;
        }

        private static float GetLevelMultiplier(int level)
        {
            if (level <= 1)
                return 1f;

            return 1f + (level - 1) * 0.05f;
        }
    }
}