using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.Database
{
    [CreateAssetMenu(fileName = "ItemTierDatabase", menuName = "ScriptableObjects/Shared/ItemTierDatabase")]
    public class ItemTierDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// thong tin tier
        /// </summary>
        public ItemTierEntry[] entries;

        public ItemTierEntry Get(EItemTier tier)
        {
            return entries.FirstOrDefault(e => e.type == tier);
        }
    }

    [Serializable]
    public class ItemTierEntry
    {
        public EItemTier type;
        [PreviewField] public Sprite tier;
        [PreviewField] public Sprite border;
        [PreviewField] public Sprite background;
    }

    public enum EItemTier
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