using System;
using System.Globalization;
using Immortal_Switch.Scripts.Shared;
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
            var weeklyRewards = DatabaseManager.Instance.GetShopSpecialRewards(weekly.Pack.iD);
            packWeekly.Bind(weekly.Pack.nameVi, weeklyPrice, weekly.Product, weekly.Pack, onClickBuy, weeklyRewards);

            var dailyPrice = daily.Product.price.ToString(CultureInfo.InvariantCulture);
            var dailyRewards = DatabaseManager.Instance.GetShopSpecialRewards(daily.Pack.iD);
            packDaily.Bind(daily.Pack.nameVi, dailyPrice, daily.Product, daily.Pack, onClickBuy, dailyRewards);
        }
    }
}