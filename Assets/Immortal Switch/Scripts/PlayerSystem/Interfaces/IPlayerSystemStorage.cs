using Immortal_Switch.Scripts.PlayerSystem.Models;

namespace Immortal_Switch.Scripts.PlayerSystem.Interfaces
{
    public interface IPlayerSystemStorage
    {
        /// <summary>
        /// save data
        /// </summary>
        PlayerSystemData Data { get; }

        /// <summary>
        /// save data
        /// </summary>
        void Save();

        /// <summary>
        /// load data
        /// </summary>
        void Load();
    }
}