using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Common
{
    /// <summary>
    /// Ghi target skillUid[] (theo slot) của 1 hero xuống local NGAY sau khi equip/unequip/replace/
    /// auto-equip cập nhật state cục bộ, trước khi gửi skill/equip|unequip|auto_equip lên server.
    /// Nếu app bị kill/crash/Editor-Stop hoặc user logout trước khi request kịp tới server, lần mở
    /// app/login kế tiếp sẽ đọc lại entry của từng heroUid và gửi lại qua skill/auto_equip — tránh
    /// mất skill loadout vừa đổi. Cùng pattern với PendingLineupSync, áp dụng riêng cho skill equip.
    /// Key theo userId vì nhiều tài khoản có thể đăng nhập trên cùng 1 máy (guest device switch).
    /// </summary>
    public static class PendingSkillEquipSync
    {
        private const string KeyPrefix = "pending_skill_equip_sync_";

        public static void Save(string userId, string heroUid, string[] skillUids)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(heroUid)) return;

            var map = LoadMap(userId) ?? new Dictionary<string, string[]>();
            map[heroUid] = skillUids;
            PlayerPrefs.SetString(KeyPrefix + userId, JsonConvert.SerializeObject(map));
            PlayerPrefs.Save();
        }

        public static Dictionary<string, string[]> Load(string userId)
        {
            return LoadMap(userId);
        }

        public static void Clear(string userId, string heroUid)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(heroUid)) return;

            var map = LoadMap(userId);
            if (map == null || !map.Remove(heroUid)) return;

            if (map.Count == 0)
                PlayerPrefs.DeleteKey(KeyPrefix + userId);
            else
                PlayerPrefs.SetString(KeyPrefix + userId, JsonConvert.SerializeObject(map));
            PlayerPrefs.Save();
        }

        private static Dictionary<string, string[]> LoadMap(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var json = PlayerPrefs.GetString(KeyPrefix + userId, null);
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string[]>>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
