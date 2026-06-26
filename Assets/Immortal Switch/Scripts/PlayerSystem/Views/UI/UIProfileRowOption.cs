using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem.Views.UI
{
    public class UIProfileRowOption : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TMP_Text txtTitle;

        [SerializeField] private TMP_Text txtValue;

        public void Bind(string title, string value)
        {
            txtTitle.text = title;
            txtValue.text = value;
        }
    }
}