using System;
using Game.Configs.Generated;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopProductLimit : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI txtTitle;

        [SerializeField]
        private TextMeshProUGUI txtPrice;

        [SerializeField]
        private TextMeshProUGUI txtBonus;

        [SerializeField]
        private TextMeshProUGUI txtLimit;

        [SerializeField]
        private Button btnBuy;

        [Header("Product references")]
        [SerializeField]
        private RectTransform productContainer;

        [SerializeField]
        private UIShopProductItem productPrefab;

        // --- Private Fields ---
        private Action<string, int> _onClickBuy;
        private DynamicHeroesGlobalSpecificationsProductIdRow _product;
        private DynamicHeroesGlobalSpecificationsPackIapRow _iap;

        private void Awake()
        {
            btnBuy.onClick.AddListener(OnClickBuy);
        }

        private void OnDestroy()
        {
            btnBuy.onClick.RemoveListener(OnClickBuy);
            ShopManager.Instance.OnDataChanged -= OnPurchaseDataChanged;
        }

        private void OnEnable()
        {
            ShopManager.Instance.OnDataChanged += OnPurchaseDataChanged;
        }

        private void OnDisable()
        {
            ShopManager.Instance.OnDataChanged -= OnPurchaseDataChanged;
        }

        private void OnPurchaseDataChanged()
        {
            if (_iap != null)
            {
                RefreshLimit();
            }
        }

        private void OnClickBuy()
        {
            if (_product == null)
                return;

#if UNITY_IOS
            string storeProductId = _product.appleID;
#elif UNITY_ANDROID
            string storeProductId = _product.googleID;
#else
            string storeProductId = _product.googleID;
#endif

            _onClickBuy?.Invoke(storeProductId, _iap.iD);
        }

        public void Bind(string title, string price, DynamicHeroesGlobalSpecificationsProductIdRow product,
            DynamicHeroesGlobalSpecificationsPackIapRow iap, Action<string, int> onClickBuy)
        {
            txtPrice.text = price;
            txtTitle.text = title;

            _onClickBuy = onClickBuy;
            _product = product;
            _iap = iap;

            RefreshLimit();
            RefreshBonus();
        }

        private void RefreshLimit()
        {
            var remaining = ShopManager.Instance.GetRemaining(_iap.iD, _iap.limit);
            var purchased = ShopManager.Instance.GetPurchasedCount(_iap.iD);

            txtLimit.text = $"Giới Hạn ({purchased}/{_iap.limit})";
            btnBuy.interactable = remaining > 0;
        }

        private void RefreshBonus()
        {
            txtBonus.text = $"Giá trị <size=30>{_iap.bonus}</size>";
        }
    }
}