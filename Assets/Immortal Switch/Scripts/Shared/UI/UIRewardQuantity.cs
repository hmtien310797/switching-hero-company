using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.UIRuntime;
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

        [SerializeField] 
        private UIWeaponStarDisplay uiWeaponStarDisplay;

        public void Bind(int itemId, BigNumber quantity)
        {
            rewardSlot.Bind(itemId);

            txtQuantity.text = quantity.ToInputString();
        }

        public void Bind(Sprite itemIcon, Sprite borderIcon, Sprite bgIcon, Sprite tierIcon, BigNumber quantity, int star = 0)
        {
            rewardSlot.Bind(itemIcon, borderIcon, bgIcon, tierIcon);

            txtQuantity.text = quantity.ToInputString();
            
            //temp

            if (uiWeaponStarDisplay == null)
                return;
            
            if (star == 0)
            {
                uiWeaponStarDisplay.gameObject.SetActive(false);
            }
            else
            {
                uiWeaponStarDisplay.gameObject.SetActive(true);
                uiWeaponStarDisplay.BindStandard(star);
            }
        }
    }
}