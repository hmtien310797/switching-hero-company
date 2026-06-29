using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// event fire khi setting changed
        /// 1: setting data
        /// </summary>
        public event Action<TransmutationSystemAutoSettingData> OnSettingChanged;

        // --- Private Fields ---
        private float _lastAutoFuseTime;

        protected override void OnSingletonAwake()
        {
            var now = Time.unscaledTime;
            _lastAutoFuseTime = now + autoFuseInterval;

            Load();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Đồng bộ toàn bộ state từ server (transmutation/list) — nguồn sự thật. Ghi đè toàn bộ
        /// state local (level/exp/energy/equips/pending), không merge — giống
        /// WeaponManager.SyncFromServer (xem Docs/be-transmutation-rpc-spec.md mục 3 &amp; 9).
        /// </summary>
        public async UniTask<bool> SyncFromServerAsync()
        {
            TransmutationListResponse response;
            try
            {
                response = await NakamaClient.Instance.GetTransmutationListAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransmutationSystemManager] transmutation/list RPC failed: {e.Message}");
                return false;
            }

            if (response == null)
            {
                return false;
            }

            Service.ApplyListResponse(response);
            _DispatchChanged();
            return true;
        }

        private void Load()
        {
            Storage = new TransmutationSystemStorage(Database);
            Service = new TransmutationSystemService(Storage);

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
            if (!Storage.Data.Setting.Enabled)
            {
                Debug.Log("Transmutation: AutoFuse: Stop by disabled");
                return;
            }

            if (Storage.Data.StuckEquip != null)
            {
                Debug.Log("Transmutation: AutoFuse: Stop by stuck 1");
                return;
            }

            TwiceFuse(Storage.Data.Setting.Count, Storage.Data.Setting.IsWaiting, Storage.Data.Setting.Tier).Forget();
            _DispatchChanged();
        }

        public void NotifyReady()
        {
            _DispatchChanged();
            OnSettingChanged?.Invoke(Storage.Data.Setting);
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

        public ETabPresetStatus IsUnlockGradeOption(EItemTier tier)
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

        public void SaveSetting(List<List<string>> uniqueOptions, int count, EItemTier tier, bool isEnabled)
        {
            Service.SaveSetting(uniqueOptions, count, tier, isEnabled);
            OnSettingChanged?.Invoke(Storage.Data.Setting);
        }

        public void SetWaitingMaterial(bool value)
        {
            Service.SetWaitingMaterial(value);
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

        public async UniTask TwiceFuse(int count, bool isWaitingMaterial, EItemTier tier)
        {
            for (var i = 0; i < count; i++)
            {
                if (Storage.Data.StuckEquip != null)
                {
                    Debug.Log("Transmutation: AutoFuse: Stop by stuck");
                    return;
                }

                var newEquip = await FuseAsync();

                if (newEquip != null)
                {
                    Debug.Log($"TwiceFuse {i} _ {newEquip.ParsedTier} _ {tier}");

                    // trùng tier
                    if (newEquip.ParsedTier >= tier)
                    {
                        var oldEquip = GetEquip(newEquip.ItemType);

                        if (oldEquip != null)
                        {
                            var ui = await UIManager.Instance.OpenPopupAsync<UITransmutationSystemReplaceStuckPanel>();
                            ui.Setup(newEquip, oldEquip);
                        }
                        else
                        {
                            await EquipAsync();
                        }

                        // check dieu kien dung
                        if (TryStopFuse(newEquip))
                        {
                            StopFuse();
                        }
                    }
                    else
                    {
                        await DismantleAsync();
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
            SaveSetting(new List<List<string>>(), 0, EItemTier.D, false);
        }

        private bool TryStopFuse(PlayerEquipViewData view)
        {
            return view.Modifiers.Any(modifier =>
            {
                var unique = TransmutationSystemHelper.ToModifier(modifier.StatType, modifier.Operation);
                return Storage.Data.Setting.UniqueOptions.Any(entries => entries.Contains(unique));
            });
        }

        /// <summary>
        /// Roll 1 lần nếu không có pending chờ resolve, hoặc trả lại pending hiện tại nếu auto-fuse
        /// đang Enabled (không roll mới — vòng lặp auto-fuse tự gọi <see cref="FuseAsync"/> riêng).
        /// </summary>
        public async UniTask<PlayerEquipViewData> FuseIfPossibleAsync()
        {
            return Storage.Data.Setting.Enabled ? GetStuckIfPossible() : await FuseAsync();
        }

        private PlayerEquipViewData GetStuckIfPossible()
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

            return null;
        }

        /// <summary>
        /// Roll 1 lần qua RPC transmutation/fuse — RNG (tier/base stat/unique stat) nằm hoàn toàn
        /// server-side, client chỉ apply lại kết quả (xem Docs/be-transmutation-rpc-spec.md mục 4).
        /// Nếu đang có pending chưa resolve, server trả lại đúng pending đó (không tốn thêm Energy)
        /// — <see cref="GetStuckIfPossible"/> ở đây chỉ là tối ưu hiển thị ngay từ cache local.
        /// </summary>
        private async UniTask<PlayerEquipViewData> FuseAsync()
        {
            var stuckEquip = GetStuckIfPossible();

            if (stuckEquip != null)
            {
                return stuckEquip;
            }

            TransmutationFuseResponse response;
            try
            {
                response = await NakamaClient.Instance.FuseTransmutationAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransmutationSystemManager] transmutation/fuse RPC failed: {e.Message}");
                return null;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                {
                    Debug.LogWarning($"[TransmutationSystemManager] transmutation/fuse rejected: {response.Error}");
                }

                return null;
            }

            Service.ApplyFuseResult(response);
            _DispatchChanged();

            var itemCfg = Database.ItemConfig.rows.Find(v => v.configId == response.Pending.CfgId);

            return new PlayerEquipViewData
            {
                Title = itemCfg != null ? itemCfg.itemName : response.Pending.CfgId,
                CfgId = response.Pending.CfgId,
                ItemType = response.Pending.ItemType,
                Level = response.Pending.Level,
                Modifiers = TransmutationSystemHelper.ToModifiers(response.Pending.Modifiers),
                Tier = response.Pending.Tier,
            };
        }

        /// <summary>
        /// Chốt giữ item pending hiện tại qua RPC transmutation/equip — server tự biết pending của
        /// user, không cần truyền id (xem Docs/be-transmutation-rpc-spec.md mục 5).
        /// </summary>
        public async UniTask<bool> EquipAsync()
        {
            TransmutationEquipResponse response;
            try
            {
                response = await NakamaClient.Instance.EquipTransmutationAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransmutationSystemManager] transmutation/equip RPC failed: {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                {
                    Debug.LogWarning($"[TransmutationSystemManager] transmutation/equip rejected: {response.Error}");
                }

                return false;
            }

            var oldEquip = TransmutationSystemHelper.ToPlayerEquipItem(response.ItemType, response.Replaced);
            var newEquip = TransmutationSystemHelper.ToPlayerEquipItem(response.ItemType, response.Equipped);

            Service.ApplyEquipResult(response);
            _DispatchChanged();
            OnEquipChanged?.Invoke(oldEquip, newEquip);
            return true;
        }

        /// <summary>Huỷ item pending hiện tại qua RPC transmutation/dismantle.</summary>
        public async UniTask<bool> DismantleAsync()
        {
            TransmutationDismantleResponse response;
            try
            {
                response = await NakamaClient.Instance.DismantleTransmutationAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[TransmutationSystemManager] transmutation/dismantle RPC failed: {e.Message}");
                return false;
            }

            if (response == null || !response.Success)
            {
                if (response != null)
                {
                    Debug.LogWarning($"[TransmutationSystemManager] transmutation/dismantle rejected: {response.Error}");
                }

                return false;
            }

            Service.ApplyDismantleResult(response);
            _DispatchChanged();
            return true;
        }

        private void _DispatchChanged()
        {
            var cfg = Database.LevelConfig.rows.Find(v => v.level == Storage.Data.Level);
            var targetExp = cfg?.totalExp ?? 0;

            var changed = new TransmutationSystemChanged
            {
                Data = Storage.Data,
                Progress = BigIntegerHelper.ClampProgress01(Storage.Data.Exp, targetExp),
                TargetExp = targetExp,
            };

            OnChanged?.Invoke(changed);
        }
    }
}