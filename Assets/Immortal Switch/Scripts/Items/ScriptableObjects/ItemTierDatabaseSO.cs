using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Immortal_Switch.Scripts.Items.ScriptableObjects
{
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