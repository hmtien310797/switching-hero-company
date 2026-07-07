using System;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationTierOption : UITabPreset
    {
        [Header("References")] [SerializeField]
        private Image imgTier;

        // --- Private Fields ---
        public EItemTier Tier { get; private set; }

        public void Bind(EItemTier type, Sprite tier, int idx, string txt, Action<int> onClick)
        {
            Tier = type;
            imgTier.sprite = tier;
            Bind(idx, txt, onClick);
        }
    }
}