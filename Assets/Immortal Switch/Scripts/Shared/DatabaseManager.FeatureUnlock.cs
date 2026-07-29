using System.Collections.Generic;
using System.Linq;
using Common;
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

        public DynamicHeroesGlobalSpecificationsFeatureUnlockConfigRow TryCheckFeatureUnlockConfig(
            EFeatureUnlockType unlockType,
            out bool isUnlocked
        )
        {
            var cfg = GetFeatureUnlockConfig(unlockType);

            if (cfg == null)
            {
                isUnlocked = false;
                return null;
            }

            var playerLevelInfo = GetLevelByTotalExp(UserDataCache.Instance.Exp);
            isUnlocked = playerLevelInfo.level >= cfg.requiredLevel;
            return cfg;
        }

        public List<DynamicHeroesGlobalSpecificationsFeatureUnlockConfigRow> GetFeatureUnlocks()
        {
            var playerLevelInfo = GetLevelByTotalExp(UserDataCache.Instance.Exp);
            var unlocks = new List<DynamicHeroesGlobalSpecificationsFeatureUnlockConfigRow>();

            foreach (var row in _featureUnlockConfigDb.rows)
            {
                if (playerLevelInfo.level >= row.requiredLevel)
                {
                    unlocks.Add(row);
                }
            }

            return unlocks;
        }
    }
}