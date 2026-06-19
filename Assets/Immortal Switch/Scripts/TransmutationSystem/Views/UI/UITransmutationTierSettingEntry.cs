using System;
using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationTierSettingEntry : UITabPreset
    {
        [Header("References")] [SerializeField]
        private Image imgTier;

        // --- Private Fields ---

        public void Bind(Sprite tier, int idx, string txt, Action<int> onClick)
        {
            imgTier.sprite = tier;
            Bind(idx, txt, onClick);
        }
    }
}