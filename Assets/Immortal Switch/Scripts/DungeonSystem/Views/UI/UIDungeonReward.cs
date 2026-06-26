using System.Numerics;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.DungeonSystem.Views.UI
{
    public class UIDungeonReward : UIBagSlot
    {
        [Header("Reward references")] [SerializeField]
        private TextMeshProUGUI txtQuantity;

        public void BindQuantity(BigInteger quantity)
        {
            txtQuantity.text = BigIntegerHelper.Format(quantity);
        }
    }
}