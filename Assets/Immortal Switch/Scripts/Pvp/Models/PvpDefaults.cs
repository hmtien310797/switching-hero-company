namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Config mặc định Phase-1 cho bootstrap. Mọi giá trị balance phải tách ra SO riêng sau
    /// (DOCX §32 — "Local rank and rewards are simulation data; keep their config separate for
    /// later server replacement"). Đây chỉ là starting value local-first, không phải final balance.
    /// </summary>
    public static class PvpDefaults
    {
        public const string SeasonId = "season_01";

        public const int StartingTickets = 5;
        public const int MaxTicketsCap = 5;
        public const int TicketRecoverMinutes = 30;

        public const long StartingArenaToken = 1000;

        public const int SeasonDurationDays = 14;

        public const int MaxBattleHistoryEntries = 50;
        public const int MaxRollHistoryEntries = 30;

        // ── Battle simulation (Phase-1 local — DOCX §32: balance tách riêng sau) ──
        public const int MaxBattleDurationTicks = 100;
        public const float TickDurationSeconds = 0.5f;

        // ── Rank/reward (Phase-1 simulation data — DOCX §32: "Local rank and rewards are simulation
        //    data; keep their config separate for later server replacement") ──
        public const int VictoryRankGain = 25;
        public const int DefeatRankLoss = 18;
        public const long VictoryArenaToken = 120;
        public const long DefeatArenaToken = 20;
    }
}
