using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Items.Models;

namespace Immortal_Switch.Scripts.Event.EventWheel.Models
{
    [Serializable]
    public class EventWheelPassProgressData
    {
        public int PurchasedSpinCount;
        public bool PremiumPurchased;
        public List<int> ClaimedFreeMilestoneIds = new();
        public List<int> ClaimedPremiumMilestoneIds = new();

        /// <summary>Số lần đã mua của từng ô shop, không reset trong suốt sự kiện.</summary>
        public Dictionary<int, int> AccountShopPurchaseCounts = new();

        /// <summary>Số lần đã mua trong ngày của từng ô shop.</summary>
        public Dictionary<int, int> DailyShopPurchaseCounts = new();

        /// <summary>Thời điểm gần nhất dữ liệu giới hạn mua hằng ngày được reset.</summary>
        public DateTime? DailyShopResetDate;
    }

    [Serializable]
    public class EventWheelPassData
    {
        public Dictionary<int, EventWheelPassProgressData> Events = new();
    }

    public class EventWheelPassClaimResult
    {
        public bool Changed;
        public List<ItemData> Rewards = new();
    }
}
