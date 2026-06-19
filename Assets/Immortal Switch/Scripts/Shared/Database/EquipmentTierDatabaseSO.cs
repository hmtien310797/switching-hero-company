using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.Shared.Database
{
    [CreateAssetMenu(fileName = "EquipmentTierDatabase", menuName = "ScriptableObjects/Shared/EquipmentTierDatabase")]
    public class EquipmentTierDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// thong tin tier
        /// </summary>
        public EquipmentTierEntry[] entries;

        public EquipmentTierEntry Get(EEquipmentTier tier)
        {
            return entries.FirstOrDefault(e => e.type == tier);
        }
    }

    [Serializable]
    public class EquipmentTierEntry
    {
        public EEquipmentTier type;
        [PreviewField] public Sprite tier;
        [PreviewField] public Sprite border;
        [PreviewField] public Sprite background;
    }

    public enum EEquipmentTier
    {
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4,
        SS = 5,
        SSS = 6,
        R = 7,
        SR = 8,
    }
}