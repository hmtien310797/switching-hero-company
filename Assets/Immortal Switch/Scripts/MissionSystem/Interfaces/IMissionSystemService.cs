using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.MissionSystem.Interfaces
{
    internal interface IMissionSystemService
    {
        /// <summary>
        /// cap nhat progress mission
        /// </summary>
        /// <param name="type">loai nhiem vu can tang tien du lieu</param>
        /// <param name="progress">so luong can tang.</param>
        /// <returns>true neu cap nhat thanh cong, otherwise false.</returns>
        bool UpdateProgress(EMissionSystemType type, int progress);

        /// <summary>
        /// kiem tra mission complete hay chua
        /// </summary>
        /// <returns>true neu da hoan thanh, otherwise false.</returns>
        bool IsComplete();

        /// <summary>
        /// hoàn thành mission hien tai. va chuyen sang mission moi.
        /// </summary>
        void Complete();
    }
}