using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.Shop.Models
{
    public class MonthlyPassData
    {
        /// <summary>Ngày mua gói.</summary>
        public DateTime PurchaseDate;

        /// <summary>Danh sách ngày đã nhận thưởng (1-based).</summary>
        public List<int> ClaimedDays = new();
    }

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

        /// <summary>
        /// Tong so lan nap (mua diamond) tu truoc den nay.
        /// </summary>
        public int TotalTopupCount;

        /// <summary>
        /// Danh sach milestoneId GloryPass da nhan thuong.
        /// </summary>
        public List<int> ClaimedGloryPassIds = new();

        /// <summary>
        /// Data monthly pass theo packId (15 = normal, 16 = premium).
        /// </summary>
        public Dictionary<int, MonthlyPassData> MonthlyPasses = new();
    }
}
