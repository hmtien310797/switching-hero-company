using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;

namespace StageSelection.UI
{
    public class UIChapterStageBaseReward : MonoBehaviour
    {
        [SerializeField] private UIBagSlot slot;
        [SerializeField] private TMP_Text txtAmount;

        public async UniTask Bind(StageReward reward)
        {
            var set = await DatabaseManager.Instance.GetSpriteSetByCurrency(reward.currencyType);

            if (set != null)
            {
                slot.Bind(set.ItemIcon, set.TierInfo.border, set.TierInfo.background, set.TierInfo.tier);
            }

            txtAmount.text = reward.Amount.ToInputString();
        }
    }
}