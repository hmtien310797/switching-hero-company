using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public class PowerUpSystemService
    {
        private const string PowerUpSourceTag = "POWER_UP_SYSTEM";

        private readonly List<IPowerUpSource> sources = new();
        private readonly List<PowerUpModifierData> rawModifiers = new();
        private PowerUpSnapshot currentSnapshot = new();

        public event Action<PowerUpSnapshot> OnPowerUpRebuilt;

        public PowerUpSnapshot CurrentSnapshot => currentSnapshot;
        public IReadOnlyList<PowerUpModifierData> RawModifiers => rawModifiers;

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

            for (int i = 0; i < sources.Count; i++)
            {
                sources[i].CollectPowerUps(rawModifiers);
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

            statModule.RemoveModifiersBySource(PowerUpSourceTag);

            foreach (var pair in currentSnapshot.FlatAdds)
            {
                statModule.AddModifier(new StatModifier(
                    pair.Key,
                    ModifierOp.Add,
                    pair.Value,
                    PowerUpSourceTag
                ));
            }

            foreach (var pair in currentSnapshot.BasePercents)
            {
                statModule.AddModifier(new StatModifier(
                    pair.Key,
                    ModifierOp.Multiply,
                    pair.Value,
                    PowerUpSourceTag
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