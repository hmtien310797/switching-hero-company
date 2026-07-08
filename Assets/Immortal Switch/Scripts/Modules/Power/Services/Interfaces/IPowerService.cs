using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.Modules.Power.Services.Interfaces
{
    public interface IPowerService
    {
        /// <summary>
        /// tinh toan luc chien cua player
        /// </summary>
        double CalculatePlayerCp();

        /// <summary>
        /// tinh toan luc chien cua hero
        /// </summary>
        double CalculateHeroCp(StatsController stats, int playerLevel);
    }
}