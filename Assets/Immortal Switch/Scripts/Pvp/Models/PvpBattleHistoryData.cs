using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// 1 entry battle history (Markdown screen 13). SummaryJson chứa per-hero summary
    /// (PvPBattleResultRequest — typed shape introduced in M5/M8).
    /// <b>FLAGGED (ambiguity):</b> DOCX không liệt kê field entry; suy luận từ Markdown §13.
    /// </summary>
    [Serializable]
    public sealed class PvpBattleHistoryEntry
    {
        public string BattleId;
        public string Result;          // "Victory" | "Defeat" | "Draw" | "Surrender"
        public string OpponentName;
        public int RankChange;
        public long DurationMs;
        public ulong RandomSeed;
        public int BattleRulesVersion;
        public long TimestampUnix;
        public string SummaryJson;
    }

    /// <summary>
    /// History local có giới hạn retention (Markdown §13 — "Store only a configured number of
    /// records to prevent unbounded ES3 growth"). Key <see cref="PvpEs3Keys.BattleHistory"/>.
    /// </summary>
    [Serializable]
    public sealed class PvpBattleHistoryData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public int MaxRetention = PvpDefaults.MaxBattleHistoryEntries;
        public List<PvpBattleHistoryEntry> Entries = new();
    }
}
