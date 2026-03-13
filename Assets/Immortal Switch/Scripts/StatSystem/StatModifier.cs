using System;

namespace Immortal_Switch.Scripts.StatSystem
{
    [Serializable]
    public class StatModifier
    {
        public StatType StatType;
        public ModifierOp Operation;
        public float Value;
        public string SourceId;

        public StatModifier(StatType statType, ModifierOp operation, float value, string sourceId = "")
        {
            StatType = statType;
            Operation = operation;
            Value = value;
            SourceId = sourceId;
        }

        public StatModifier Clone()
        {
            return new StatModifier(StatType, Operation, Value, SourceId);
        }
    }
}