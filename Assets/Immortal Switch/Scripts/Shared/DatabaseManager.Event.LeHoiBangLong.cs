using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared.Helper;

namespace Immortal_Switch.Scripts.Shared
{
    public partial class DatabaseManager
    {
        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneDatabase _eventBLDailyGachaMilestoneDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBLDailyMissionDatabase _eventBLDailyMissionDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneDatabase _eventBLDailyMissionMilestoneDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBLCheckInDatabase _eventBLCheckInDb;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBLCheckIn2Database _eventBLCheckIn2Db;

        [DatabaseBinding]
        private DynamicHeroesGlobalSpecificationsEventBLRateDatabase _eventBLRateDb;

        public DynamicHeroesGlobalSpecificationsEventBLRateRow EventBLRandomRate()
        {
            var probabilities = _eventBLRateDb.rows.Select(v => v.rate).ToList();
            var idx = RandomHelper.RandomIndexByWeight(probabilities, v => v);
            return idx >= 0 ? _eventBLRateDb.rows[idx] : null;
        }

        public List<DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow> GetEventLHBLMilestone()
        {
            return _eventBLDailyGachaMilestoneDb.rows;
        }

        public List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow> GetEventLHBLMissions()
        {
            return _eventBLDailyMissionDb.rows;
        }

        public List<DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow> GetEventLHBLMissionMilestones()
        {
            return _eventBLDailyMissionMilestoneDb.rows;
        }

        public List<DynamicHeroesGlobalSpecificationsEventBLCheckInRow> GetEventLHBLCheckIn()
        {
            return _eventBLCheckInDb.rows;
        }

        public IList<int> GetEventLHBLCheckInEventPointReward()
        {
            return _eventBLCheckInDb.rows
                .Select(v => v.rewardId)
                .Distinct()
                .ToList();
        }

        public (
            List<ItemData> instantRewards,
            List<ItemData> bonusRewards,
            string packPrice,
            int packId,
            DynamicHeroesGlobalSpecificationsProductIdRow product
            )
            GetEventLHBLCheckInBonusRewards(int day)
        {
            var instantRewards = new List<ItemData>();
            var bonusRewards = new List<ItemData>();
            var packPrice = string.Empty;

            var checkIn = _eventBLCheckIn2Db.rows.Find(v => v.day == day);

            if (checkIn == null)
            {
                return (instantRewards, bonusRewards, packPrice, 0, null);
            }

            TryAddRewards(instantRewards, checkIn.rewardId, checkIn.quantity);
            TryAddRewards(instantRewards, checkIn.rewardId2, checkIn.quantity2);
            TryAddRewards(instantRewards, checkIn.rewardId3, checkIn.quantity3);

            var bonus = _packEventDb.rows.Find(v => v.iD == checkIn.packId);

            if (bonus == null)
            {
                return (instantRewards, bonusRewards, packPrice, checkIn.packId, null);
            }

            TryAddRewards(bonusRewards, bonus.itemId1, bonus.quantity1);
            TryAddRewards(bonusRewards, bonus.itemId2, bonus.quantity2);
            TryAddRewards(bonusRewards, bonus.itemId3, bonus.quantity3);

            var product = _productDb.rows.Find(v => v.iD == bonus.productID);

            if (product != null)
            {
                packPrice = product.price.ToString(CultureInfo.InvariantCulture);
            }

            return (instantRewards, bonusRewards, packPrice, checkIn.packId, product);

            void TryAddRewards(List<ItemData> rewards, int itemId, int quantity)
            {
                if (itemId > 0 &&
                    quantity > 0)
                {
                    rewards.Add(new ItemData(itemId, quantity));
                }
            }
        }
    }
}