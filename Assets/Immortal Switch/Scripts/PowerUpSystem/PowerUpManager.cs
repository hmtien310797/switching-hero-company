using System;
using UnityEngine;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.StatSystem;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        [Header("Optional Refs")]
        [SerializeField] private GrowthManager growthManager;

        private PowerUpSystemService service;
        private StatsController boundPlayerStats;
        private bool sourcesInitialized;

        public event Action<PowerUpSnapshot> OnPowerUpChanged;

        public PowerUpSystemService Service => service;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            service = new PowerUpSystemService();
            service.OnPowerUpRebuilt += HandlePowerUpRebuilt;
        }

        private void Start()
        {
            TryInitializeSources();
            RebuildAndApply();
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

            if (growthManager == null)
                growthManager = GrowthManager.Instance;

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
            boundPlayerStats = statsController;
            TryInitializeSources();
            RebuildAndApply();
        }

        public void UnbindPlayer(StatsController statsController)
        {
            if (boundPlayerStats == statsController)
                boundPlayerStats = null;
        }

        public void RebuildAndApply()
        {
            TryInitializeSources();

            service.RebuildSnapshot();

            if (boundPlayerStats != null && boundPlayerStats.StatModule != null)
            {
                service.ApplyToStatModule(boundPlayerStats.StatModule);
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