using System;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Trạng thái battle chưa resolve: BattleId + snapshot (JSON) + result (JSON).
    /// Sau khi BattleId tạo, rời/thoát trận = surrender/defeat, ticket KHÔNG hoàn
    /// (DOCX §2 "Exit rule", §15 "Battle Completion"). Key <see cref="PvpEs3Keys.PendingBattle"/>.
    /// Snapshot/result lưu JSON string để M1 không phụ thuộc typed snapshot shape (M3/M5);
    /// cũng phục vụ debug export/import JSON (DOCX §14 Markdown, §41 QA).
    /// <b>FLAGGED (ambiguity):</b> DOCX không định nghĩa field PendingBattle; suy luận từ
    /// DOCX §6 ("Unresolved BattleId, snapshot, or result") + §33 (immutable BattleSnapshot).
    /// </summary>
    [Serializable]
    public sealed class PvpPendingBattleData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;

        public string BattleId;
        public ulong RandomSeed;
        public int BattleRulesVersion = 1;
        public int SnapshotVersion = 1;
        public long CreatedAtUnix;

        public bool IsResolved;
        public bool IsSurrendered;

        /// <summary>HeroVsHeroBattleSnapshot dạng JSON — typed shape introduced in M3.</summary>
        public string SnapshotJson;
        /// <summary>PvPBattleResultRequest dạng JSON — typed shape introduced in M5.</summary>
        public string ResultJson;
    }
}
