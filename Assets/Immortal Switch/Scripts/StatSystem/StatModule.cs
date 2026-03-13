using System;
using System.Collections.Generic;

namespace Immortal_Switch.Scripts.StatSystem
{
    public class StatModule
    {
        public event Action<StatType, float, float> OnStatChanged;

        private readonly Dictionary<StatType, RuntimeStat> stats = new();

        public void Init(Dictionary<StatType, float> baseStats)
        {
            stats.Clear();

            foreach (var pair in baseStats)
            {
                stats[pair.Key] = CreateDefaultRuntimeStat(pair.Key, pair.Value);
            }
        }

        public IReadOnlyDictionary<StatType, RuntimeStat> GetAllStats()
        {
            return stats;
        }

        public bool HasStat(StatType statType)
        {
            return stats.ContainsKey(statType);
        }

        public float GetBaseStat(StatType statType)
        {
            return stats.TryGetValue(statType, out var stat) ? stat.BaseValue : 0f;
        }

        public float GetFinalStat(StatType statType)
        {
            return stats.TryGetValue(statType, out var stat) ? stat.FinalValue : 0f;
        }

        public RuntimeStat GetRuntimeStat(StatType statType)
        {
            stats.TryGetValue(statType, out var stat);
            return stat;
        }

        public void SetBaseStat(StatType statType, float value)
        {
            if (!stats.TryGetValue(statType, out var stat))
            {
                stat = CreateDefaultRuntimeStat(statType, value);
                stats[statType] = stat;
                OnStatChanged?.Invoke(statType, 0f, stat.FinalValue);
                return;
            }

            float oldValue = stat.FinalValue;
            stat.SetBaseValue(value);
            NotifyIfChanged(statType, oldValue, stat.FinalValue);
        }

        public void AddModifier(StatModifier modifier)
        {
            if (!stats.TryGetValue(modifier.StatType, out var stat))
            {
                stat = CreateDefaultRuntimeStat(modifier.StatType, 0f);
                stats[modifier.StatType] = stat;
            }

            float oldValue = stat.FinalValue;
            stat.AddModifier(modifier);
            NotifyIfChanged(modifier.StatType, oldValue, stat.FinalValue);
        }

        public bool RemoveModifier(StatModifier modifier)
        {
            if (!stats.TryGetValue(modifier.StatType, out var stat))
                return false;

            float oldValue = stat.FinalValue;
            bool removed = stat.RemoveModifier(modifier);

            if (removed)
                NotifyIfChanged(modifier.StatType, oldValue, stat.FinalValue);

            return removed;
        }

        public int RemoveModifiersBySource(string sourceId)
        {
            int totalRemoved = 0;

            foreach (var pair in stats)
            {
                float oldValue = pair.Value.FinalValue;
                int removed = pair.Value.RemoveModifiersBySource(sourceId);
                totalRemoved += removed;

                if (removed > 0)
                    NotifyIfChanged(pair.Key, oldValue, pair.Value.FinalValue);
            }

            return totalRemoved;
        }

        public void SetClamp(StatType statType, float minValue, float maxValue)
        {
            if (!stats.TryGetValue(statType, out var stat))
            {
                stat = CreateDefaultRuntimeStat(statType, 0f);
                stats[statType] = stat;
            }

            float oldValue = stat.FinalValue;
            stat.SetClamp(minValue, maxValue);
            NotifyIfChanged(statType, oldValue, stat.FinalValue);
        }

        private void NotifyIfChanged(StatType statType, float oldValue, float newValue)
        {
            if (Math.Abs(oldValue - newValue) > 0.0001f)
                OnStatChanged?.Invoke(statType, oldValue, newValue);
        }

        private RuntimeStat CreateDefaultRuntimeStat(StatType statType, float baseValue)
        {
            return statType switch
            {
                StatType.AttackSpeed => new RuntimeStat(baseValue, 0.05f, 100f),
                StatType.CritChance => new RuntimeStat(baseValue, 0f, 1f),
                StatType.CritDamage => new RuntimeStat(baseValue, 1f, 100f),
                StatType.Accuracy => new RuntimeStat(baseValue, 0f, 999999f),
                StatType.DamageReduction => new RuntimeStat(baseValue, 0f, 0.95f),
                _ => new RuntimeStat(baseValue)
            };
        }
    }
}