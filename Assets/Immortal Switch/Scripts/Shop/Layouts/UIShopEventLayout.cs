using System;
using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shop.Views.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Layouts
{
    public class ShopEventRuntimeData
    {
        public DynamicHeroesGlobalSpecificationsPackEventRow Pack;
        public DynamicHeroesGlobalSpecificationsProductIdRow Product;
        public List<ItemData> Rewards;
    }

    public class UIShopEventLayout : MonoBehaviour
    {
        [SerializeField]
        private Transform productContainer;

        [SerializeField]
        private UIShopProductEvent productPrefab;

        // --- Private Fields ---
        private SimpleUIPool<UIShopProductEvent> _pools;
        private Action<string, int> _onClickBuy;

        public void Bind(List<ShopEventRuntimeData> rows, Action<string, int> onClickBuy)
        {
            _onClickBuy = onClickBuy;

            RefreshProducts(rows);
        }

        private void RefreshProducts(List<ShopEventRuntimeData> rows)
        {
            _pools ??= new SimpleUIPool<UIShopProductEvent>(productPrefab, productContainer);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var clone = _pools.Get(i);
                var purchasedCount = ShopManager.Instance.GetEventPurchasedCount(row.Pack.iD);

                clone.Bind(
                    row.Pack.nameVi, row.Product.price,
                    row.Product, row.Pack.iD,
                    row.Pack.limitReset,
                    purchasedCount, row.Pack.limit,
                    DateTime.Now.AddMinutes(30),
                    _onClickBuy, row.Pack.iconId,
                    row.Rewards
                );
            }

            _pools.ReleaseFrom(rows.Count);
        }
    }
}