using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem.Models;
using Immortal_Switch.Scripts.Shared.Database;
using Immortal_Switch.Scripts.Shared.UI;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.TransmutationSystem.Interfaces;
using Immortal_Switch.Scripts.TransmutationSystem.Models;
using Immortal_Switch.Scripts.TransmutationSystem.Views.UI;
using Immortal_Switch.Scripts.UI;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Immortal_Switch.Scripts.TransmutationSystem
{
    public class TransmutationSystemManager : Singleton<TransmutationSystemManager>
    {
        [Header("Config")]
        [field: SerializeField]
        public TransmutationSystemDatabaseSO Database { get; private set; }

        /// <summary>
        /// thoi gian fuse tu dong neu bat.
        /// </summary>
        [SerializeField] private float autoFuseInterval = 2f;

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

        // --- Private Fields ---
        private float _lastAutoFuseTime;

        protected override void OnSingletonAwake()
        {
            var now = Time.unscaledTime;
            _lastAutoFuseTime = now + autoFuseInterval;

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

        public void LateUpdate()
        {
            if (Time.unscaledTime < _lastAutoFuseTime)
            {
                return;
            }

            _lastAutoFuseTime = Time.unscaledTime + autoFuseInterval;
            AutoFuse();
        }

        public void AutoFuse()
        {
            Debug.Log($"Transmutation: AutoFuse: {Storage.Data.Level}");

            if (!Storage.Data.Setting.Enabled)
            {
                return;
            }

            TwiceFuse(Storage.Data.Setting.Count, Storage.Data.Setting.IsWaiting, Storage.Data.Setting.Tier).Forget();
        }

        public void NotifyReady()
        {
            _DispatchChanged();
        }

        [CanBeNull]
        public PlayerEquipViewData GetEquip(string itemType)
        {
            var equip = Service.GetEquip(itemType);

            if (equip == null)
            {
                return null;
            }

            var cfg = Database.ItemConfig.rows.Find(v => v.configId == equip.CfgId);

            return new PlayerEquipViewData
            {
                Title = cfg.itemName,
                CfgId = cfg.configId,
                ItemType = cfg.itemType,
                Level = equip.Level,
                Modifiers = equip.Modifiers,
                Tier = equip.Tier,
            };
        }

        public ETabPresetStatus IsUnlockGradeOption(EEquipmentTier tier)
        {
            var firstCfg = Database.GradeConfig.rows.Find(v => v.highestUnlockedGrade == tier.ToString());

            // ko co cfg thi unlock false.
            if (firstCfg == null)
            {
                return ETabPresetStatus.Lock;
            }

            return Storage.Data.Level >= firstCfg.level ? ETabPresetStatus.Normal : ETabPresetStatus.Lock;
        }

        public IEnumerable<PlayerEquipItem> GetEquips()
        {
            return Service.GetEquips();
        }

        public void SaveSetting(List<List<string>> uniqueOptions, int count, EEquipmentTier tier, bool isEnabled)
        {
            Service.SaveSetting(uniqueOptions, count, tier, isEnabled);
        }

        public void SetWaitingMaterial(bool value)
        {
            Service.SetWaitingMaterial(value);
        }

        public bool ToggleWaitingMaterial()
        {
            return Service.ToggleWaitingMaterial();
        }

        public bool CurrentWaitingMaterial()
        {
            return Storage.Data.Setting.IsWaiting;
        }

        public Dictionary<int, ETabPresetStatus> GetCounts()
        {
            // key: so lan thuc hien
            // value: trang thai cua tab
            var result = new Dictionary<int, ETabPresetStatus>
            {
                { 1, ETabPresetStatus.Selected },
                { 2, ETabPresetStatus.Normal },
            };

            foreach (var row in Database.CountConfig.rows

                         // neu ko chua key
                         .Where(row => !result.ContainsKey(row.maxAutoCount))

                         // check level cua config dau tien
                         .Where(row => Storage.Data.Level >= row.maxAutoCount))
            {
                result.TryAdd(row.maxAutoCount, ETabPresetStatus.Normal);
            }

            return result;
        }

        public DynamicHeroesGlobalSpecificationsTransmuationUniqueRow GetUniqueCfg(StatType stat, ModifierOp op)
        {
            var mapping = TransmutationSystemHelper.ToModifier(stat, op);
            return Database.UniqueConfig.rows.Find(v => v.uniqueId == mapping);
        }

        public List<KeyValuePair<StatType, (float pct, bool isUnique, ModifierOp op)>> GetAllModifiers()
        {
            var modifiers = new Dictionary<StatType, (float pct, bool isUnique, ModifierOp op)>();

            foreach (var modifier in Storage.Data.Equips.SelectMany(pair => pair.Value.Modifiers))
            {
                modifiers[modifier.StatType] = (
                    modifiers.GetValueOrDefault(modifier.StatType).Item1 + modifier.Value,
                    modifier.IsUnique,
                    modifier.Operation
                );
            }

            return modifiers.ToList();
        }

        public async UniTask TwiceFuse(int count, bool isWaitingMaterial, EEquipmentTier tier)
        {
            for (var i = 0; i < count; i++)
            {
                var newEquip = Fuse();

                if (newEquip != null)
                {
                    // trùng tier
                    if (newEquip.ParsedTier == tier)
                    {
                        var oldEquip = GetEquip(newEquip.ItemType);

                        if (oldEquip != null)
                        {
                            var ui = await UIManager.Instance.OpenPopupAsync<UITransmutationSystemReplaceStuckPanel>();
                            ui.Setup(newEquip, oldEquip);
                        }
                        else
                        {
                            Equip(newEquip, null);
                        }

                        if (TryStopFuse(newEquip))
                        {
                            StopFuse();
                        }
                    }
                    else
                    {
                        Dismantle(newEquip);
                    }
                }
                else if (!isWaitingMaterial)
                {
                    // todo: show toast hết tiền rồi dừng luôn
                    StopFuse();
                }
            }
        }

        private void StopFuse()
        {
            // dung cau hinh tu dong.
            SaveSetting(new List<List<string>>(), 0, EEquipmentTier.D, false);
        }

        private bool TryStopFuse(PlayerEquipViewData view)
        {
            return view.Modifiers.Any(modifier =>
                Storage.Data.Setting.UniqueOptions.Any(entries => entries.Contains(modifier.StatType.ToString())));
        }

        public PlayerEquipViewData Fuse()
        {
            if (Storage.Data.StuckEquip != null)
            {
                var stuckCfg = Database.ItemConfig.rows.Find(v => v.configId == Storage.Data.StuckEquip.CfgId);

                if (stuckCfg != null)
                {
                    return new PlayerEquipViewData
                    {
                        Title = stuckCfg.itemName,
                        CfgId = stuckCfg.configId,
                        ItemType = stuckCfg.itemType,
                        Level = Storage.Data.StuckEquip.Level,
                        Modifiers = Storage.Data.StuckEquip.Modifiers,
                        Tier = Storage.Data.StuckEquip.Tier,
                    };
                }

                Debug.LogError($"Stuck config not found: {Storage.Data.StuckEquip.CfgId}");
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

            var modifiersOption = Database.ItemUniqueConfig.rows.Where(v => v.tier == itemCfg.tier).ToList();
            var uniqueModifiers = Service.BuildUniqueModifiers(modifiersOption, 2);
            var equip = Service.BuildEquip(itemCfg, levelRangeCfg, uniqueModifiers);

            return new PlayerEquipViewData
            {
                Title = itemCfg.itemName,
                CfgId = itemCfg.configId,
                ItemType = itemCfg.itemType,
                Level = equip.Level,
                Modifiers = equip.Modifiers,
                Tier = equip.Tier,
            };
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