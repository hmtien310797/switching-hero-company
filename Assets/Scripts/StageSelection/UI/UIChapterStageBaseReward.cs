using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;

namespace StageSelection.UI
{
    public class UIChapterStageBaseReward : MonoBehaviour
    {
        [SerializeField]
        private UIItemSlot slot;

        [SerializeField]
        private TMP_Text txtAmount;

        public void Bind(StageReward reward)
        {
            var set = DatabaseManager.Instance.GetDisplayDataByCurrency(reward.currencyType);

            if (set != null)
            {
                slot.Bind(set.ItemIcon, set.TierInfo.border, set.TierInfo.background, set.TierInfo.tierIcon);
            }

            txtAmount.text = reward.Amount.ToInputString();
        }
    }
}