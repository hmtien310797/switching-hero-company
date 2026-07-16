using Immortal_Switch.Scripts.Event.EventWheel.Models;

namespace Immortal_Switch.Scripts.Event.EventWheel.Interfaces
{
    /// <summary>
    /// Định nghĩa nơi lưu trữ dữ liệu tiến trình Event Wheel Pass.
    /// </summary>
    public interface IEventWheelPassStorage
    {
        /// <summary>
        /// Dữ liệu Event Wheel Pass hiện đang được lưu trong bộ nhớ.
        /// </summary>
        EventWheelPassData Data { get; }

        /// <summary>
        /// Lưu dữ liệu Event Wheel Pass hiện tại.
        /// </summary>
        void Save();

        /// <summary>
        /// Tải dữ liệu Event Wheel Pass đã lưu.
        /// </summary>
        void Load();
    }
}
