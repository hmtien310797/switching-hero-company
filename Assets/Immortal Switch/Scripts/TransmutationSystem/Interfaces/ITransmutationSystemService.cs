using System.Collections.Generic;
using System.Numerics;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.PlayerSystem.Models;

namespace Immortal_Switch.Scripts.TransmutationSystem.Interfaces
{
    public interface ITransmutationSystemService
    {
        /// <summary>
        /// add exp to transmulate
        /// </summary>
        void AddExp(BigInteger quantity);

        /// <summary>
        /// add energy to transmulate
        /// </summary>
        void AddEnergy(BigInteger quantity);

        /// <summary>
        /// tang cap
        /// </summary>
        void LevelUp(int totalLevel);

        /// <summary>
        /// roll ngau nhien ra item trong level hien tai.
        /// </summary>
        string RollTier(DynamicHeroesGlobalSpecificationsTransmutationRateConfigRow row);

        /// <summary>
        /// tao modifier item tu row cfg
        /// </summary>
        PlayerEquipItem BuildEquip(DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow itemCfg,
            DynamicHeroesGlobalSpecificationsTransmutationRandomLevelRangeConfigRow levelRangeCfg);

        /// <summary>
        /// lay ra equip hien tai theo type
        /// </summary>
        PlayerEquipItem GetEquip(string itemType);

        /// <summary>
        /// lay ra ds equip dang mac.
        /// </summary>
        IEnumerable<PlayerEquipItem> GetEquips();

        /// <summary>
        /// trang bi item moi
        /// </summary>
        void Equip(PlayerEquipItem newEquip);
    }
}