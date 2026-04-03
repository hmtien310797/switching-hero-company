using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonProbabilityItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text probabilityText;

        public void Bind(string gradeLabel, float percent)
        {
            if (gradeText != null)
                gradeText.text = gradeLabel;

            if (probabilityText != null)
                probabilityText.text = $"{percent:0.##}%";
        }
    }
}