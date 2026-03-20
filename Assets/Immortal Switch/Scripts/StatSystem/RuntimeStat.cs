using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.StatSystem
{
    [Serializable]
    public class RuntimeStat
    {
        public float BaseValue { get; private set; }
        public float FinalValue { get; private set; }

        public float MinValue { get; private set; } = float.MinValue;
        public float MaxValue { get; private set; } = float.MaxValue;

        private readonly List<StatModifier> modifiers = new();

        public RuntimeStat(float baseValue, float minValue = float.MinValue, float maxValue = float.MaxValue)
        {
            BaseValue = baseValue;
            MinValue = minValue;
            MaxValue = maxValue;
            Recalculate();
        }

        public void SetBaseValue(float value)
        {
            BaseValue = value;
            Recalculate();
        }

        public void SetClamp(float minValue, float maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Recalculate();
        }

        public void AddModifier(StatModifier modifier)
        {
            modifiers.Add(modifier);
            Recalculate();
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            bool removed = modifiers.Remove(modifier);
            if (removed)
                Recalculate();

            return removed;
        }

        public int RemoveModifiersBySource(string sourceId)
        {
            int removedCount = modifiers.RemoveAll(x => x.SourceId == sourceId);
            if (removedCount > 0)
                Recalculate();

            return removedCount;
        }

        public void ClearModifiers()
        {
            modifiers.Clear();
            Recalculate();
        }

        public IReadOnlyList<StatModifier> GetModifiers()
        {
            return modifiers;
        }

        public void Recalculate()
        {
            float totalAdd = 0f;
            float totalMul = 1f;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var mod = modifiers[i];
                switch (mod.Operation)
                {
                    case ModifierOp.Add:
                        totalAdd += mod.Value;
                        break;

                    case ModifierOp.Multiply:
                        totalMul *= (1f + mod.Value);
                        break;
                }
            }

            FinalValue = (BaseValue + totalAdd) * totalMul;

            if (FinalValue < MinValue) FinalValue = MinValue;
            if (FinalValue > MaxValue) FinalValue = MaxValue;
        }
    }
}