using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.Profile.Models;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.TransmutationSystem;

namespace Immortal_Switch.Scripts.PowerUpSystem
{
    public class PowerUpManager : Singleton<PowerUpManager>
    {
        private GrowthManager growthManager;
        private TransmutationSystemManager _transmutationSystemManager;

        private PowerUpSystemService service;
        private readonly List<StatsController> boundPlayerStats = new();
        private bool sourcesInitialized;

        public event Action<PowerUpSnapshot> OnPowerUpChanged;

        public PowerUpSystemService Service => service;
        public IReadOnlyList<StatsController> BoundPlayerStats => boundPlayerStats;

        public override UniTask InitializeAsync()
        {
            growthManager = GrowthManager.Instance;
            _transmutationSystemManager = TransmutationSystemManager.Instance;
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

            if (_transmutationSystemManager != null)
                _transmutationSystemManager.OnEquipChanged -= OnTransmutationSystemEquipChanged;
        }

        public void TryInitializeSources()
        {
            if (sourcesInitialized)
                return;

            if (growthManager != null &&
                growthManager.Service != null)
            {
                service.RegisterSource(growthManager.Service);

                growthManager.OnGrowthChanged -= HandleAnySourceChanged;
                growthManager.OnGrowthChanged += HandleAnySourceChanged;
            }

            if (_transmutationSystemManager != null)
            {
                _transmutationSystemManager.OnEquipChanged -= OnTransmutationSystemEquipChanged;
                _transmutationSystemManager.OnEquipChanged += OnTransmutationSystemEquipChanged;
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
            if (statsController == null ||
                statsController.StatModule == null)
                return;

            // Đảm bảo snapshot hiện tại đã có
            if (service.CurrentSnapshot == null)
                service.RebuildSnapshot();

            service.ApplyToStatModule(statsController.StatModule);
            ApplyTransmutationTo(statsController);
        }

        /// <summary>
        /// Apply toàn bộ trang bị transmutation đang có trong storage vào hero.
        /// Clear-and-reapply qua source id cố định (POWERUP:TRANSMUTATION) — idempotent,
        /// không phụ thuộc reference equality của <see cref="StatModifier"/> (vốn là class
        /// không override Equals). Xử lý đúng cả trường hợp load game với equips cũ, sync
        /// server, và replace equip cùng slot (tránh double-count modifier cũ).
        /// </summary>
        private void ApplyTransmutationTo(StatsController statsController)
        {
            if (statsController == null ||
                statsController.StatModule == null)
                return;

            var module = statsController.StatModule;

            // Xoá toàn bộ modifier transmutation cũ rồi re-add từ storage hiện tại.
            module.RemoveModifiersBySource(StatSourceIds.Transmutation);

            if (_transmutationSystemManager == null)
                return;

            foreach (var equip in _transmutationSystemManager.GetEquips())
            {
                if (equip?.Modifiers == null)
                    continue;

                foreach (var mod in equip.Modifiers)
                {
                    // Clone với source id riêng để RemoveModifiersBySource xoá sạch —
                    // không đụng modifier của growth (POWERUP:SYSTEM) hay weapon
                    // (EQUIPMENT:HERO_*).
                    module.AddModifier(new StatModifier(
                        mod.StatType,
                        mod.Operation,
                        mod.Value,
                        StatSourceIds.Transmutation,
                        mod.IsUnique));
                }
            }
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

        private void OnTransmutationSystemEquipChanged(
            PlayerEquipItem oldEquip,
            PlayerEquipItem newEquip
        )
        {
            // RebuildAndApply sẽ clear-and-reapply toàn bộ equips (xem ApplyTransmutationTo)
            // cho mọi bound player — không cần phân biệt old/new, không phụ thuộc reference
            // equality. Cũng cover trường hợp SyncFromServerAsync fire (null, null).
            RebuildAndApply();
        }

        private void HandlePowerUpRebuilt(PowerUpSnapshot snapshot)
        {
            OnPowerUpChanged?.Invoke(snapshot);
        }
    }
}