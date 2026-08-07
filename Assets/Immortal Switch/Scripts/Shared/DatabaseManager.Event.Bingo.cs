using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [field: DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBingoMilestoneDatabase _eventBingoMilestoneDb;

        [field: DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBingoBoardDatabase _eventBingoBoardDb;

        [field: DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBingoLineRewardsDatabase _eventBingoLineRewardsDb;

        public int GetEventBingoRandomPoolId(int excludedPoolId = 0)
        {
            var poolIds = _eventBingoBoardDb.rows
                .Select(row => row.poolId)
                .Distinct()
                .ToList();

            if (poolIds.Count == 0)
            {
                return 1;
            }

            var availablePoolIds = poolIds
                .Where(poolId => poolId != excludedPoolId)
                .ToList();

            if (availablePoolIds.Count == 0)
            {
                availablePoolIds = poolIds;
            }

            return availablePoolIds[UnityEngine.Random.Range(0, availablePoolIds.Count)];
        }

        public List<DynamicHeroesGlobalSpecificationsEventBingoBoardRow> GetEventBingoBoard(int poolId)
        {
            return _eventBingoBoardDb.rows
                .Where(v => v.poolId == poolId)
                .ToList();
        }

        public List<DynamicHeroesGlobalSpecificationsEventBingoMilestoneRow> GetEventBingoMilestone()
        {
            return _eventBingoMilestoneDb.rows;
        }

        public List<DynamicHeroesGlobalSpecificationsEventBingoLineRewardsRow> GetEventBingoLineRewards(int poolId)
        {
            return _eventBingoLineRewardsDb.rows
                .Where(v => v.poolId == poolId)
                .ToList();
        }
    }
}