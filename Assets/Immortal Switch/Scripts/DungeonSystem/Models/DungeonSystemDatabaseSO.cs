using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.DungeonSystem.Models
{
    [CreateAssetMenu(fileName = "DungeonSystemDatabase", menuName = "ScriptableObjects/DungeonSystem/Database")]
    public class DungeonSystemDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// du lieu dungeon
        /// </summary>
        [field: SerializeField]
        public List<DungeonSystemSpriteEntry> Entries { get; private set; }

        [CanBeNull]
        public DungeonSystemSpriteEntry Get(EDungeonType type)
        {
            return Entries.FirstOrDefault(v => v.type == type);
        }
    }

    [Serializable]
    public class DungeonSystemSpriteEntry
    {
        public EDungeonType type;
        [PreviewField] public Sprite banner;
        [PreviewField] public Sprite background;
    }

    public enum EDungeonType
    {
        /// <summary>
        /// ham kho bau
        /// </summary>
        Treasure = 0,

        /// <summary>
        /// ham tin vat
        /// </summary>
        Artifact = 1,

        /// <summary>
        /// ham kim cuong
        /// </summary>
        Diamond = 2,

        /// <summary>
        /// ham trang bi
        /// </summary>
        Equipment = 3,
    }
}