using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using Common;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Modules.Cache.Analytics;
using Immortal_Switch.Scripts.Shared;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.Modules.Analytics
{
    /// <summary>
    /// Gửi event AppsFlyer. Mọi event đi qua LogEvent — nếu SDK chưa init thì bỏ qua (chỉ warning)
    /// </summary>
    public static class AppsflyerService
    {
        /// <summary>af_login</summary>
        private const string AF_LOGIN = "af_login";

        /// <summary>af_complete_registration</summary>
        private const string AF_COMPLETE_REGISTRATION = "af_complete_registration";

        /// <summary>af_login_3days</summary>
        private const string AF_LOGIN_3_DAYS = "af_login_3days";

        /// <summary>af_tutorial_completion</summary>
        private const string AF_TUTORIAL_COMPLETION = "af_tutorial_completion";

        /// <summary>af_tutorial_begin</summary>
        private const string AF_TUTORIAL_BEGIN = "af_tutorial_begin";

        /// <summary>af_level_achieved</summary>
        private const string AF_LEVEL_ACHIEVED = "af_level_achieved";

        /// <summary>af_purchase</summary>
        private const string AF_PURCHASE = "af_purchase";

        /// <summary>af_purchase_first</summary>
        private const string AF_PURCHASE_FIRST = "af_purchase_first";

        /// <summary>af_purchase_first_1u</summary>
        private const string AF_PURCHASE_FIRST_1_U = "af_purchase_first_1u";

        // ---------- Init ----------

        public static void Init()
        {
            TrackingLogin();
            GameEventManager.Subscribe(GameEvents.OnLoginNewDay, OnLoginNewDay);
            GameEventManager.Subscribe(GameEvents.OnAppResumed, OnAppResumed);
        }

        private static void OnLoginNewDay()
        {
            RegisterLogin(NakamaClient.Instance?.Session?.UserId);
            SubscribeToExpChanged();
        }

        // OnAppResumed fire mỗi lần chạy app (không phụ thuộc có phải ngày mới hay không) — bảo
        // đảm subscribe OnExpChanged ngay cả khi mở lại app trong cùng ngày để không bỏ sót level-up.
        private static void OnAppResumed()
        {
            SubscribeToExpChanged();
        }

        /// <summary>
        /// Subscribe OnExpChanged của UserDataCache để tự gửi af_level_achieved mỗi khi exp tăng.
        /// Idempotent: unsubscribe trước rồi subscribe lại, tránh double-subscribe.
        /// </summary>
        private static void SubscribeToExpChanged()
        {
            if (UserDataCache.Instance == null)
            {
                return;
            }

            UserDataCache.Instance.OnExpChanged -= OnExpChangedHandler;
            UserDataCache.Instance.OnExpChanged += OnExpChangedHandler;
        }

        private static void OnExpChangedHandler()
        {
            TrackingLevelAchieved();
        }

        /// <summary>
        /// Lưu thông tin đăng nhập mới và tính streak ngày liên tiếp. Khi streak chạm 3 ngày liên
        /// tiếp thì gửi af_login_3days. Bỏ lỡ ≥ 1 ngày sẽ reset streak về 1.
        /// </summary>
        public static void RegisterLogin(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var account = AnalyticsTrackingCache.Instance.GetOrCreate(userId);
            var today = DateTime.UtcNow.Date;

            if (account.LastLoginUtc == default)
            {
                account.LoginStreak = 1;
            }
            else
            {
                var daysDiff = (today - account.LastLoginUtc.Date).Days;
                account.LoginStreak = daysDiff == 1 ? account.LoginStreak + 1 : 1;
            }

            account.LastLoginUtc = DateTime.UtcNow;

            AnalyticsTrackingCache.Instance.Save();

            if (account.LoginStreak == 3)
            {
                TrackingLoginThreeDays();
            }
        }

        // ---------- Tracking: Login / Registration ----------

        public static void TrackingLogin(string customerUserId = null)
        {
            var dict = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(customerUserId))
            {
                dict[AFInAppEvents.CUSTOMER_USER_ID] = customerUserId;
            }

            LogEvent(AF_LOGIN, dict);
        }

        public static void TrackingCompleteRegistration(string registrationMethod = "ID")
        {
            LogEvent(AF_COMPLETE_REGISTRATION, new Dictionary<string, string>
            {
                [AFInAppEvents.REGSITRATION_METHOD] = registrationMethod,
            });
        }

        public static void TrackingLoginThreeDays()
        {
            LogEvent(AF_LOGIN_3_DAYS);
        }

        // ---------- Tracking: Tutorial ----------

        public static void TrackingTutorialBegin(int tutorialGuideId)
        {
            var userId = NakamaClient.Instance?.Session?.UserId;

            if (!string.IsNullOrEmpty(userId) &&
                AnalyticsTrackingCache.Instance.HasSentTutorialBegin(userId, tutorialGuideId))
            {
                Debug.Log($"[AppsflyerService] Bỏ qua af_tutorial_begin (đã gửi rồi): guide={tutorialGuideId}");
                return;
            }

            var sent = LogEvent(AF_TUTORIAL_BEGIN, new Dictionary<string, string>
            {
                [AFInAppEvents.CONTENT_ID] = tutorialGuideId.ToString(),
            });

            if (sent && !string.IsNullOrEmpty(userId))
            {
                AnalyticsTrackingCache.Instance.MarkTutorialBeginSent(userId, tutorialGuideId);
            }
        }

        public static void TrackingTutorialCompletion(int tutorialGuideId)
        {
            LogEvent(AF_TUTORIAL_COMPLETION, new Dictionary<string, string>
            {
                [AFInAppEvents.CONTENT_ID] = tutorialGuideId.ToString(),
            });
        }

        // ---------- Tracking: Level ----------

        public static void TrackingLevelAchieved()
        {
            var exp = UserDataCache.Instance?.Exp ?? 0;
            var level = DatabaseManager.Instance?.GetLevelByTotalExp(exp).level ?? 1;

            if (level is 3 or 5 or 7)
            {
                var userId = NakamaClient.Instance?.Session?.UserId;

                if (!string.IsNullOrEmpty(userId) &&
                    AnalyticsTrackingCache.Instance.HasTrackedLevel(userId, level))
                {
                    Debug.Log($"[AppsflyerService] Bỏ qua af_level_achieved (đã gửi rồi): level={level}");
                    return;
                }

                var sent = LogEvent(AF_LEVEL_ACHIEVED, new Dictionary<string, string>
                {
                    [AFInAppEvents.LEVEL] = level.ToString(),
                });

                if (sent && !string.IsNullOrEmpty(userId))
                {
                    AnalyticsTrackingCache.Instance.MarkLevelTracked(userId, level);
                }
            }
        }

        // ---------- Tracking: Purchase ----------

        public static void TrackingPurchase(
            string contentId,
            float priceUsd,
            string currency = "USD",
            string receiptId = null,
            int quantity = 1)
        {
            LogEvent(AF_PURCHASE, BuildPurchaseParams(contentId, priceUsd, currency, receiptId, quantity));
        }

        public static void TrackingPurchase(
            DynamicHeroesGlobalSpecificationsProductIdRow product,
            string receiptId = null,
            int quantity = 1)
        {
            TrackingPurchase(product.iD.ToString(), product.price, "USD", receiptId, quantity);
        }

        public static void TrackingPurchaseFirst(
            string contentId,
            float priceUsd,
            string currency = "USD",
            string receiptId = null,
            int quantity = 1)
        {
            LogEvent(AF_PURCHASE_FIRST, BuildPurchaseParams(contentId, priceUsd, currency, receiptId, quantity));
        }

        public static void TrackingPurchaseFirst(
            DynamicHeroesGlobalSpecificationsProductIdRow product,
            string receiptId = null,
            int quantity = 1)
        {
            TrackingPurchaseFirst(product.iD.ToString(), product.price, "USD", receiptId, quantity);
        }

        public static void TrackingPurchaseFirst1u(
            string contentId,
            float priceUsd,
            string currency = "USD",
            string receiptId = null,
            int quantity = 1)
        {
            LogEvent(AF_PURCHASE_FIRST_1_U, BuildPurchaseParams(contentId, priceUsd, currency, receiptId, quantity));
        }

        public static void TrackingPurchaseFirst1u(
            DynamicHeroesGlobalSpecificationsProductIdRow product,
            string receiptId = null,
            int quantity = 1)
        {
            TrackingPurchaseFirst1u(product.iD.ToString(), product.price, "USD", receiptId, quantity);
        }

        private static Dictionary<string, string> BuildPurchaseParams(
            string contentId,
            float priceUsd,
            string currency,
            string receiptId,
            int quantity)
        {
            var dict = new Dictionary<string, string>
            {
                [AFInAppEvents.CONTENT_ID] = contentId,
                [AFInAppEvents.REVENUE] = priceUsd.ToString("0.00"),
                [AFInAppEvents.CURRENCY] = currency,
                [AFInAppEvents.QUANTITY] = quantity.ToString(),
            };

            if (!string.IsNullOrEmpty(receiptId))
            {
                dict[AFInAppEvents.RECEIPT_ID] = receiptId;
            }

            return dict;
        }

        private static bool LogEvent(string eventName, Dictionary<string, string> eventValues = null)
        {
            if (!IsInitialized())
            {
                Debug.LogWarning($"[AppsflyerService] AppsFlyer chưa init, bỏ qua event: {eventName}");
                return false;
            }

            AppsFlyer.sendEvent(eventName, eventValues);
            Debug.Log($"[AppsflyerService] Sent event: {eventName} values: {JsonConvert.SerializeObject(eventValues)}");
            return true;
        }

        private static bool IsInitialized()
        {
            return AppsFlyer.instance != null && AppsFlyer.instance.isInit;
        }
    }
}