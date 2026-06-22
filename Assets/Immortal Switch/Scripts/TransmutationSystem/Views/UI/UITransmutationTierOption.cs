using System;
using Immortal_Switch.Scripts.Shared.Database;
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
        public EEquipmentTier Tier { get; private set; }

        public void Bind(EEquipmentTier type, Sprite tier, int idx, string txt, Action<int> onClick)
        {
            Tier = type;
            imgTier.sprite = tier;
            Bind(idx, txt, onClick);
        }
    }
}