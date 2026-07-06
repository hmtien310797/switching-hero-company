using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
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