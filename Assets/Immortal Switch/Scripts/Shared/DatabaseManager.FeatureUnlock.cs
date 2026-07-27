using System.Linq;
using Game.Configs.Generated;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsFeatureUnlockConfigDatabase _featureUnlockConfigDb;

        public DynamicHeroesGlobalSpecificationsFeatureUnlockConfigRow GetFeatureUnlockConfig(EFeatureUnlockType unlockType)
        {
            return _featureUnlockConfigDb.rows.FirstOrDefault(v => v.featureId == (int)unlockType);
        }
    }
}