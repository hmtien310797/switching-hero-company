using System.Linq;
using Game.Configs.Generated;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsConfigLeaderboardRewardDatabase _leaderboardRewardDb;

        public DynamicHeroesGlobalSpecificationsConfigLeaderboardRewardRow GetLeaderboardRewardByRank(int rank)
        {
            return _leaderboardRewardDb.rows.FirstOrDefault(v => rank >= v.rankFrom && rank <= v.rankTo);
        }
    }
}