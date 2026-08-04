using System;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ModuleManager = Immortal_Switch.Scripts.Modules.ModuleManager;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopProductDiamond : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtSubtitle;

        [SerializeField]
        private TextMeshProUGUI txtPrice;

        [SerializeField]
        private TextMeshProUGUI txtBaseValue;

        [SerializeField]
        private Image imgIcon;

        [SerializeField]
        private GameObject goBadge;

        [SerializeField]
        private TextMeshProUGUI txtFirstTopupMultiplierQuantity;

        [SerializeField]
        private Button btnBuy;

        // --- Private Fields ---
        private Action<string, int> _onClickBuy;
        private DynamicHeroesGlobalSpecificationsProductIdRow _product;
        private int _packId;

        private void Awake()
        {
            btnBuy.onClick.AddListener(OnClickBuy);
        }

        private void OnDestroy()
        {
            btnBuy.onClick.RemoveListener(OnClickBuy);
        }

        private void OnClickBuy()
        {
            if (_product == null)
            {
                return;
            }

#if UNITY_IOS
            string storeProductId = _product.appleID;
#elif UNITY_ANDROID
            string storeProductId = _product.googleID;
#else
            string storeProductId = _product.googleID;
#endif

            _onClickBuy?.Invoke(storeProductId, _packId);
        }

        public void Bind(
            string title, string price,
            DynamicHeroesGlobalSpecificationsProductIdRow product,
            int packId,
            int baseValue, bool hasFirstBuyMultiplier,
            Action<string, int> onClickBuy, string iconId
        )
        {
            goBadge.SetActive(hasFirstBuyMultiplier);

            txtFirstTopupMultiplierQuantity.text = baseValue.ToString();
            txtBaseValue.text = baseValue.ToString();
            txtPrice.text = price;
            txtTitle.text = LocalizationManager.GetText(title);

            _onClickBuy = onClickBuy;
            _product = product;
            _packId = packId;

            var icon = ModuleManager.ShopAtlas.LoadIcon(iconId);

            if (icon != null)
            {
                imgIcon.sprite = icon;
            }
        }
    }
}