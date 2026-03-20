using System;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    [Serializable]
    public struct PowerUpModifierData
    {
        public string SourceId;
        public StatType TargetStat;
        public PowerUpValueKind ValueKind;
        public float Value;

        public PowerUpModifierData(string sourceId, StatType targetStat, PowerUpValueKind valueKind, float value)
        {
            SourceId = sourceId;
            TargetStat = targetStat;
            ValueKind = valueKind;
            Value = value;
        }
    }
    
    public enum PowerUpValueKind
    {
        FlatAdd,
        PercentOfBase
    }
}