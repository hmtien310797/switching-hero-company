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
        public bool IsUnique;

        public StatModifier(StatType statType, ModifierOp operation, float value)
        {
            StatType = statType;
            Operation = operation;
            Value = value;
            SourceId = "";
            IsUnique = false;
        }
        
        public StatModifier(StatType statType, ModifierOp operation, float value, string sourceId = "")
        {
            StatType = statType;
            Operation = operation;
            Value = value;
            SourceId = sourceId;
            IsUnique = false;
        }
        
        public StatModifier(StatType statType, ModifierOp operation, float value, string sourceId = "", bool isUnique = false)
        {
            StatType = statType;
            Operation = operation;
            Value = value;
            SourceId = sourceId;
            IsUnique = isUnique;
        }

        public StatModifier Clone()
        {
            return new StatModifier(StatType, Operation, Value, SourceId);
        }
    }
}