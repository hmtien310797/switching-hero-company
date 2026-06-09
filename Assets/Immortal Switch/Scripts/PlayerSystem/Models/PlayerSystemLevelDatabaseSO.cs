using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem.Models
{
    [CreateAssetMenu(fileName = "PlayerSystemLevelDatabase", menuName = "ScriptableObjects/PlayerSystem/LevelDatabase")]
    public class PlayerSystemLevelDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// ds level requires.
        /// </summary>
        [SerializeField] private PlayerSystemLevelEntry[] requires;

        /// <summary>
        /// level dict
        /// </summary>
        private readonly Dictionary<int, PlayerSystemLevelEntry> _levels = new();

        internal void Load()
        {
            _levels.Clear();

            foreach (var entry in requires)
            {
                _levels.TryAdd(entry.level, entry);
            }
        }

        public int LevelCount => _levels.Count;

        public int GetRequireExp(int level)
        {
            return _levels.TryGetValue(level, out var entry) ? entry.requireExp : 0;
        }
    }

    [Serializable]
    public struct PlayerSystemLevelEntry
    {
        /// <summary>
        /// level
        /// </summary>
        public int level;

        /// <summary>
        /// so exp yeu cau de upgrade.
        /// </summary>
        public int requireExp;
    }
}