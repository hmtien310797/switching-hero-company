using Immortal_Switch.Scripts.TransmutationSystem.Models;

namespace Immortal_Switch.Scripts.TransmutationSystem.Interfaces
{
    public interface ITransmutationSystemStorage
    {
        /// <summary>
        /// save data
        /// </summary>
        TransmutationSystemData Data { get; }

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