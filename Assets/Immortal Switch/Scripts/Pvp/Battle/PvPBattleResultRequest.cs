using System;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Battle result request (DOCX §34) — client submit cho local result service (Phase-1) / server
    /// (Phase-2). Chứa BattleId, result, duration, per-hero summaries (attacker+defender), totals,
    /// CombatLogHash, versions.
    /// </summary>
    [Serializable]
    public sealed class PvPBattleResultRequest
    {
        public string BattleId;
        public PvPBattleResult Result;
        public long DurationMs;

        public HeroBattleResultSummary[] AttackerHeroes;
        public HeroBattleResultSummary[] DefenderHeroes;

        public long TotalDamageDealt;
        public long TotalHealingDone;
        public string CombatLogHash;

        public int SnapshotVersion;
        public int BattleRulesVersion;
    }
}
