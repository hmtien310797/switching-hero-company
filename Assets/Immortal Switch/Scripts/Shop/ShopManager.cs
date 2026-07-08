using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.PlayerSystem;
using Immortal_Switch.Scripts.Shop.Interfaces;
using UnityEngine;

namespace Immortal_Switch.Scripts.Shop
{
    public class ShopManager : Singleton<ShopManager>
    {
        public IShopService Service { get; private set; }
        public IShopStorage Storage { get; private set; }

        /// <summary>Kết quả recharge/state gần nhất — nguồn sự thật cho số lượt tích nạp/milestone
        /// đã nhận, không lưu local (xem <see cref="SyncRechargeStateAsync"/>).</summary>
        public RechargeStateResponse RechargeState { get; private set; }

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

        // ── Internal ──────────────────────────────────────────────────────────

        private void Load()
        {
            Storage = new ShopStorage();
            Service = new ShopService(Storage);
            Storage.Load();
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