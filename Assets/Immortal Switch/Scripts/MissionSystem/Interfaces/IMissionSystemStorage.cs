using Immortal_Switch.Scripts.MissionSystem.Models;

namespace Immortal_Switch.Scripts.MissionSystem.Interfaces
{
    public interface IMissionSystemStorage
    {
        /// <summary>
        /// save data
        /// </summary>
        MissionSystemData Data { get; }

        /// <summary>
        /// save data
        /// </summary>
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