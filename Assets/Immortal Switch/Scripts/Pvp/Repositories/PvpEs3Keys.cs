namespace Immortal_Switch.Scripts.Pvp.Repositories
{
    /// <summary>
    /// Các ES3 key & schema-version hiện hành cho PvP (DOCX §6, §24).
    /// UI / combat KHÔNG BAO GIỜ gọi ES3 trực tiếp — chỉ <see cref="Es3PvPRepository"/> được phép.
    /// Mỗi key = 1 data group độc lập, có SchemaVersion riêng để migrate.
    /// </summary>
    public static class PvpEs3Keys
    {
        public const string PlayerData = "pvp_player_data";
        public const string Formation = "pvp_formation";
        public const string OwnedBuffs = "pvp_owned_buffs";
        public const string MockOpponents = "pvp_mock_opponents";
        public const string BattleHistory = "pvp_battle_history";
        public const string PendingBattle = "pvp_pending_battle";
        public const string BuffGachaState = "pvp_buff_gacha_state";
        public const string PendingBuffRoll = "pvp_pending_buff_roll";
        public const string BuffRollHistory = "pvp_buff_roll_history";
        public const string ProcessedTransactions = "pvp_processed_transactions";

        /// <summary>
        /// Schema version hiện hành cho MỌI data group. Bump giá trị này + thêm nhánh migrate
        /// trong Es3PvPRepository.Migrate khi đổi shape dữ liệu. v1 = Phase-1 khởi tạo.
        /// </summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Toàn bộ key PvP — dùng cho ResetAll (DOCX §6 "Reset PvP Local Data").</summary>
        public static readonly string[] All =
        {
            PlayerData, Formation, OwnedBuffs, MockOpponents, BattleHistory,
            PendingBattle, BuffGachaState, PendingBuffRoll, BuffRollHistory,
            ProcessedTransactions
        };
    }
}
