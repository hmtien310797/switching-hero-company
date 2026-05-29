using System.Collections.Generic;
using Game.Configs.Generated;
using UnityEngine;

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
        /// key: tier
        /// value: list item in tier
        /// </summary>
        private readonly Dictionary<string, List<DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow>> _itemTiers = new();

        public void Load()
        {
            foreach (var entry in ItemConfig.rows)
            {
                if (_itemTiers.TryGetValue(entry.tier, out var rows))
                {
                    rows.Add(entry);
                }
                else
                {
                    _itemTiers.Add(entry.tier, new List<DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow>
                    {
                        entry,
                    });
                }
            }
        }

        public DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow RandomItem(string tier)
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