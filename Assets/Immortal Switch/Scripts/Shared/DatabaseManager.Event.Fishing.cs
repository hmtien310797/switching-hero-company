using System.Collections.Generic;
using Game.Configs.Generated;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventFishingCollectionDatabase _eventFishingCollectionDb;

        [field: DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventFishingShopDatabase _eventFishingShopDb;

        public List<DynamicHeroesGlobalSpecificationsEventFishingCollectionRow> GetEventFishingCollection()
        {
            return _eventFishingCollectionDb.rows;
        }

        public List<DynamicHeroesGlobalSpecificationsEventFishingShopRow> GetEventFishingShop()
        {
            return _eventFishingShopDb.rows;
        }
    }
}