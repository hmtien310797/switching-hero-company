using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.Models;
using Immortal_Switch.Scripts.Shared.Helper;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsConfigEventDatabase EventDb { get; private set; }

        [field: DatabaseBinding]
        public EventDisplayDatabaseSO EventDisplayDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelShopConfigDatabase EventWheelShopConfigDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelRewardsPoolDatabase EventWheelRewardsPoolDb { get; private set; }

        [field: DatabaseBinding]
        public DynamicHeroesGlobalSpecificationsEventWheelPassConfigDatabase EventWheelPassConfigDb { get; private set; }

        public List<DynamicHeroesGlobalSpecificationsConfigEventRow> GetEventActives()
        {
            return EventDb.rows
                .Where(v => /*DateTimeHelper.InTime(DateTime.Now, v.startTime, v.endTime) &&*/ v.status == "Active")
                .ToList();
        }
    }
}