using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Event.EventLeHoiBangLong.Interfaces;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.MissionSystem;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shop.IAP;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventLeHoiBangLong
{
    public class EventLeHoiBangLongManager : Singleton<EventLeHoiBangLongManager>
    {
        public IEventLeHoiBangLongStorage Storage { get; private set; }
        public IEventLeHoiBangLongService Service { get; private set; }

        private readonly HashSet<int> _pendingBonusPurchases = new();
        private static readonly TimeSpan DayCheckInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Được phát sau khi tiến trình sự kiện thay đổi để UI cập nhật ngay lập tức.
        /// </summary>
        public event Action OnDataChanged;

        public override UniTask InitializeAsync()
        {
            Storage = new EventLeHoiBangLongStorage();
            Storage.Load();

            Service = new EventLeHoiBangLongService(
                Storage,
                DatabaseManager.Instance.GetEventLHBLMissions()
            );

            Service.Initialize(DateTime.Now);
            SubscribeEvents();
            MonitorDayChangeAsync().Forget();
            return UniTask.CompletedTask;
        }

        protected override void OnDestroy()
        {
            UnsubscribeEvents();
            base.OnDestroy();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RefreshDailyState();
            }
        }

        /// <summary>
        /// Yêu cầu mua gói bonus trả phí bằng IAP sau khi lượt bonus miễn phí đã nhận.
        /// Khi thanh toán thành công, trả danh sách phần thưởng qua callback để UI hiển thị.
        /// </summary>
        public bool RequestBonusPurchase(
            int day,
            Action<IReadOnlyList<ItemData>> onSuccess = null)
        {
            if (!Service.CanPurchaseBonus(day) ||
                !_pendingBonusPurchases.Add(day))
            {
                return false;
            }

            var config = DatabaseManager.Instance.GetEventLHBLCheckInBonusRewards(day);
            var storeProductId = IAPManager.GetStoreProductId(config.product);

            if (config.packId <= 0 ||
                string.IsNullOrWhiteSpace(storeProductId))
            {
                _pendingBonusPurchases.Remove(day);
                Debug.LogError($"[EventLeHoiBangLong] Không tìm thấy cấu hình IAP cho bonus ngày {day}.");
                return false;
            }

            IAPManager.Instance.BuyPackProduct(config.packId, storeProductId, (success, error) =>
            {
                _pendingBonusPurchases.Remove(day);

                if (!success)
                {
                    Debug.LogWarning($"[EventLeHoiBangLong] Mua bonus ngày {day} thất bại: {error}");
                    return;
                }

                // RPC IAP đã cấp vật phẩm, client chỉ lưu trạng thái để không cho mua/nhận lại.
                var changed = Service.ConfirmBonusPurchase(day);
                changed |= Service.ClaimBonus(day);
                NotifyIfChanged(changed);

                if (changed)
                {
                    onSuccess?.Invoke(config.bonusRewards);
                }
            });

            return true;
        }

        /// <summary>
        /// Xác nhận mua bonus thành công và cập nhật UI.
        /// </summary>
        public bool ConfirmBonusPurchase(int day)
        {
            return NotifyIfChanged(Service.ConfirmBonusPurchase(day));
        }

        /// <summary>
        /// Nhận phần thưởng miễn phí của ngày và cập nhật UI.
        /// </summary>
        public bool ClaimFreeLoginReward(int day)
        {
            return NotifyIfChanged(Service.ClaimFreeLoginReward(day));
        }

        /// <summary>
        /// Nhận lượt thưởng miễn phí riêng trong panel bonus và cập nhật UI.
        /// </summary>
        public bool ClaimFreeBonus(int day)
        {
            return NotifyIfChanged(Service.ClaimFreeBonus(day));
        }

        /// <summary>
        /// Nhận phần thưởng bonus đã mua của ngày và cập nhật UI.
        /// </summary>
        public bool ClaimBonus(int day)
        {
            return NotifyIfChanged(Service.ClaimBonus(day));
        }

        /// <summary>
        /// Cộng tiến độ nhiệm vụ theo trigger trong config và cập nhật UI.
        /// </summary>
        public bool ChangeMissionProgress(string trigger, int value)
        {
            return NotifyIfChanged(Service.ChangeMissionProgress(trigger, value));
        }

        /// <summary>
        /// Nhận thưởng một nhiệm vụ hoàn thành và cộng tổng điểm.
        /// </summary>
        public List<ItemData> ClaimMission(DynamicHeroesGlobalSpecificationsEventBLDailyMissionRow row)
        {
            var rewards = new List<ItemData>();

            if (Service.ClaimMission(row))
            {
                rewards.Add(new ItemData(row.itemId, row.quantity));
                NotifyIfChanged(true);
            }

            return rewards;
        }

        /// <summary>
        /// Nhận một milestone điểm nhiệm vụ và cập nhật UI.
        /// </summary>
        public List<ItemData> ClaimMissionMilestone(DynamicHeroesGlobalSpecificationsEventBLDailyMissionMilestoneRow row)
        {
            var rewards = new List<ItemData>();

            if (Service.ClaimMissionMilestone(row))
            {
                rewards.Add(new ItemData(row.itemId1, row.quantity1));
                NotifyIfChanged(true);
            }

            return rewards;
        }

        /// <summary>
        /// Nhận tất cả milestone nhiệm vụ đang đủ điều kiện.
        /// </summary>
        public List<ItemData> ClaimAvailableMissionMilestones()
        {
            var changed = false;
            var rewards = new List<ItemData>();

            foreach (var row in DatabaseManager.Instance.GetEventLHBLMissionMilestones())
            {
                if (!Service.ClaimMissionMilestone(row))
                {
                    continue;
                }

                changed = true;

                rewards.Add(new ItemData(row.itemId1, row.quantity1));
            }

            NotifyIfChanged(changed);
            return rewards;
        }

        /// <summary>
        /// Cộng điểm tích lũy cho lượt triệu hồi trong event và cập nhật UI.
        /// </summary>
        public bool RecordEventSummon(int times)
        {
            return NotifyIfChanged(Service.AddSummonPoints(times));
        }

        /// <summary>
        /// Nhận một milestone tích lũy triệu hồi và cập nhật UI.
        /// </summary>
        public List<ItemData> ClaimSummonMilestone(DynamicHeroesGlobalSpecificationsEventBLDailyGachaMilestoneRow row)
        {
            var rewards = new List<ItemData>();

            if (Service.ClaimSummonMilestone(row))
            {
                rewards.Add(new ItemData(row.itemId1, row.quantity1));
                NotifyIfChanged(true);
            }

            return rewards;
        }

        /// <summary>
        /// Nhận tất cả milestone triệu hồi đang đủ điều kiện.
        /// </summary>
        public List<ItemData> ClaimAvailableSummonMilestones()
        {
            var changed = false;
            var rewards = new List<ItemData>();

            foreach (var row in DatabaseManager.Instance.GetEventLHBLMilestone())
            {
                if (!Service.ClaimSummonMilestone(row))
                {
                    continue;
                }

                changed = true;

                rewards.Add(new ItemData(row.itemId1, row.quantity1));
            }

            NotifyIfChanged(changed);
            return rewards;
        }

        private void SubscribeEvents()
        {
            PlayerSystemManager.Instance.OnLoginNewDay += OnLoginNewDay;
            GameEventManager.Subscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Subscribe(GameEvents.ON_ENHANCE_GEAR, OnEnhanceGear);
            GameEventManager.Subscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnClaimIdleReward);
        }

        private void UnsubscribeEvents()
        {
            if (PlayerSystemManager.Instance != null)
            {
                PlayerSystemManager.Instance.OnLoginNewDay -= OnLoginNewDay;
            }

            GameEventManager.Unsubscribe<int>(GameEvents.OnEnemyDead, OnEnemyDead);
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe<int>(GameEvents.ON_SUMMON_HERO, OnSummonHero);
            GameEventManager.Unsubscribe(GameEvents.ON_ENHANCE_GEAR, OnEnhanceGear);
            GameEventManager.Unsubscribe(GameEvents.ON_AFK_REWARD_CLAIM_COUNT, OnClaimIdleReward);
        }

        private void OnLoginNewDay()
        {
            RefreshDailyState();
        }

        /// <summary>
        /// Theo dõi ngày UTC trong lúc game vẫn đang mở để reset dữ liệu ngay khi bước sang ngày mới.
        /// </summary>
        private async UniTaskVoid MonitorDayChangeAsync()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await UniTask.Delay(
                        DayCheckInterval,
                        cancellationToken: cancellationToken
                    );

                    RefreshDailyState();
                }
            }
            catch (OperationCanceledException)
            {
                // Manager đã bị hủy.
            }
        }

        /// <summary>
        /// Reset nhiệm vụ và ghi nhận đăng nhập bằng cùng một thời điểm để tránh lệch ngày tại ranh giới 00:00 UTC.
        /// </summary>
        private void RefreshDailyState()
        {
            if (Service == null)
            {
                return;
            }

            var utcNow = DateTime.Now;
            var changed = Service.CheckDailyMissionReset(utcNow);
            changed |= Service.RegisterLogin(utcNow);
            NotifyIfChanged(changed);
        }

        private void OnEnemyDead(int count)
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_KILL_MONSTER, count > 0 ? 1 : 0);
        }

        private void OnStageCleared(int stage)
        {
            ChangeMissionProgress("CLEAR_STAGE_COUNT", 1);
        }

        private void OnSummonHero(int times)
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_HERO_SUMMON, times);
        }

        private void OnClaimIdleReward()
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_CLAIM_IDLE, 1);
        }

        private void OnEnhanceGear()
        {
            ChangeMissionProgress(MissionEventKeys.EVENT_ENHANCE_GEAR, 1);
        }

        private bool NotifyIfChanged(bool changed)
        {
            if (changed)
            {
                OnDataChanged?.Invoke();
            }

            return changed;
        }
    }
}