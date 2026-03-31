using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using UnityEngine;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public class PowerUpManager : Singleton<PowerUpManager>
    {
        private GrowthManager growthManager;

        private PowerUpSystemService service;
        private readonly List<StatsController> boundPlayerStats = new();
        private bool sourcesInitialized;

        public event Action<PowerUpSnapshot> OnPowerUpChanged;

        public PowerUpSystemService Service => service;
        public IReadOnlyList<StatsController> BoundPlayerStats => boundPlayerStats;

        public override UniTask InitializeAsync()
        {
            growthManager = GrowthManager.Instance;
            service = new PowerUpSystemService();
            service.OnPowerUpRebuilt += HandlePowerUpRebuilt;
            TryInitializeSources();
            RebuildAndApply();
            return UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            if (service != null)
                service.OnPowerUpRebuilt -= HandlePowerUpRebuilt;

            if (growthManager != null)
                growthManager.OnGrowthChanged -= HandleAnySourceChanged;
        }

        public void TryInitializeSources()
        {
            if (sourcesInitialized)
                return;

            if (growthManager != null && growthManager.Service != null)
            {
                service.RegisterSource(growthManager.Service);

                growthManager.OnGrowthChanged -= HandleAnySourceChanged;
                growthManager.OnGrowthChanged += HandleAnySourceChanged;
            }

            sourcesInitialized = true;
        }

        public void RegisterSource(IPowerUpSource source, bool rebuildNow = true)
        {
            service.RegisterSource(source);

            if (rebuildNow)
                RebuildAndApply();
        }

        public void UnregisterSource(IPowerUpSource source, bool rebuildNow = true)
        {
            service.UnregisterSource(source);

            if (rebuildNow)
                RebuildAndApply();
        }

        public void BindPlayer(StatsController statsController)
        {
            if (statsController == null)
                return;

            if (!boundPlayerStats.Contains(statsController))
                boundPlayerStats.Add(statsController);

            TryInitializeSources();
            ApplyToOne(statsController);
        }

        public void BindPlayers(IEnumerable<StatsController> statsControllers)
        {
            if (statsControllers == null)
                return;

            bool addedAny = false;

            foreach (var statsController in statsControllers)
            {
                if (statsController == null)
                    continue;

                if (boundPlayerStats.Contains(statsController))
                    continue;

                boundPlayerStats.Add(statsController);
                addedAny = true;
            }

            if (!addedAny)
                return;

            TryInitializeSources();
            RebuildAndApply();
        }

        public void UnbindPlayer(StatsController statsController)
        {
            if (statsController == null)
                return;

            boundPlayerStats.Remove(statsController);
        }

        public void UnbindAllPlayers()
        {
            boundPlayerStats.Clear();
        }

        public bool IsBound(StatsController statsController)
        {
            return statsController != null && boundPlayerStats.Contains(statsController);
        }

        public void RebuildAndApply()
        {
            TryInitializeSources();

            service.RebuildSnapshot();
            CleanupNullPlayers();

            for (int i = 0; i < boundPlayerStats.Count; i++)
            {
                ApplyToOne(boundPlayerStats[i]);
            }
        }

        public float GetFlatValue(StatType stat)
        {
            return service != null && service.CurrentSnapshot != null
                ? service.CurrentSnapshot.GetFlat(stat)
                : 0f;
        }

        public float GetPercentOfBaseValue(StatType stat)
        {
            return service != null && service.CurrentSnapshot != null
                ? service.CurrentSnapshot.GetPercentOfBase(stat)
                : 0f;
        }

        private void ApplyToOne(StatsController statsController)
        {
            if (statsController == null || statsController.StatModule == null)
                return;

            // Đảm bảo snapshot hiện tại đã có
            if (service.CurrentSnapshot == null)
                service.RebuildSnapshot();

            service.ApplyToStatModule(statsController.StatModule);
        }

        private void CleanupNullPlayers()
        {
            for (int i = boundPlayerStats.Count - 1; i >= 0; i--)
            {
                if (boundPlayerStats[i] == null)
                    boundPlayerStats.RemoveAt(i);
            }
        }

        private void HandleAnySourceChanged()
        {
            RebuildAndApply();
        }

        private void HandlePowerUpRebuilt(PowerUpSnapshot snapshot)
        {
            OnPowerUpChanged?.Invoke(snapshot);
        }
    }
}