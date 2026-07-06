using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonStarProbabilityRowUI : MonoBehaviour
    {
        [SerializeField] private Image[] starImages;
        [SerializeField] private TMP_Text probabilityText;

        public void Bind(int starCount, float ratePercent)
        {
            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] == null)
                        continue;

                    starImages[i].enabled = i < starCount;
                }
            }

            if (probabilityText != null)
                probabilityText.text = $"{ratePercent:0.##}%";
        }
    }
}