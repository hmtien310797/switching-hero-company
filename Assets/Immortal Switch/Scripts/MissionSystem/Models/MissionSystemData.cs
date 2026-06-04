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
        /// Ds các nhiệm vụ của user.
        /// key: mission type
        /// value: ds mission của type đó
        /// </summary>
        public Dictionary<string, List<MissionSystemEntry>> Missions = new();
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