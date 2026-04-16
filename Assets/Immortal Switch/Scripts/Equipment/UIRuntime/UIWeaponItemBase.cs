using System;
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
        [SerializeField] protected Image tierImage;
        [SerializeField] protected TMP_Text txtLevel;
        [SerializeField] protected TMP_Text txtShard;
        [SerializeField] protected TMP_Text txtStar;
        [SerializeField] protected GameObject equippedMark;
        [SerializeField] protected GameObject lockedMask;
        [SerializeField] protected GameObject redDot;
        
        [Header("Selection")]
        [SerializeField] protected GameObject selectedMark;

        protected Action onClick;

        protected void BindCommon(
            Sprite iconSprite,
            string levelText,
            string shardText,
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

            if (txtStar != null)
                txtStar.text = starText;

            if (equippedMark != null)
                equippedMark.SetActive(isEquipped);

            if (lockedMask != null)
                lockedMask.SetActive(isLocked);

            if (redDot != null)
                redDot.SetActive(showRedDot);

            if (selectedMark != null)
                selectedMark.SetActive(isSelected);

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
    }
}