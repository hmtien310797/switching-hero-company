using Immortal_Switch.Scripts.AFKReward.Interfaces;
using Immortal_Switch.Scripts.AFKReward.Models;

namespace Immortal_Switch.Scripts.AFKReward
{
    /// <summary>
    /// Lưu trữ dữ liệu AFK Reward xuống ES3.
    /// </summary>
    internal class AFKRewardStorage : IAFKRewardStorage
    {
        private const string SAVE_KEY = nameof(AFKRewardData);

        public AFKRewardData Data { get; private set; }

        /// <summary>
        /// Lưu dữ liệu xuống ES3.
        /// </summary>
        public void Save()
        {
            ES3.Save(SAVE_KEY, Data);
        }

        /// <summary>
        /// Đọc dữ liệu từ ES3, khởi tạo mới nếu chưa có.
        /// </summary>
        public void Load()
        {
            Data = ES3.KeyExists(SAVE_KEY)
                ? ES3.Load<AFKRewardData>(SAVE_KEY)
                : new AFKRewardData();
        }
    }
}