using Immortal_Switch.Scripts.Tutorial.Models;

namespace Immortal_Switch.Scripts.Tutorial.Interfaces
{
    public interface ITutorialStorage
    {
        /// <summary>
        /// save data
        /// </summary>
        TutorialSaveData Data { get; }

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