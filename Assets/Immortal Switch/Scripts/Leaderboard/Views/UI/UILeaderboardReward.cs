using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.Leaderboard.Views.UI
{
    public class UILeaderboardReward : MonoBehaviour
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