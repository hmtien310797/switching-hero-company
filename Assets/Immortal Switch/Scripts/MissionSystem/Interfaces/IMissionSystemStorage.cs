using System;
using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.MissionSystem.Interfaces
{
    public interface IMissionSystemStorage
    {
        MissionSystemData Data { get; }

        /// <summary>
        /// Callback sau mỗi lần Save() — Manager dùng để fire-and-forget sync lên server.
        /// </summary>
        Action OnAfterSave { get; set; }

        /// <summary>
        /// Ghi đè Data từ server (không lưu ES3 ngay — chờ lần Save() tiếp theo).
        /// </summary>
        void LoadFromData(MissionSystemData data);

        void Save();

        /// <summary>
        /// load data
        /// </summary>
        void Load();

        /// <summary>
        /// reset ds nhiem vu daily
        /// </summary>
        void ResetDaily();

        /// <summary>
        /// reset ds nhiem vu weekly
        /// </summary>
        void ResetWeekly();

        /// <summary>
        /// init data sau khi load.
        /// </summary>
        void Initialize();
    }
}