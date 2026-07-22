using System;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public abstract class UIWeaponItemBase : MonoBehaviour
    {
        [Header("Common UI")]
        [SerializeField] protected Button button;
        [SerializeField] protected Image icon;
        [SerializeField] protected TMP_Text txtLevel;
        [SerializeField] protected TMP_Text txtShard;
        [SerializeField] protected TMP_Text txtStar;
        [SerializeField] protected GameObject equippedMark;
        [SerializeField] protected GameObject lockedMask;
        [SerializeField] protected GameObject redDot;

        [Header("Selection")]
        [SerializeField] protected GameObject selectedMark;

        [Header("Shard Progress")]
        [SerializeField] protected Image shardSlider;
        
        [Header("Tier Visual")]
        [SerializeField] protected Image tierLabelImage;
        [SerializeField] protected Image tierBackgroundImage;
        [SerializeField] protected Image tierBorderImage;

        [Header("Star Display")]
        [SerializeField] protected UIWeaponStarDisplay starDisplay;
        
        protected Action onClick;

        public void BindCommon(
            Sprite iconSprite,
            string levelText,
            string shardText,
            float shardProgressNormalized,
            bool showShardSlider,
            string starText,
            bool isEquipped,
            bool isLocked,
            bool showRedDot,
            bool isSelected,
            Action clickCallback)
        {
            onClick = clickCallback;

            if (icon != null)
                icon.sprite = iconSprite;

            if (txtLevel != null)
                txtLevel.text = levelText;

            if (txtShard != null)
                txtShard.text = shardText;
            
            if(equippedMark != null)
                equippedMark.gameObject.SetActive(isEquipped);

            if (lockedMask != null)
                lockedMask.SetActive(isLocked);
            
            if (selectedMark != null)
                selectedMark.SetActive(isSelected);

            if (shardSlider != null)
            {
                shardSlider.gameObject.SetActive(showShardSlider);
                shardSlider.fillAmount = shardProgressNormalized;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        protected virtual void HandleClick()
        {
            onClick?.Invoke();
        }
        
        public void BindTierVisual(WeaponTier tier)
        {
            var entry = ItemTierVisualImageService.GetItemTierEntry(tier);
            if (entry == null)
                return;

            if (tierLabelImage != null)
                tierLabelImage.sprite = entry.tierIcon;

            if (tierBackgroundImage != null)
                tierBackgroundImage.sprite = entry.background;

            if (tierBorderImage != null)
            {
                tierBorderImage.sprite = entry.border;
            }
        }
    }
}