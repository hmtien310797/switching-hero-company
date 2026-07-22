using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shop.Views.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Layouts
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
        private SimpleUIPool<UIShopProductDiamond> _pools;
        private Action<string, int> _onClickBuy;
        private List<ShopTopupRuntimeData> _rows;

        private void OnEnable()
        {
            ShopManager.Instance.OnDataChanged += OnShopDataChanged;
        }

        private void OnDisable()
        {
            ShopManager.Instance.OnDataChanged -= OnShopDataChanged;
        }

        private void OnShopDataChanged()
        {
            if (_rows != null)
            {
                RefreshProducts(_rows);
            }
        }

        public void Bind(List<ShopTopupRuntimeData> rows, Action<string, int> onClickBuy)
        {
            _rows = rows;
            _onClickBuy = onClickBuy;

            RefreshProducts(rows);
        }

        private void RefreshProducts(List<ShopTopupRuntimeData> rows)
        {
            _pools ??= new SimpleUIPool<UIShopProductDiamond>(productPrefab, productContainer);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                var hasFirstBuyMultiplier =
                    row.Pack.firstBuy > 0 &&
                    ShopManager.Instance.IsDiamondFirstPurchaseAvailable(row.Pack.iD);

                var price = row.Product.price.ToString(CultureInfo.InvariantCulture);
                var clone = _pools.Get(i);

                clone.Bind(
                    row.Pack.nameVi, price,
                    row.Product, row.Pack.iD, row.Pack.quantity,
                    hasFirstBuyMultiplier, _onClickBuy,
                    row.Pack.iconId
                );
            }

            _pools.ReleaseFrom(rows.Count);
        }
    }
}