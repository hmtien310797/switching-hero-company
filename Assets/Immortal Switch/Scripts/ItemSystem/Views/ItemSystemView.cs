using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.ItemSystem.Views
{
    public class ItemSystemView : MonoBehaviour
    {
        [Header("Item view")] [SerializeField] private Image imgTier;
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgBorder;
        [SerializeField] private Image imgTheme;
        [SerializeField] private TMP_Text txtLevel;

        [Header("Item status")] [SerializeField]
        private GameObject goEquipped;

        [SerializeField] private GameObject goLocked;
        [SerializeField] private GameObject goSelected;

        public void Bind(string level, string itemType, bool isEquipped, bool isLocked, bool isSelected)
        {
        }
    }
}