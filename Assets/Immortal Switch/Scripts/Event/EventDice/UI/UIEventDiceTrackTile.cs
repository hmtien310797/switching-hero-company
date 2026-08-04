using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventDice.UI
{
    public class UIEventDiceTrackTile : MonoBehaviour
    {
        [SerializeField]
        private UIRewardQuantity rewardQuantity;

        [SerializeField]
        private GameObject goClaimed;

        public void Bind(int itemId, BigNumber quantity, bool isClaimed)
        {
            rewardQuantity.Bind(itemId, quantity);
            goClaimed.SetActive(isClaimed);
        }
    }
}