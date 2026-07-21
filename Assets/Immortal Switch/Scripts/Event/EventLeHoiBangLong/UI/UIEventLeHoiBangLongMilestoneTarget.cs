using Immortal_Switch.Scripts.Shared.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.UI
{
    public class UIEventLeHoiBangLongMilestoneTarget : UIRewardQuantity
    {
        [SerializeField]
        private GameObject overlayClaimed;

        public void SetClaimed(bool active)
        {
            overlayClaimed.SetActive(active);
        }
    }
}