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
        /// Key là ID trong bảng pack_event, value là số lần đã mua trong chu kỳ hiện tại.
        /// Tách riêng với PurchaseCount để tránh trùng ID giữa pack_event và pack_iap.
        /// </summary>
        public Dictionary<int, int> EventPurchaseCount = new();

        /// <summary>
        /// Danh sách ID gói kim cương đã mua ít nhất một lần.
        /// Mỗi ID trong danh sách đã sử dụng hết ưu đãi x2 lần mua đầu.
        /// </summary>
        public List<int> PurchasedDiamondPackIds = new();

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
