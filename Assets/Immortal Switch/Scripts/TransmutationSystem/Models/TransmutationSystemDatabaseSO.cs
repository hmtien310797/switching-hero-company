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
    }
}