using System;
using System.Globalization;
using Game.Configs.Generated;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Views.UI
{
    public class UIShopPackSpecial : MonoBehaviour
    {
        [SerializeField]
        private UIShopProductLimit packWeekly;

        [SerializeField]
        private UIShopProductLimit packDaily;

        public void Bind(ShopSpecialRuntimeData weekly, ShopSpecialRuntimeData daily, Action<string, int> onClickBuy)
        {
            var weeklyPrice = weekly.Product.price.ToString(CultureInfo.InvariantCulture);
            packWeekly.Bind(weekly.Pack.nameVi, weeklyPrice, weekly.Product, weekly.Pack, onClickBuy);

            var dailyPrice = daily.Product.price.ToString(CultureInfo.InvariantCulture);
            packDaily.Bind(daily.Pack.nameVi, dailyPrice, daily.Product, daily.Pack, onClickBuy);
        }
    }
}