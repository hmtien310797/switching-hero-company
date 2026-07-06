using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopProductItem : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtQuantity;

        [SerializeField]
        private Image imgIcon;

        [SerializeField]
        private Image imgBg;

        // --- Private Fields ---

        public void Bind(string quantity)
        {
            txtQuantity.text = quantity;
        }
    }
}