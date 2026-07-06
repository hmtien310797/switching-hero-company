using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem.Models
{
    [CreateAssetMenu(fileName = "PlayerSystemDatabase", menuName = "ScriptableObjects/PlayerSystem/Database")]
    public class PlayerSystemDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// level database.
        /// </summary>
        [field: SerializeField]
        public PlayerSystemLevelDatabaseSO Level { get; private set; }

        public void Load()
        {
            Level.Load();
        }
    }
}