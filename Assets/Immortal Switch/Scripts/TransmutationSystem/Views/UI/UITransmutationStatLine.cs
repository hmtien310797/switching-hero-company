using System.Numerics;
using Immortal_Switch.Scripts.Core;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.TransmutationSystem.Views.UI
{
    public class UITransmutationStatLine : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField] private TextMeshProUGUI txtValue;
        [SerializeField] private Image imgState;

        [Header("Sprites")] [PreviewField] [SerializeField]
        private Sprite sprDown;

        [PreviewField] [SerializeField] private Sprite sprUp;

        [Header("Colors")] [SerializeField] private Color32 colorNormal;
        [SerializeField] private Color32 colorUnique;

        public void Bind(bool isUnique, bool? isUp, string title, BigInteger value)
        {
            var color = isUnique ? colorUnique : colorNormal;
            txtTitle.color = color;
            txtValue.color = color;

            txtTitle.text = title;
            txtValue.text = BigNumberHelper.Format(value);

            if (isUp == null)
            {
                imgState.gameObject.SetActive(false);
            }
            else
            {
                imgState.gameObject.SetActive(true);
                imgState.sprite = isUp.Value ? sprUp : sprDown;
            }
        }
    }
}