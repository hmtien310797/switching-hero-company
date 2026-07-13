using Immortal_Switch.Scripts.Core;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public class UIReward : UIItemSlot
    {
        [Header("Reward references")]
        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        public void BindQuantity(BigNumber quantity)
        { 
            txtQuantity.text = quantity.ToInputString();
        }
    }
}