using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Shop.Models
{
    public class ShopData
    {
        /// <summary>
        /// Key: packIapId (DynamicHeroesGlobalSpecificationsPackIapRow.iD)
        /// Value: so lan da mua
        /// </summary>
        public Dictionary<int, int> PurchaseCount = new();

        /// <summary>
        /// Ngay reset cuoi cung cho daily limit.
        /// </summary>
        public DateTime? DailyResetDate;

        /// <summary>
        /// Ngay reset cuoi cung cho weekly limit.
        /// </summary>
        public DateTime? WeeklyResetDate;

        /// <summary>
        /// Ngay reset cuoi cung cho monthly limit.
        /// </summary>
        public DateTime? MonthlyResetDate;
    }
}
