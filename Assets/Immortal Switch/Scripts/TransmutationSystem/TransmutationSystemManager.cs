using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.TransmutationSystem.Interfaces;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    public class TransmutationSystemManager : Singleton<TransmutationSystemManager>
    {
        [Header("Config")] [SerializeField] private TransmutationSystemDatabaseSO database;

        private ITransmutationSystemService Service { get; set; }
        private ITransmutationSystemStorage Storage { get; set; }
        public TransmutationSystemData Data => Storage.Data;

        /// <summary>
        /// event fire khi energy thay đổi
        /// 1: energy
        /// </summary>
        public event Action<BigInteger> OnEnergyChanged;

        /// <summary>
        /// event fire khi equip thay doi
        /// 1: item removed
        /// 2: item added
        /// </summary>
        public event Action<PlayerEquipItem, PlayerEquipItem> OnEquipChanged;

        protected override void OnSingletonAwake()
        {
            //GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            //GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        }

        private void OnEnemyDead(int obj)
        {
            Debug.Log($"Transmutation: OnEnemyDead: {obj}");

            if (obj < 1)
            {
                return;
            }

            var cfg = database.RateConfig.rows.Find(v => v.level == Storage.Data.Level);

            if (cfg == null)
            {
                Debug.LogError($"Energy dont config: {Storage.Data.Level}");
                return;
            }

            var quantity = obj + Storage.Data.Level + cfg.cost;
            Service.AddEnergy(quantity);
            _DispatchEnergyChanged();
        }

        public override UniTask InitializeAsync()
        {
            Load();
            return UniTask.CompletedTask;
        }

        private void Load()
        {
            Storage = new TransmutationSystemStorage(database);
            Service = new TransmutationSystemService(Storage);

            database.Load();
            Storage.Load();
            _DispatchEnergyChanged();
        }

        public PlayerEquipItem GetEquip(string itemType)
        {
            return Service.GetEquip(itemType);
        }

        public IEnumerable<PlayerEquipItem> GetEquips()
        {
            return Service.GetEquips();
        }

        public PlayerEquipItem Transmutation()
        {
            var levelRangeCfg = database.LevelRangeConfig.rows.Find(v => v.transmutationLevel == Storage.Data.Level);

            if (levelRangeCfg == null)
            {
                Debug.LogError($"Level range config not found: {Storage.Data.Level}");
                return null;
            }

            var cfg = database.RateConfig.rows.Find(v => v.level == Storage.Data.Level);

            if (cfg == null ||
                cfg.cost > Storage.Data.Energy)
            {
                Debug.LogError($"Energy dont enough for roll: current energy: {Storage.Data.Energy} - {cfg?.cost}");
                return null;
            }

            var tier = Service.RollTier(cfg);
            var itemCfg = database.RandomItem(tier);

            if (itemCfg == null)
            {
                Debug.LogError($"{tier} not found");
                return null;
            }

            return Service.BuildEquip(itemCfg, levelRangeCfg);
        }

        public void Equip(PlayerEquipItem newEquip, PlayerEquipItem oldEquip)
        {
            Service.Equip(newEquip);
            OnEquipChanged?.Invoke(oldEquip, newEquip);
        }

        public void Dismantle(PlayerEquipItem newEquip)
        {
            var cfg = database.RateConfig.rows.Find(v => v.level == Storage.Data.Level);

            if (cfg == null)
            {
                Debug.LogError($"Energy dont config: {Storage.Data.Level}");
                return;
            }

            var quantity = Storage.Data.Level + cfg.cost;
            Service.AddEnergy(quantity);
            _DispatchEnergyChanged();
        }

        private void _DispatchEnergyChanged()
        {
            OnEnergyChanged?.Invoke(Storage.Data.Energy);
        }
    }
}