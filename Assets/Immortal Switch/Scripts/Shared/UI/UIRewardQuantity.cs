using Immortal_Switch.Scripts.Core;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared.UI
{
    public class UIRewardQuantity : MonoBehaviour
    {
        [Header("Reward references")]
        [SerializeField]
        private UIItemSlot rewardSlot;

        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        public void Bind(int itemId, BigNumber quantity)
        {
            rewardSlot.Bind(itemId);

            txtQuantity.text = quantity.ToInputString();
        }
    }
}