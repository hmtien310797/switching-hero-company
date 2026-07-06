using Immortal_Switch.Scripts.DungeonSystem.Models;

namespace Immortal_Switch.Scripts.DungeonSystem.Interfaces
{
    public interface IDungeonSystemStorage
    {
        /// <summary>
        /// save data
        /// </summary>
        DungeonSystemData Data { get; }

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