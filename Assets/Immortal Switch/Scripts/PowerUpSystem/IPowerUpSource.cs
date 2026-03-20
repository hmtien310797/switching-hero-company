using System.Collections.Generic;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public interface IPowerUpSource
    {
        string SourceId { get; }
        void CollectPowerUps(List<PowerUpModifierData> output);
    }
}