using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Models;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Interfaces
{
    /// <summary>
    /// Cung cấp dữ liệu lưu trữ cục bộ của sự kiện Lễ Hội Băng Long.
    /// </summary>
    public interface IEventLeHoiBangLongStorage
    {
        /// <summary>
        /// Dữ liệu tiến trình hiện tại của người chơi trong sự kiện.
        /// </summary>
        EventLeHoiBangLongData Data { get; }

        /// <summary>
        /// Đọc dữ liệu sự kiện đã lưu trên thiết bị.
        /// </summary>
        void Load();

        /// <summary>
        /// Ghi dữ liệu sự kiện hiện tại xuống thiết bị.
        /// </summary>
        void Save();
    }
}
