using Game.Configs.Generated;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem.Models
{
    [CreateAssetMenu(fileName = "MissionSystemDatabase", menuName = "ScriptableObjects/MissionSystem/Database")]
    public class MissionSystemDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// mission config
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsMissionConfigDatabase MissionConfig { get; private set; }

        /// <summary>
        /// mission point milestone config
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsMissionPointMilesStoneDatabase MissionPointMilesStoneConfig { get; private set; }
    }
}