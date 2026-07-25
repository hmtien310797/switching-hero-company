using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsConfigStatsInfoDatabase _configStatInfo;

        public DynamicHeroesGlobalSpecificationsConfigStatsInfoRow GetConfigStats(StatType type)
        {
            return _configStatInfo.rows.FirstOrDefault(v => v.statType == (int)type);
        }

        public List<DynamicHeroesGlobalSpecificationsConfigStatsInfoRow> GetConfigStats()
        {
            return _configStatInfo.rows;
        }
    }
}