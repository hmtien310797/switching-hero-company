using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public class UIItemSlot : MonoBehaviour
    {
        [Header("View references")]
        [SerializeField]
        private Image imgItemIcon;

        [SerializeField]
        private Image imgBorder;

        [SerializeField]
        private Image imgBg;

        [SerializeField]
        private Image imgTier;

        public void Bind(Sprite itemIcon, Sprite borderIcon, Sprite bgIcon, Sprite tierIcon)
        {
            imgItemIcon.sprite = itemIcon;
            imgBorder.sprite = borderIcon;
            imgBg.sprite = bgIcon;
            imgTier.sprite = tierIcon;
        }
    }
}