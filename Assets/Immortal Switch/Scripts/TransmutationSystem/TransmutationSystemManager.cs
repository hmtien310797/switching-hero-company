using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.TransmutationSystem.Interfaces;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using Newtonsoft.Json;
using Unity.Mathematics;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    public class TransmutationSystemManager : Singleton<TransmutationSystemManager>
    {
        [Header("Config")]
        [field: SerializeField]
        public TransmutationSystemDatabaseSO Database { get; private set; }

        private ITransmutationSystemService Service { get; set; }
        public ITransmutationSystemStorage Storage { get; private set; }

        /// <summary>
        /// event fire khi co thay doi du lieu cua transmutation
        /// 1: data changed
        /// </summary>
        public event Action<TransmutationSystemChanged> OnChanged;

        /// <summary>
        /// event fire khi equip thay doi
        /// 1: item removed
        /// 2: item added
        /// </summary>
        public event Action<PlayerEquipItem, PlayerEquipItem> OnEquipChanged;

        protected override void OnSingletonAwake()
        {
            Load();
            GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
        }

        private void OnEnemyDead(int obj)
        {
            Debug.Log($"Transmutation: OnEnemyDead: {obj}");

            if (obj < 1)
            {
                return;
            }

            var cfg = Database.RateConfig.rows.Find(v => v.level == Storage.Data.Level);

            if (cfg == null)
            {
                Debug.LogError($"Energy dont config: {Storage.Data.Level}");
                return;
            }

            var quantity = obj + Storage.Data.Level + cfg.cost;
            Service.AddEnergy(quantity);
            _DispatchChanged();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        private void Load()
        {
            Storage = new TransmutationSystemStorage(Database);
            Service = new TransmutationSystemService(Storage);

            Database.Load();
            Storage.Load();
        }

        public void NotifyReady()
        {
            _DispatchChanged();
        }

        public PlayerEquipItem GetEquip(string itemType)
        {
            return Service.GetEquip(itemType);
        }

        public IEnumerable<PlayerEquipItem> GetEquips()
        {
            return Service.GetEquips();
        }

        public List<KeyValuePair<StatType, float>> GetAllModifiers()
        {
            var modifiers = new Dictionary<StatType, float>();

            foreach (var modifier in Storage.Data.Equips.SelectMany(pair => pair.Value.Modifiers))
            {
                modifiers[modifier.StatType] = modifiers.GetValueOrDefault(modifier.StatType) + modifier.Value;
            }

            return modifiers.ToList();
        }

        public PlayerEquipItem Fuse()
        {
            if (Storage.Data.StuckEquip != null)
            {
                return Storage.Data.StuckEquip;
            }

            var levelRangeCfg = Database.LevelRangeConfig.rows.Find(v => v.transmutationLevel == Storage.Data.Level);

            if (levelRangeCfg == null)
            {
                Debug.LogError($"Level range config not found: {Storage.Data.Level}");
                return null;
            }

            var cfg = Database.RateConfig.rows.Find(v => v.level == Storage.Data.Level);

            if (cfg == null ||
                cfg.cost > Storage.Data.Energy)
            {
                Debug.LogError($"Energy dont enough for roll: current energy: {Storage.Data.Energy} - {cfg?.cost}");
                return null;
            }

            var tier = Service.RollTier(cfg);
            var itemCfg = Database.RandomItem(tier);

            if (itemCfg == null)
            {
                Debug.LogError($"{tier} not found");
                return null;
            }

            Debug.Log($"Fuse: {tier}");
            return Service.BuildEquip(itemCfg, levelRangeCfg);
        }

        public void Equip(PlayerEquipItem newEquip, PlayerEquipItem oldEquip)
        {
            Service.Equip(newEquip);
            OnEquipChanged?.Invoke(oldEquip, newEquip);
        }

        public void Dismantle(PlayerEquipItem newEquip)
        {
            // todo: remove stuck equip
            var cfg = Database.RateConfig.rows.Find(v => v.level == Storage.Data.Level);

            if (cfg == null)
            {
                Debug.LogError($"Energy dont config: {Storage.Data.Level}");
                return;
            }

            var quantity = Storage.Data.Level + cfg.cost;
            Service.AddEnergy(quantity);
            _DispatchChanged();
        }

        private void _DispatchChanged()
        {
            var cfg = Database.LevelConfig.rows.Find(v => v.level == Storage.Data.Level);
            var targetExp = cfg?.requiredExp ?? 0;

            var changed = new TransmutationSystemChanged
            {
                Data = Storage.Data,
                Progress = targetExp > 0 ? 0 : Mathf.Clamp01((float)(Storage.Data.Exp / targetExp)),
                TargetExp = targetExp,
            };

            Debug.Log($"Data changed: {JsonConvert.SerializeObject(changed)}");
            OnChanged?.Invoke(changed);
        }
    }
}