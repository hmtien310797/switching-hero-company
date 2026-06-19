using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationTotalStatLine : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private TextMeshProUGUI txtValue;

        [Header("Colors")] [SerializeField] private Color32 colorNormal;
        [SerializeField] private Color32 colorUnique;

        public void Bind(TransmutationSystemTotalStatEntry entry)
        {
            var color = entry.IsUnique ? colorUnique : colorNormal;
            txtTitle.color = color;
            txtValue.color = color;

            txtTitle.text = entry.Title;
            txtValue.text = BigIntegerHelper.Format(entry.Value);
        }
    }
}