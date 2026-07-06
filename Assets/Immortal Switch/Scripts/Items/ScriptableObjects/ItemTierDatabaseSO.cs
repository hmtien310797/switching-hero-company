using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.Items.ScriptableObjects
{
    [CreateAssetMenu(fileName = "ItemTierDatabase", menuName = "ScriptableObjects/Shared/ItemTier/Database")]
    public class ItemTierDatabaseSO : ScriptableObject
    {
        /// <summary>
        /// thong tin tier
        /// </summary>
        public ItemTierEntry[] entries;

        public ItemTierEntry Get(EItemTier tier)
        {
            return entries.FirstOrDefault(e => e.tier == tier);
        }
    }

    [Serializable]
    public class ItemTierEntry
    {
        [FormerlySerializedAs("type")]
        public EItemTier tier;

        [FormerlySerializedAs("tier")]
        [PreviewField]
        public Sprite tierIcon;

        [PreviewField]
        public Sprite border;

        [PreviewField]
        public Sprite background;
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