using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopProductItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        [SerializeField]
        private Image imgIcon;

        [SerializeField]
        private Image imgBg;

        // --- Private Fields ---

        public void Bind(Sprite itemIcon, BigNumber quantity)
        {
            imgIcon.sprite = itemIcon;
            txtQuantity.text = quantity.ToInputString();
        }

        public void Bind(int itemId, BigNumber quantity)
        {
            var display = DatabaseManager.Instance.GetDisplayData(itemId);

            if (display != null)
            {
                imgIcon.sprite = display.ItemIcon;
            }

            txtQuantity.text = quantity.ToInputString();
        }
    }
}