using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponFuseAllRewardItem : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Image icon;
        [SerializeField] private Image tierLabelImage;
        [SerializeField] private Image tierBackgroundImage;
        [SerializeField] private UIWeaponStarDisplay starDisplay;
        [SerializeField] private WeaponTierVisualConfigSO tierVisualConfig;

        [Header("Amount")]
        [SerializeField] private TMP_Text txtAmount;

        public void Bind(WeaponFuseAllRewardEntry vm)
        {
            if (vm == null)
                return;

            if (icon != null)
                icon.sprite = vm.Icon;

            BindTierVisual(vm.IsExclusive ? WeaponTier.SS : vm.Tier);

            if (starDisplay != null)
            {
                if (vm.IsExclusive)
                    starDisplay.BindExclusive(vm.Star);
                else
                    starDisplay.BindStandard(vm.Star);
            }

            if (txtAmount != null)
                txtAmount.text = $"x{vm.Amount}";
        }

        private void BindTierVisual(WeaponTier tier)
        {
            if (tierVisualConfig == null)
                return;

            var entry = tierVisualConfig.Get(tier);
            if (entry == null)
                return;

            if (tierLabelImage != null)
                tierLabelImage.sprite = entry.TierLabelSprite;

            if (tierBackgroundImage != null)
                tierBackgroundImage.sprite = entry.TierBackgroundSprite;
        }
    }
}