using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Shared.Constants;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class ShopSpecialRuntimeData
    {
        public DynamicHeroesGlobalSpecificationsPackIapRow Pack;
        public DynamicHeroesGlobalSpecificationsProductIdRow Product;
    }

    public class UIShopSpecialLayout : MonoBehaviour
    {
        [SerializeField]
        private UIShopPackSpecial packSpecial;

        [SerializeField]
        private Transform productContainer;

        [SerializeField]
        private UIShopProductLimit productPrefab;

        // --- Private Fields ---
        private List<UIShopProductLimit> _productsItem = new();
        private Action<string, int> _onClickBuy;

        public void Bind(List<ShopSpecialRuntimeData> rows, Action<string, int> onClickBuy)
        {
            _onClickBuy = onClickBuy;
            RefreshProducts(rows);
        }

        private void RefreshProducts(List<ShopSpecialRuntimeData> rows)
        {
            ShopSpecialRuntimeData packWeekly = null;
            ShopSpecialRuntimeData packDaily = null;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                switch (row.Pack.iD)
                {
                    // id 13: gói weekly, 14: daily
                    case PackIdConstants.ID_WEEKLY_SPECIAL:
                        packWeekly = row;
                        continue;

                    case PackIdConstants.ID_DAILY_SPECIAL:
                        packDaily = row;
                        continue;
                }

                var price = row.Product.price.ToString(CultureInfo.InvariantCulture);

                if (_productsItem.Count > i)
                {
                    var clone = _productsItem[i];
                    clone.gameObject.SetActive(true);
                    clone.Bind(row.Pack.nameVi, price, row.Product, row.Pack, _onClickBuy);
                }
                else
                {
                    var clone = Instantiate(productPrefab, productContainer);
                    clone.Bind(row.Pack.nameVi, price, row.Product, row.Pack, _onClickBuy);
                    _productsItem.Add(clone);
                }
            }

            if (packWeekly != null &&
                packDaily != null)
            {
                packSpecial.Bind(packWeekly, packDaily, _onClickBuy);
            }
        }
    }
}