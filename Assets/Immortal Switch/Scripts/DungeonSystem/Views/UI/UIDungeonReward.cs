using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.DungeonSystem.Views.UI
{
    public class UIDungeonReward : UIItemSlot
    {
        [Header("Reward references")] [SerializeField]
        private TextMeshProUGUI txtQuantity;

        public void BindQuantity(BigNumber quantity)
        {
            txtQuantity.text = quantity.ToInputString();
        }
    }
}