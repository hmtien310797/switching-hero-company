using Immortal_Switch.Scripts.Level.Stage;
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
                //icon.sprite = rewardIcon;
                //icon.gameObject.SetActive(rewardIcon != null);
            }

            if (amountText != null)
            {
                amountText.text = reward.Amount.ToString();
            }
        }

    }
}