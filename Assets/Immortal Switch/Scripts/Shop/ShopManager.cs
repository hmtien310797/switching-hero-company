using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Shop.Interfaces;
using Immortal_Switch.Scripts.Shop.Models;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop
{
    public class ShopManager : Singleton<ShopManager>
    {
        public IShopService Service { get; private set; }
        public IShopStorage Storage { get; private set; }
        public ShopAtlasService Atlas { get; private set; }

        /// <summary>Kết quả recharge/state gần nhất — nguồn sự thật cho số lượt tích nạp/milestone
        /// đã nhận, không lưu local (xem <see cref="SyncRechargeStateAsync"/>).</summary>
        public RechargeStateResponse RechargeState { get; private set; }

        /// <summary>Kết quả monthlypass/state gần nhất — nguồn sự thật cho trạng thái mua/nhận
        /// thưởng ngày của Monthly Pass, không lưu local (xem <see cref="SyncMonthlyPassStateAsync"/>).</summary>
        public MonthlyPassStateResponse MonthlyPassState { get; private set; }

        /// <summary>Fire khi purchase count hoặc recharge state thay đổi (mua mới, reset, hoặc sync lại từ server).</summary>
        public event Action OnDataChanged;

        protected override void OnSingletonAwake()
        {
            Load();
        }

        public override async UniTask InitializeAsync()
        {
            Service.CheckAndReset();
            SubscribeEvents();
            await SyncRechargeStateAsync();
            await SyncMonthlyPassStateAsync();
        }

        protected override void OnDestroy()
        {
            UnsubscribeEvents();
            base.OnDestroy();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void RecordPurchase(int packId)
        {
            Service.RecordPurchase(packId);
            OnDataChanged?.Invoke();
        }

        public int GetRemaining(int packId, int limit)
        {
            return Service.GetRemaining(packId, limit);
        }

        public int GetPurchasedCount(int packId)
        {
            return Service.GetPurchasedCount(packId);
        }

        /// <summary>Kiểm tra gói kim cương còn ưu đãi x2 lần mua đầu hay không.</summary>
        public bool IsDiamondFirstPurchaseAvailable(int packId)
        {
            return !Service.HasPurchasedDiamondPack(packId);
        }

        /// <summary>Đánh dấu ưu đãi x2 lần đầu của gói kim cương đã được sử dụng và cập nhật UI.</summary>
        public void RecordDiamondFirstPurchase(int packId)
        {
            Service.RecordDiamondFirstPurchase(packId);
            OnDataChanged?.Invoke();
        }

        /// <summary>Lấy số lượt mua Event còn lại trong chu kỳ hiện tại.</summary>
        public int GetEventRemaining(int packId, int limit)
        {
            Service.CheckAndReset();
            return Service.GetEventRemaining(packId, limit);
        }

        /// <summary>Lấy số lần gói Event đã được mua trong chu kỳ hiện tại.</summary>
        public int GetEventPurchasedCount(int packId)
        {
            Service.CheckAndReset();
            return Service.GetEventPurchasedCount(packId);
        }

        /// <summary>Ghi nhận giao dịch Event nếu chưa đạt giới hạn và cập nhật UI ngay.</summary>
        public bool TryRecordEventPurchase(int packId, int limit)
        {
            Service.CheckAndReset();

            if (!Service.TryRecordEventPurchase(packId, limit))
            {
                return false;
            }

            OnDataChanged?.Invoke();
            return true;
        }

        // ── Topup / GloryPass (recharge/state — server là nguồn sự thật) ──────

        /// <summary>Tổng số lượt nạp trong tháng hiện tại, lấy từ recharge/state — không đếm local
        /// nữa vì server đã cộng điểm ngay khi iap/purchase hoặc iap/pack_purchase thành công.</summary>
        public int GetTopupCount()
        {
            return RechargeState?.Points ?? 0;
        }

        /// <summary>Kiểm tra milestone đã nhận thưởng trong tháng hiện tại chưa (theo recharge/state).</summary>
        public bool IsGloryPassClaimed(int milestoneId)
        {
            return RechargeState?.Milestones?.Find(m => m.Id == milestoneId)?.IsClaimed ?? false;
        }

        /// <summary>Gọi recharge/state và cache lại — dùng khi login (InitializeAsync) và mỗi lần mở
        /// màn Shop/GloryPass, để điểm/trạng thái claimed luôn khớp server thay vì đếm local.</summary>
        public async UniTask<bool> SyncRechargeStateAsync()
        {
            try
            {
                RechargeState = await NakamaClient.Instance.RechargeStateAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShopManager] recharge/state RPC failed: {e.Message}");
                return false;
            }

            OnDataChanged?.Invoke();
            return true;
        }

        /// <summary>Reset danh sách GloryPass đã nhận đầu tháng.</summary>
        public void ResetGloryPassClaims()
        {
            Service.ResetGloryPassClaims();
            OnDataChanged?.Invoke();
        }

        // ── Monthly Pass (monthlypass/state — server là nguồn sự thật) ────────

        /// <summary>Đã mua monthly pass packId chưa (còn trong hạn drip_days ngày), theo
        /// monthlypass/state — không đếm/lưu local nữa vì server tự tính hạn từ purchased_at.</summary>
        public bool IsMonthlyPassPurchased(int packId)
        {
            return MonthlyPassState?.Passes?.Find(p => p.Id == packId)?.IsPurchased ?? false;
        }

        /// <summary>Ngày hiện tại tính từ lúc mua (1-based), 0 nếu chưa mua hoặc đã hết hạn.</summary>
        public int GetMonthlyPassCurrentDay(int packId)
        {
            return MonthlyPassState?.Passes?.Find(p => p.Id == packId)?.CurrentDay ?? 0;
        }

        public bool IsMonthlyPassDayClaimed(int packId, int day)
        {
            var pass = MonthlyPassState?.Passes?.Find(p => p.Id == packId);
            return pass?.Days?.Find(d => d.Day == day)?.IsClaimed ?? false;
        }

        /// <summary>Gọi monthlypass/state và cache lại — dùng khi login (InitializeAsync) và sau mỗi
        /// lần mua/nhận thưởng Monthly Pass, để trạng thái luôn khớp server thay vì đếm local.</summary>
        public async UniTask<bool> SyncMonthlyPassStateAsync()
        {
            try
            {
                MonthlyPassState = await NakamaClient.Instance.MonthlyPassStateAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShopManager] monthlypass/state RPC failed: {e.Message}");
                return false;
            }

            OnDataChanged?.Invoke();
            return true;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void Load()
        {
            Storage = new ShopStorage();
            Service = new ShopService(Storage);
            Atlas = new ShopAtlasService();

            Storage.Load();
            Atlas.InitializeAsync().Forget();
        }

        private void SubscribeEvents()
        {
            PlayerSystemManager.Instance.OnLoginNewDay += OnLoginNewDay;
        }

        private void UnsubscribeEvents()
        {
            PlayerSystemManager.Instance.OnLoginNewDay -= OnLoginNewDay;
        }

        private void OnLoginNewDay()
        {
            Service.CheckAndReset();
            OnDataChanged?.Invoke();
        }
    }
}
