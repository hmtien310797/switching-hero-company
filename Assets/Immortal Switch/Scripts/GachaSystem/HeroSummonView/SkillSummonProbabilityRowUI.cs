using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonProbabilityRowUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text probabilityText;

        public void Bind(float ratePercent)
        {
            if (probabilityText != null)
                probabilityText.text = $"{ratePercent:0.##}%";
        }
    }
}