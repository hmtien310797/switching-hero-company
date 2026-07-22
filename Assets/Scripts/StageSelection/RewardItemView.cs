using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class RewardItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        public void Bind(StageReward reward)
        {
            if (icon != null)
            {
                var itemDisplay = DatabaseManager.Instance.GetDisplayDataByCurrency(reward.currencyType);
                icon.sprite = itemDisplay?.ItemIcon;
                icon.gameObject.SetActive(itemDisplay?.ItemIcon != null);
            }

            if (amountText != null)
            {
                amountText.text = reward.Amount.ToString();
            }
        }

    }
}