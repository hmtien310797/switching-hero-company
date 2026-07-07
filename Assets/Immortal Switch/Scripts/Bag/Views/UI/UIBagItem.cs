using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using RecyclableScrollRect;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Bag.Views.UI
{
    public class UIBagItem : BaseItem
    {
        [SerializeField]
        private UIItemSlot itemSlot;

        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        public void Bind(Sprite itemIcon, Sprite borderIcon, Sprite bgIcon, Sprite tierIcon, BigNumber quantity)
        {
            itemSlot.Bind(itemIcon, borderIcon, bgIcon, tierIcon);
            txtQuantity.text = (quantity.ToInputString());
        }
    }
}