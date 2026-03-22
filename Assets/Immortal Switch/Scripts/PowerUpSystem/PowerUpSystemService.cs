using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public class PowerUpSystemService
    {
        public const string AppliedModifierSourceId = StatSourceIds.PowerUpSystem;

        private readonly List<IPowerUpSource> sources = new();
        private readonly List<PowerUpModifierData> rawModifiers = new();
        private readonly Dictionary<string, List<PowerUpModifierData>> rawModifiersBySource = new();

        private PowerUpSnapshot currentSnapshot = new();

        public event Action<PowerUpSnapshot> OnPowerUpRebuilt;

        public PowerUpSnapshot CurrentSnapshot => currentSnapshot;
        public IReadOnlyList<PowerUpModifierData> RawModifiers => rawModifiers;
        public IReadOnlyDictionary<string, List<PowerUpModifierData>> RawModifiersBySource => rawModifiersBySource;

        public void RegisterSource(IPowerUpSource source)
        {
            if (source == null) return;
            if (sources.Contains(source)) return;

            sources.Add(source);
        }

        public void UnregisterSource(IPowerUpSource source)
        {
            if (source == null) return;
            sources.Remove(source);
        }

        public PowerUpSnapshot RebuildSnapshot()
        {
            rawModifiers.Clear();
            rawModifiersBySource.Clear();

            for (int i = 0; i < sources.Count; i++)
            {
                sources[i].CollectPowerUps(rawModifiers);
            }

            for (int i = 0; i < rawModifiers.Count; i++)
            {
                var item = rawModifiers[i];
                string sourceId = string.IsNullOrEmpty(item.SourceId) ? "(No Source)" : item.SourceId;

                if (!rawModifiersBySource.TryGetValue(sourceId, out var list))
                {
                    list = new List<PowerUpModifierData>();
                    rawModifiersBySource.Add(sourceId, list);
                }

                list.Add(item);
            }

            var snapshot = new PowerUpSnapshot();

            for (int i = 0; i < rawModifiers.Count; i++)
            {
                snapshot.Add(rawModifiers[i]);
            }

            currentSnapshot = snapshot;
            OnPowerUpRebuilt?.Invoke(currentSnapshot);
            return currentSnapshot;
        }

        public void ApplyToStatModule(StatModule statModule)
        {
            if (statModule == null)
                return;

            statModule.RemoveModifiersBySource(AppliedModifierSourceId);

            foreach (var pair in currentSnapshot.FlatAdds)
            {
                statModule.AddModifier(new StatModifier(
                    pair.Key,
                    ModifierOp.Add,
                    pair.Value,
                    AppliedModifierSourceId
                ));
            }

            foreach (var pair in currentSnapshot.BasePercents)
            {
                statModule.AddModifier(new StatModifier(
                    pair.Key,
                    ModifierOp.Multiply,
                    pair.Value,
                    AppliedModifierSourceId
                ));
            }
        }

        public void RebuildAndApply(StatModule statModule)
        {
            RebuildSnapshot();
            ApplyToStatModule(statModule);
        }
    }
}