using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonProbabilityItemUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rateText;

        public void Bind(SkillSummonProbabilityData data)
        {
            if (data == null)
                return;

            if (icon != null)
                icon.sprite = data.Icon;

            if (nameText != null)
                nameText.text = data.Name;

            if (rateText != null)
                rateText.text = $"{data.Rate:0.##}%";
        }
    }
}