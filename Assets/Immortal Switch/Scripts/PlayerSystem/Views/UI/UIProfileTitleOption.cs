using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.PlayerSystem.Views.UI
{
    public class UIProfileTitleOption : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private TMP_Text txtTitle;

        public void Bind(string title)
        {
            txtTitle.text = title;
        }
    }
}