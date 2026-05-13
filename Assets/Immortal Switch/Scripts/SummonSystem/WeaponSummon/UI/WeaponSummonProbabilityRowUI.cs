using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonProbabilityRowUI : MonoBehaviour
    {
        [SerializeField] private Image tierIcon;
        [SerializeField] private TMP_Text probabilityText;

        public void Bind(Sprite icon, float ratePercent)
        {
            if (tierIcon != null)
            {
                tierIcon.sprite = icon;
                tierIcon.enabled = icon != null;
            }

            if (probabilityText != null)
                probabilityText.text = $"{ratePercent:0.##}%";
        }
    }
}