using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.Shop.Views;
using Immortal_Switch.Scripts.Shop.Views.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop.Layouts
{
    public class UIShopMonthlyLayout : MonoBehaviour
    {
        [SerializeField]
        private UIShopMonthlyPass normalPass;

        [SerializeField]
        private UIShopMonthlyPass premiumPass;

        public void Bind(List<ShopSpecialRuntimeData> rows, Action<string, int> onClickBuy, Action<int, EShopTab> onClickClaim)
        {
            ShopSpecialRuntimeData normal = null;
            ShopSpecialRuntimeData premium = null;

            foreach (var row in rows)
            {
                switch (row.Pack.iD)
                {
                    case PackIdConstants.ID_MONTHLY_NORMAL:
                        normal = row;
                        continue;

                    case PackIdConstants.ID_MONTHLY_PREMIUM:
                        premium = row;
                        continue;
                }

                if (normal != null &&
                    premium != null)
                {
                    break;
                }
            }

            if (normal != null)
            {
                BindPass(normalPass, normal, onClickBuy, onClickClaim);
            }

            if (premium != null)
            {
                BindPass(premiumPass, premium, onClickBuy, onClickClaim);
            }
        }

        private void BindPass(UIShopMonthlyPass view, ShopSpecialRuntimeData data,
            Action<string, int> onClickBuy, Action<int, EShopTab> onClickClaim)
        {
            var packId = data.Pack.iD;
            var rewardCurrentDay = ShopManager.Instance.GetMonthlyPassCurrentDay(packId);
            var rewardMonthly = DatabaseManager.Instance.GetPackMonthlyTotal(packId);
            var rewardDaily = DatabaseManager.Instance.GetPackMonthly(packId, rewardCurrentDay);
            var rewardInstant = DatabaseManager.Instance.GetShopSpecialRewards(packId);

            view.Bind(data.Product, data.Pack,
                onClickBuy, onClickClaim,
                rewardMonthly, rewardDaily, rewardInstant
            );
        }
    }
}