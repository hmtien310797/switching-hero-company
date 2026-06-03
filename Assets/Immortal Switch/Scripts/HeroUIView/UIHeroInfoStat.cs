using System.Globalization;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class UIHeroInfoStat : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TMP_Text txtValue;

        public void Bind(float value)
        {
            txtValue.text = value.ToString(CultureInfo.InvariantCulture);
        }
    }
}