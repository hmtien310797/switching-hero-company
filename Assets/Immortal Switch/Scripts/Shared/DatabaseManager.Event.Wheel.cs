using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventWheel.Layout;
using Immortal_Switch.Scripts.Event.Views;
using Immortal_Switch.Scripts.Shared.Helper;
using JetBrains.Annotations;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsConfigEventDatabase EventDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsConfigPassEventDatabase EventPassConfigDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelShopConfigDatabase EventWheelShopConfigDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelRewardsPoolDatabase EventWheelRewardsPoolDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelPassConfigDatabase EventWheelPassConfigDb { get; private set; }

        // --- Private Fields ---
        private Dictionary<int, DynamicHeroesGlobalSpecificationsConfigEventRow> _activeEvents = new();

        public List<DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow> GetEventShopItem(int eventId)
        {
            return EventWheelShopConfigDb.rows
                .Where(v => v.eventId == eventId)
                .OrderBy(v => v.sortOrder)
                .ToList();
        }

        public List<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> GetEventPassItem(int eventId)
        {
            return EventWheelPassConfigDb.rows
                .Where(v => v.eventId == eventId)
                .ToList();
        }

        public List<DynamicHeroesGlobalSpecificationsEventWheelRewardsPoolRow> GetEventWheelRewardsPool(EEventCategory category)
        {
            return EventWheelRewardsPoolDb.rows
                .Where(v => v.wheelId == (int)category)
                .ToList();
        }

        [CanBeNull]
        public DynamicHeroesGlobalSpecificationsConfigEventRow GetEventIfActive(int eventId)
        {
            return _activeEvents.GetValueOrDefault(eventId);
        }

        // Quyết định event nào đang active dựa trên server (event/config_windows — xem
        // handler/event_config.js), KHÔNG dùng đồng hồ máy nữa: config Addressable cục bộ
        // (EventDb) chỉ còn được dùng để lấy metadata hiển thị (nameVi/displayMode/...), là bản
        // sao độc lập có thể lệch với config thật trên server (cùng rủi ro đã ghi nhận với
        // game_item.js — xem project memory "Item config client sync"). Fallback về check cục bộ
        // cũ nếu RPC lỗi (vd. mất mạng lúc boot), để icon event không biến mất hẳn khi offline.
        public async UniTask InitEventAsync()
        {
            Dictionary<int, bool> serverActive = null;

            try
            {
                var response = await NakamaClient.Instance.GetEventConfigWindowsAsync();
                serverActive = response?.Events?.ToDictionary(v => v.EventId, v => v.IsActive);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[DatabaseManager] event/config_windows failed, fallback to local device-time check: {ex.Message}");
            }

            IEnumerable<DynamicHeroesGlobalSpecificationsConfigEventRow> list = serverActive != null
                ? EventDb.rows.Where(v => serverActive.TryGetValue(v.eventId, out var active) && active)
                : EventDb.rows.Where(v =>
                    DateTimeHelper.InTime(DateTime.Now, v.startTime, v.endTime) &&
                    v.status == 1);

            foreach (var row in list)
            {
                _activeEvents.TryAdd(row.eventId, row);
            }
        }

        public List<DynamicHeroesGlobalSpecificationsConfigEventRow> GetEventActives(EEventDisplayMode mode)
        {
            if (mode == EEventDisplayMode.All)
            {
                return _activeEvents.Select(v => v.Value).ToList();
            }

            return _activeEvents.Where(v => v.Value.displayMode == (int)mode)
                .Select(v => v.Value)
                .ToList();
        }
    }
}