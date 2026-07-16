using Newtonsoft.Json;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// Ghi lineup vừa swap xuống local NGAY khi swap (đồng bộ, không chờ network), trước khi gửi
    /// hero/set_lineup lên server. Nếu app bị kill/crash/Editor-Stop trước khi request kịp tới
    /// server, lần mở app/login kế tiếp sẽ đọc lại entry này và gửi lại — tránh mất lineup vừa đổi.
    /// Key theo userId vì nhiều tài khoản có thể đăng nhập trên cùng 1 máy (guest device switch).
    /// </summary>
    public static class PendingLineupSync
    {
        private const string KeyPrefix = "pending_lineup_sync_";

        public static void Save(string userId, string[] lineup)
        {
            if (string.IsNullOrEmpty(userId)) return;
            PlayerPrefs.SetString(KeyPrefix + userId, JsonConvert.SerializeObject(lineup));
            PlayerPrefs.Save();
        }

        public static string[] Load(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var json = PlayerPrefs.GetString(KeyPrefix + userId, null);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<string[]>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void Clear(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            PlayerPrefs.DeleteKey(KeyPrefix + userId);
            PlayerPrefs.Save();
        }
    }
}
