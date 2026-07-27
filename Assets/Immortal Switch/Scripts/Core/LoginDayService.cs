using System;
using UnityEngine;

namespace Immortal_Switch.Scripts.Core
{
    /// <summary>
    /// Kiểm tra ngày đăng nhập mới độc lập với hệ thống dữ liệu người chơi.
    /// Ngày được tính theo UTC để thống nhất với các cơ chế daily reset hiện tại.
    /// </summary>
    public static class LoginDayService
    {
        private const string LAST_LOGIN_UTC_KEY = "LoginDayService.LastLoginUtc";
        private static bool _isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Cho phép service bắt đầu kiểm tra sau khi các listener trong bootstrap đã được đăng ký.
        /// </summary>
        public static bool Initialize()
        {
            _isInitialized = true;
            return CheckAndNotify();
        }

        /// <summary>
        /// Kiểm tra lần đăng nhập hiện tại có thuộc ngày UTC mới hay không.
        /// Khi sang ngày mới, lưu ngày hiện tại trước rồi phát GameEvents.OnLoginNewDay.
        /// </summary>
        /// <returns>True nếu đã phát event ngày mới; ngược lại là false.</returns>
        public static bool CheckAndNotify()
        {
            if (!_isInitialized)
            {
                return false;
            }

            var utcNow = DateTime.UtcNow;
            var today = utcNow.Date;

            var lastLoginUtc = ES3.KeyExists(LAST_LOGIN_UTC_KEY)
                ? ES3.Load<DateTime>(LAST_LOGIN_UTC_KEY)
                : (DateTime?)null;

            if (lastLoginUtc.HasValue &&
                lastLoginUtc.Value.Date >= today)
            {
                return false;
            }

            // Lưu trước khi dispatch để một listener lỗi cũng không làm event bị phát lặp.
            ES3.Save(LAST_LOGIN_UTC_KEY, utcNow);

            Debug.Log($"[LoginDayService] Login new UTC day: {today:yyyy-MM-dd}");
            GameEventManager.Trigger(GameEvents.OnLoginNewDay);
            return true;
        }
    }
}