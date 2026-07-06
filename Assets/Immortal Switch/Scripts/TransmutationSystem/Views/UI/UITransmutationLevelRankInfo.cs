using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationLevelRankInfo : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TextMeshProUGUI txtPercent;

        [SerializeField] private Image imgTier;

        public void Bind(Sprite tier, float percent)
        {
            txtPercent.text = $"{percent:F2}%";
            imgTier.sprite = tier;
        }
    }
}