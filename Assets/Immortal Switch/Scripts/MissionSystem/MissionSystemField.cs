namespace Immortal_Switch.Scripts.MissionSystem
{
    /// <summary>
    /// Các sự kiện được dùng để theo dõi tiến trình nhiệm vụ, thành tựu và battle pass.
    /// </summary>
    public static class MissionSystemEventKeys
    {
        /// <summary>
        /// Hoàn thành một màn chơi.
        /// </summary>
        public const string EVENT_CLEAR_STAGE = "CLEAR_STAGE";

        /// <summary>
        /// Triệu hồi tướng.
        /// </summary>
        public const string EVENT_HERO_SUMMON = "HERO_SUMMON";

        /// <summary>
        /// Trang bị vật phẩm.
        /// </summary>
        public const string EVENT_EQUIP_ITEM = "EQUIP_ITEM";

        /// <summary>
        /// Cường hóa trang bị.
        /// </summary>
        public const string EVENT_ENHANCE_GEAR = "ENHANCE_GEAR";

        /// <summary>
        /// Nâng cấp cấp độ tướng.
        /// </summary>
        public const string EVENT_HERO_LEVELUP = "HERO_LEVELUP";

        /// <summary>
        /// Chiến thắng đấu trường.
        /// </summary>
        public const string EVENT_ARENA_WIN = "ARENA_WIN";

        /// <summary>
        /// Tham gia một trận đấu trường.
        /// </summary>
        public const string EVENT_ARENA_FIGHT = "ARENA_FIGHT";

        /// <summary>
        /// Hoàn thành phó bản.
        /// </summary>
        public const string EVENT_DUNGEON_CLEAR = "DUNGEON_CLEAR";

        /// <summary>
        /// Nâng cấp kỹ năng.
        /// </summary>
        public const string EVENT_SKILL_UPGRADE = "SKILL_UPGRADE";

        /// <summary>
        /// Nhận phần thưởng treo máy.
        /// </summary>
        public const string EVENT_CLAIM_IDLE = "CLAIM_IDLE";

        /// <summary>
        /// Đăng nhập vào trò chơi.
        /// </summary>
        public const string EVENT_LOGIN = "LOGIN";

        /// <summary>
        /// Tiêu diệt quái thường.
        /// </summary>
        public const string EVENT_KILL_MONSTER = "KILL_MONSTER";

        /// <summary>
        /// Tiêu diệt boss.
        /// </summary>
        public const string EVENT_KILL_BOSS = "KILL_BOSS";

        /// <summary>
        /// Hoàn thành nhiệm vụ ngày.
        /// </summary>
        public const string EVENT_COMPLETE_DAILY = "COMPLETE_DAILY";

        /// <summary>
        /// Sở hữu số lượng tướng yêu cầu.
        /// </summary>
        public const string EVENT_OWN_HERO = "OWN_HERO";

        /// <summary>
        /// Đạt mốc lực chiến yêu cầu.
        /// </summary>
        public const string EVENT_REACH_POWER = "REACH_POWER";

        /// <summary>
        /// Đạt mốc cấp độ yêu cầu.
        /// </summary>
        public const string EVENT_REACH_LEVEL = "REACH_LEVEL";

        /// <summary>
        /// Rèn trang bị.
        /// </summary>
        public const string EVENT_FORGE_GEAR = "FORGE_GEAR";

        /// <summary>
        /// Chuyển hóa trang bị.
        /// </summary>
        public const string EVENT_TRANSMUTE_GEAR = "TRANSMUTE_GEAR";

        /// <summary>
        /// Tiêu hao vàng.
        /// </summary>
        public const string EVENT_USE_GOLD = "USE_GOLD";

        /// <summary>
        /// Hoàn thành nhiệm vụ tuần.
        /// </summary>
        public const string EVENT_COMPLETE_WEEKLY = "COMPLETE_WEEKLY";
    }

    /// <summary>
    /// Loại nhiệm vụ trong hệ thống.
    /// </summary>
    public static class MissionSystemTypes
    {
        /// <summary>
        /// Nhiệm vụ chính tuyến.
        /// </summary>
        public const string MAIN = "MAIN";

        /// <summary>
        /// Nhiệm vụ hằng ngày.
        /// </summary>
        public const string DAILY = "DAILY";

        /// <summary>
        /// Nhiệm vụ hằng tuần.
        /// </summary>
        public const string WEEKLY = "WEEKLY";

        /// <summary>
        /// Thành tựu.
        /// </summary>
        public const string ACHIEVEMENT = "ACHIEVEMENT";
    }
}