using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared.Database;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.TransmutationSystem.Interfaces
{
    public interface ITransmutationSystemService
    {
        /// <summary>Apply toàn bộ state từ transmutation/list — ghi đè, không merge.</summary>
        void ApplyListResponse(TransmutationListResponse response);

        /// <summary>Apply kết quả roll từ transmutation/fuse (energy/exp/level mới + pending mới).</summary>
        void ApplyFuseResult(TransmutationFuseResponse response);

        /// <summary>Apply kết quả chốt giữ pending từ transmutation/equip.</summary>
        void ApplyEquipResult(TransmutationEquipResponse response);

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
        /// <summary>Apply kết quả huỷ pending từ transmutation/dismantle (energy refund/exp/level mới).</summary>
        void ApplyDismantleResult(TransmutationDismantleResponse response);

        /// <summary>
        /// lay ra equip hien tai theo type
        /// </summary>
        PlayerEquipItem GetEquip(string itemType);

        /// <summary>
        /// lay ra ds equip dang mac.
        /// </summary>
        IEnumerable<PlayerEquipItem> GetEquips();

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
