using System.Collections.Generic;
using Game.Configs.Generated;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventDiceMilestoneDatabase _eventDiceMilestoneDb;

        [field: DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventDiceBoardDatabase _eventDiceBoardDb;

        public List<DynamicHeroesGlobalSpecificationsEventDiceBoardRow> GetEventDiceBoard()
        {
            return _eventDiceBoardDb.rows;
        }

        public List<DynamicHeroesGlobalSpecificationsEventDiceMilestoneRow> GetEventDiceMilestone()
        {
            return _eventDiceMilestoneDb.rows;
        }
    }
}