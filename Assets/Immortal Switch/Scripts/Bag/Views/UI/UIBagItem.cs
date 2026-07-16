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

        public void Bind(int itemId, BigNumber quantity)
        {
            itemSlot.Bind(itemId, true);
            txtQuantity.text = quantity.ToInputString();
        }
    }
}