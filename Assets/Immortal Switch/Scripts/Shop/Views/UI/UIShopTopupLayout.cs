using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs.Generated;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class ShopTopupRuntimeData
    {
        public DynamicHeroesGlobalSpecificationsPackDiamondRow Pack;
        public DynamicHeroesGlobalSpecificationsProductIdRow Product;
    }

    public class UIShopTopupLayout : MonoBehaviour
    {
        [SerializeField]
        private Transform productContainer;

        [SerializeField]
        private UIShopProductDiamond productPrefab;

        // --- Private Fields ---
        private List<UIShopProductDiamond> _products = new();
        private Action<string, int> _onClickBuy;

        public void Bind(List<ShopTopupRuntimeData> rows, Action<string, int> onClickBuy)
        {
            _onClickBuy = onClickBuy;
            RefreshProducts(rows);
        }

        private void RefreshProducts(List<ShopTopupRuntimeData> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var hasFirstBuyMultiplier = row.Pack.firstBuy > 0;
                var price = row.Product.price.ToString(CultureInfo.InvariantCulture);

                if (_products.Count > i)
                {
                    var clone = _products[i];
                    clone.gameObject.SetActive(true);

                    clone.Bind(
                        row.Pack.nameVi, price,
                        row.Product, row.Pack.iD, row.Pack.quantity,
                        hasFirstBuyMultiplier, _onClickBuy,
                        row.Pack.iconId
                    );
                }
                else
                {
                    var clone = Instantiate(productPrefab, productContainer);

                    clone.Bind(
                        row.Pack.nameVi, price,
                        row.Product, row.Pack.iD, row.Pack.quantity,
                        hasFirstBuyMultiplier, _onClickBuy,
                        row.Pack.iconId
                    );

                    _products.Add(clone);
                }
            }
        }
    }
}