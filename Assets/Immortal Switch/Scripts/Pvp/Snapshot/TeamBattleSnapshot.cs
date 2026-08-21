using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Pvp.Snapshot
{
    /// <summary>Team 2 hero (Front/Back) + team power (DOCX §10).</summary>
    [Serializable]
    public sealed class TeamBattleSnapshot
    {
        public HeroBattleSnapshot FrontHero;
        public HeroBattleSnapshot BackHero;
        public long TeamPower;

        /// <summary>Account-wide Growth/Transmutation bonus của đối thủ THẬT (server ghép được qua
        /// findRealOpponent — xem handler/pvp.js) — null với mọi team khác (attacker luôn giữ nguyên
        /// bonus sống qua ResetData/PowerUpManager, mock/dev-tool opponent không có data này). Áp lên
        /// StatModule của defender ở PvpRealBattleController.ApplyOpponentGrowthAndTransmutation,
        /// KHÔNG resolve ở đây — snapshot chỉ mang dữ liệu thô.</summary>
        public GrowthSaveData OpponentGrowth;
        /// <summary>Modifier Transmutation của đối thủ thật — ĐÃ resolve sẵn (server bake stat_type/
        /// op/value lúc roll, xem TransmutationSystemHelper.ToModifiers) nên apply thẳng, không cần
        /// tính lại.</summary>
        public List<StatModifier> OpponentTransmutationModifiers;
    }
}
