using System;

namespace Immortal_Switch.Scripts.Level.Stage
{
    [Serializable]
    public struct StageReward
    {
        public string ResourceType;
        public double Amount;

        public StageReward(string resourceType, double amount)
        {
            ResourceType = resourceType;
            Amount = amount;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(ResourceType) && Amount > 0;
    }
}