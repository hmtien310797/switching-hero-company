using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PvpItemReward : MonoBehaviour
{
    [SerializeField] private Image imageTier;

    [SerializeField] private TMP_Text tierLevel;

    [SerializeField] private TMP_Text tierName;

    [SerializeField] private TMP_Text tierThreshold;

    [SerializeField] private UIRewardQuantity[] uiRewardQuantities;

    public void Bind(Sprite spriteTier, string level, string name, string threshold, ItemRewardData[] pvpItemReward)
    {
        imageTier.sprite = spriteTier;
        tierLevel.text = level;
        tierName.text = name;
        tierThreshold.text = threshold;
        for (int i = 0; i < uiRewardQuantities.Length; i++)
        {
            uiRewardQuantities[i].Bind(pvpItemReward[i].ItemId, pvpItemReward[i].Quantity);
        }
    }
}
