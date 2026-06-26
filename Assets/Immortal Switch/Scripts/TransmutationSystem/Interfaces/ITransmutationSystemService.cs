using System.Collections.Generic;
using System.Numerics;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared.Database;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.TransmutationSystem.Interfaces
{
    public interface ITransmutationSystemService
    {
        /// <summary>
        /// add exp to transmulate
        /// </summary>
        void UpdateExp(BigInteger quantity);

        /// <summary>
        /// add energy to transmulate
        /// </summary>
        void UpdateEnergy(BigInteger quantity);

        /// <summary>
        /// tang cap
        /// </summary>
        void UpdateLevel(int level);

        /// <summary>
        /// dismantle item
        /// </summary>
        void Dismantle();

        /// <summary>
        /// roll ngau nhien ra item trong level hien tai.
        /// </summary>
        EItemTier RollTier(DynamicHeroesGlobalSpecificationsTransmutationRateConfigRow row);

        /// <summary>
        /// tao modifier item tu row cfg
        /// </summary>
        PlayerEquipItem BuildEquip(
            DynamicHeroesGlobalSpecificationsTransmuationItemConfigRow itemCfg,
            DynamicHeroesGlobalSpecificationsTransmutationRandomLevelRangeConfigRow levelRangeCfg,
            List<StatModifier> uniqueModifiers
        );

        /// <summary>
        /// tao unique modifiers
        /// </summary>
        List<StatModifier> BuildUniqueModifiers(
            IReadOnlyList<DynamicHeroesGlobalSpecificationsTransmutationItemUniqueRow> rows,
            int count
        );

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

        /// <summary>
        /// set waiting material
        /// </summary>
        void SetWaitingMaterial(bool value);

        /// <summary>
        /// toggle waiting material
        /// </summary>
        bool ToggleWaitingMaterial();

        /// <summary>
        /// luu cai dat
        /// </summary>
        void SaveSetting(List<List<string>> uniqueOptions, int count, EItemTier tier, bool enabled);
    }
}