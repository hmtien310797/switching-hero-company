using Immortal_Switch.Scripts.AFKReward.Models;

namespace Immortal_Switch.Scripts.AFKReward.Interfaces
{
    /// <summary>
    /// Lưu trữ dữ liệu AFK Reward (số lượt claim x2, ngày claim).
    /// </summary>
    public interface IAFKRewardStorage
    {
        /// <summary>
        /// Dữ liệu AFK Reward.
        /// </summary>
        AFKRewardData Data { get; }

        /// <summary>
        /// Lưu dữ liệu xuống ES3.
        /// </summary>
        void Save();

        /// <summary>
        /// Đọc dữ liệu từ ES3, khởi tạo mới nếu chưa có.
        /// </summary>
        void Load();
    }
}
