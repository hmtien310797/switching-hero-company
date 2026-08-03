using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Profile PvP local: rank, Arena Ticket, Arena Token (cũng là currency gacha local), season,
    /// milestone reward đã claim. Lưu key <see cref="PvpEs3Keys.PlayerData"/> (DOCX §6, §24).
    /// Equipped position của buff KHÔNG lưu ở đây — nằm trong pvp_formation.
    /// </summary>
    [Serializable]
    public sealed class PvpPlayerData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;

        public string DisplayName = string.Empty;

        public PvpRankTier RankTier = PvpRankTier.Bronze;
        public int RankPoint;

        public int ArenaTicket = PvpDefaults.StartingTickets;
        /// <summary>Unix timestamp (giây) lần recover ticket kế tiếp; 0 = chưa có timer.</summary>
        public long ArenaTicketNextRecoverUnix;

        public long ArenaToken = PvpDefaults.StartingArenaToken;

        public string SeasonId = PvpDefaults.SeasonId;
        public long SeasonEndsAtUnix;

        public List<string> ClaimedRankMilestoneRewards = new();
        public List<string> ClaimedSeasonRewards = new();
    }
}
