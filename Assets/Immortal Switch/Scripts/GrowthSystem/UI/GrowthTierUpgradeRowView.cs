using Immortal_Switch.Scripts.GrowthSystem.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GrowthSystem
{
    public class GrowthTierUpgradeRowView : MonoBehaviour
    {
        [SerializeField] private Image statIcon;
        [SerializeField] private TMP_Text statNameText;
        [SerializeField] private TMP_Text leftValueText;
        [SerializeField] private GameObject arrowObject;
        [SerializeField] private TMP_Text rightValueText;

        public void Bind(GrowthTierUpgradeRowData data)
        {
            if (statIcon != null)
                statIcon.sprite = data.StatIcon;

            if (statNameText != null)
                statNameText.text = data.StatName;

            if (leftValueText != null)
                leftValueText.text = data.LeftValueText;

            if (arrowObject != null)
                arrowObject.SetActive(data.ShowArrow);

            if (rightValueText != null)
            {
                rightValueText.gameObject.SetActive(data.ShowArrow);
                rightValueText.text = data.RightValueText;
            }
        }
    }
}