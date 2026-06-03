using Game.Configs.Generated;
using UnityEngine;

namespace Immortal_Switch.Scripts.MissionSystem.Models
{
    [CreateAssetMenu(fileName = "MissionSystemDatabase", menuName = "ScriptableObjects/MissionSystem/Database")]
    public class MissionSystemDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// level transmutation
        /// </summary>
        [field: SerializeField]
        public DynamicHeroesGlobalSpecificationsMissionConfigDatabase MissionConfig { get; private set; }
    }
}