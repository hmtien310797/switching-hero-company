using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Băng power của mock opponent (DOCX §30 — "lower, equal, and higher power bands").
    /// </summary>
    public enum MockOpponentPowerBand
    {
        Lower = 0,
        Equal = 1,
        Higher = 2
    }

    /// <summary>
    /// Save shape tối giản cho 1 mock opponent. Progression đầy đủ (Star/Tier/Equipment/Skills/
    /// Growth/Transmutation/buffs/FinalStats) do <c>MockPvpOpponentGenerator</c> (M4) sinh và lưu
    /// dạng JSON string — qua đúng pipeline snapshot với player (DOCX §30). M1 chỉ seed skeleton.
    /// <b>FLAGGED (ambiguity):</b> DOCX không định nghĩa field đầy đủ cho MockPvPOpponentProfile;
    /// các field dưới đây suy luận từ <c>MockPvPOpponentProfile</c> (DOCX §30) + structure local.
    /// </summary>
    [Serializable]
    public sealed class PvpMockOpponentSave
    {
        public string OpponentId;
        public string DisplayName;
        public int RankPoint;
        public long TeamPower;
        public MockOpponentPowerBand PowerBand;

        public int FrontHeroId = -1;
        public int BackHeroId = -1;
        public PvpBuffSlotLoadout FrontLoadout = new();
        public PvpBuffSlotLoadout BackLoadout = new();

        /// <summary>
        /// HeroBattleSnapshot[] (Front/Back) dạng JSON — typed shape introduced in M3.
        /// Stable seed derived from OpponentId (DOCX §30) — generator đảm bảo repeatable.
        /// </summary>
        public string ProgressionJson;
        public int DataVersion = 1;
    }

    /// <summary>
    /// Kho mock opponent local. Key <see cref="PvpEs3Keys.MockOpponents"/> (DOCX §6).
    /// M1 seed rỗng; M4 generator populate lazy khi Find Match nếu list rỗng.
    /// </summary>
    [Serializable]
    public sealed class PvpMockOpponentData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public List<PvpMockOpponentSave> Opponents = new();
    }
}
