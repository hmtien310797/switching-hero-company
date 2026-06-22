using System;
using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Shared.Database;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Immortal_Switch.Scripts.TransmutationSystem.Models
{
    [CreateAssetMenu(fileName = "TransmutationSystemDatabase", menuName = "ScriptableObjects/TransmutationSystem/Database")]
    public class TransmutationSystemDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// level transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmuationLevelConfigDatabase LevelConfig { get; private set; }

        /// <summary>
        /// level range transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmutationRandomLevelRangeConfigDatabase LevelRangeConfig { get; private set; }

        /// <summary>
        /// rate transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmutationRateConfigDatabase RateConfig { get; private set; }

        /// <summary>
        /// item transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmuationItemConfigDatabase ItemConfig { get; private set; }

        /// <summary>
        /// item unique transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmutationItemUniqueDatabase ItemUniqueConfig { get; private set; }

        /// <summary>
        /// unique transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmuationUniqueDatabase UniqueConfig { get; private set; }

        /// <summary>
        /// grade transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmuationGradeSettingDatabase GradeConfig { get; private set; }

        /// <summary>
        /// count transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsTransmutationCountSettingDatabase CountConfig { get; private set; }

        /// <summary>
        /// key: tier
        /// value: list item in tier
        /// </summary>
        private readonly Dictionary<EEquipmentTier, List<DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow>> _itemTiers =
            new();

        public void Load()
        {
            foreach (var entry in ItemConfig.rows)
            {
                var tier = Enum.TryParse<EEquipmentTier>(entry.tier, true, out var result) ? result : EEquipmentTier.D;

                if (_itemTiers.TryGetValue(tier, out var rows))
                {
                    rows.Add(entry);
                }
                else
                {
                    _itemTiers.Add(tier, new List<DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow>
                    {
                        entry,
                    });
                }
            }
        }

        public DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow RandomItem(EEquipmentTier tier)
        {
            if (!_itemTiers.TryGetValue(tier, out var rows))
            {
                return null;
            }

            var rnd = Random.Range(0, rows.Count);
            return rows[rnd];
        }
    }
}