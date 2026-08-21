using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Modules.Cache.Global;

namespace Immortal_Switch.Scripts.Modules.Cache.Analytics
{
    /// <summary>
    /// Cache cluster cho dữ liệu tracking AppsFlyer theo tài khoản — kế thừa GlobalCacheModule.
    /// Mọi truy cập data đi qua GlobalCache.Get&lt;AccountAnalyticsTrackingData&gt;.
    /// </summary>
    public class AnalyticsTrackingCache : GlobalCacheModule<AccountAnalyticsTrackingData>
    {
        private static AnalyticsTrackingCache _instance;

        public static AnalyticsTrackingCache Instance => _instance ??= new AnalyticsTrackingCache();

        private AnalyticsTrackingCache()
        {
        }

        // ---------- First purchase ----------

        /// <summary>Đã mua gói bất kỳ lần đầu chưa (theo tài khoản) — gate af_purchase_first.</summary>
        public bool HasPurchasedAny(string userId)
        {
            return !string.IsNullOrEmpty(userId) && Get(userId)?.HasPurchasedAny == true;
        }

        public void MarkPurchasedAny(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            GetOrCreate(userId).HasPurchasedAny = true;
            Save();
        }

        /// <summary>Đã mua gói 1$ lần đầu chưa (theo tài khoản) — gate af_purchase_first_1u.</summary>
        public bool HasPurchasedOneUsd(string userId)
        {
            return !string.IsNullOrEmpty(userId) && Get(userId)?.HasPurchasedOneUsd == true;
        }

        public void MarkPurchasedOneUsd(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            GetOrCreate(userId).HasPurchasedOneUsd = true;
            Save();
        }

        // ---------- Tutorial ----------

        /// <summary>Đã gửi af_tutorial_begin cho guide id này chưa (theo tài khoản).</summary>
        public bool HasSentTutorialBegin(string userId, int guideId)
        {
            return !string.IsNullOrEmpty(userId) && Get(userId)?.TutorialBeginSentGuideIds.Contains(guideId) == true;
        }

        public void MarkTutorialBeginSent(string userId, int guideId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            if (GetOrCreate(userId).TutorialBeginSentGuideIds.Add(guideId))
            {
                Save();
            }
        }

        // ---------- Level ----------

        /// <summary>Đã gửi af_level_achieved cho level này chưa (theo tài khoản).</summary>
        public bool HasTrackedLevel(string userId, int level)
        {
            return !string.IsNullOrEmpty(userId) && Get(userId)?.TrackedLevels.Contains(level) == true;
        }

        public void MarkLevelTracked(string userId, int level)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            if (GetOrCreate(userId).TrackedLevels.Add(level))
            {
                Save();
            }
        }
    }

    [Serializable]
    public class AccountAnalyticsTrackingData
    {
        /// <summary>Đã mua gói bất kỳ lần đầu chưa — gate af_purchase_first.</summary>
        public bool HasPurchasedAny;

        /// <summary>Đã mua gói 1$ lần đầu chưa — gate af_purchase_first_1u.</summary>
        public bool HasPurchasedOneUsd;

        /// <summary>Danh sách tutorial guide id đã gửi af_tutorial_begin — mỗi id chỉ gửi 1 lần.</summary>
        public HashSet<int> TutorialBeginSentGuideIds = new();

        /// <summary>Ngày UTC đăng nhập gần nhất — dùng tính streak 3 ngày liên tiếp cho af_login_3days.</summary>
        public DateTime LastLoginUtc;

        /// <summary>Số ngày đăng nhập liên tiếp hiện tại.</summary>
        public int LoginStreak;

        /// <summary>Danh sách level đã gửi af_level_achieved — mỗi level chỉ gửi 1 lần.</summary>
        public HashSet<int> TrackedLevels = new();
    }
}