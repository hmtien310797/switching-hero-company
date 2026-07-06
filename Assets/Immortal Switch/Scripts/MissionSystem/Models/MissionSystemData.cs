using System.Collections.Generic;

namespace Immortal_Switch.Scripts.MissionSystem.Models
{
    /// <summary>
    /// Dữ liệu tiến trình nhiệm vụ của người chơi.
    /// sau này sẽ tối ưu lại. tách riêng thành từng cụm main mission, daily mission, achievement mission.
    /// </summary>
    public class MissionSystemData
    {
        /// <summary>
        /// nhiem vu chinh
        /// </summary>
        public MissionSystemEntry Main;

        /// <summary>
        /// nhiem vu theo ngay
        /// </summary>
        public MissionSystemTask DailyTask;

        /// <summary>
        /// nhiem vu theo tuan
        /// </summary>
        public MissionSystemTask WeeklyTask;

        /// <summary>
        /// ds nhiem vu lap lai
        /// </summary>
        public List<MissionSystemEntry> RepeatTask;
    }

    public class MissionSystemTask
    {
        /// <summary>
        /// tong diem
        /// </summary>
        public int Point;

        /// <summary>
        /// ds nhiem vu theo tuan
        /// </summary>
        public List<MissionSystemEntry> Tasks;

        /// <summary>
        /// gia tri cua cac group da nhan thuong
        /// </summary>
        public List<MissionSystemPoint> PointsClaimed;
    }

    public class MissionSystemPoint
    {
        /// <summary>
        /// moc nhan
        /// </summary>
        public int Target;

        /// <summary>
        /// da nhan x2 hay chua
        /// </summary>
        public bool X2Claimed;
    }

    /// <summary>
    /// Dữ liệu tiến trình nhiệm vụ của người chơi.
    /// </summary>
    public class MissionSystemEntry
    {
        /// <summary>
        /// Id nhiệm vụ trong config.
        /// </summary>
        public string Id;

        /// <summary>
        /// Loại nhiệm vụ của id.
        /// </summary>
        public string EventKey;

        /// <summary>
        /// Tiến độ hiện tại của nhiệm vụ.
        /// </summary>
        public int Progress;

        /// <summary>
        /// Đã nhận thưởng.
        /// </summary>
        public bool IsClaimed;
    }
}