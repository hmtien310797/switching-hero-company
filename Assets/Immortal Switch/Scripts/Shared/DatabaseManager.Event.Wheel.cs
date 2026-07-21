using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventWheel.Layout;
using Immortal_Switch.Scripts.Event.Models;
using Immortal_Switch.Scripts.Event.Views;
using Immortal_Switch.Scripts.Shared.Helper;
using JetBrains.Annotations;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsConfigEventDatabase EventDb { get; private set; }

        [field: DatabaseBinding]
        public EventDisplayDatabaseSO EventDisplayDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsConfigPassEventDatabase EventPassConfigDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelShopConfigDatabase EventWheelShopConfigDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelRewardsPoolDatabase EventWheelRewardsPoolDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelPassConfigDatabase EventWheelPassConfigDb { get; private set; }

        // --- Private Fields ---
        private Dictionary<int, DynamicHeroesGlobalSpecificationsConfigEventRow> _eventsActive = new();

        public DynamicHeroesGlobalSpecificationsConfigPassEventRow GetEventPassConfig(int eventId)
        {
            return EventPassConfigDb.rows.FirstOrDefault(v => v.eventId == eventId);
        }

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
            return _eventsActive.GetValueOrDefault(eventId);
        }

        public List<DynamicHeroesGlobalSpecificationsConfigEventRow> GetEventActives(EEventDisplayMode mode)
        {
            var list = EventDb.rows
                .Where(v =>
                    DateTimeHelper.InTime(DateTime.Now, v.startTime, v.endTime) &&
                    v.status == "Active" &&
                    (mode == EEventDisplayMode.All || (int)mode == v.displayMode)
                )
                .ToList();

            foreach (var row in list)
            {
                _eventsActive.TryAdd(row.eventId, row);
            }

            return list;
        }
    }
}