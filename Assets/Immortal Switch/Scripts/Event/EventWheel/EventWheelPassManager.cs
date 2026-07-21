using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shop.IAP;
using Nakama;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel
{
    /// <summary>
    /// Cache trạng thái Event Wheel lấy từ server (RPC eventwheel/state) — server là nguồn sự
    /// thật duy nhất, thay cho EventWheelPassService/Storage (ES3) cục bộ trước đây.
    /// </summary>
    public class EventWheelPassManager : Singleton<EventWheelPassManager>
    {
        public event Action OnDataChanged;

        public EventWheelStateResponse State { get; private set; }

        public override UniTask InitializeAsync() => UniTask.CompletedTask;

        /// <summary>Tải lại toàn bộ state từ server. Gọi khi mở EventWheelView và sau mỗi
        /// spin/shop_buy/pass_claim/pass_buy_premium thành công để đồng bộ số dư/tiến trình.</summary>
        public async UniTask RefreshAsync()
        {
            try
            {
                State = await NakamaClient.Instance.GetEventWheelStateAsync();
                OnDataChanged?.Invoke();
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventWheelPassManager] eventwheel/state failed: {ex.StatusCode} {ex.Message}");
            }
        }

        // ── Read-only accessors — luôn đọc từ snapshot RefreshAsync() gần nhất ──────────────

        public int GetSpinTotal()
        {
            return State?.Progress?.SpinTotal ?? 0;
        }

        public int GetShopPurchasedCount(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row)
        {
            var slot = FindShopSlot(row.shopSlotId);
            return slot?.PurchasedCount ?? 0;
        }

        public int GetShopRemaining(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row)
        {
            var slot = FindShopSlot(row.shopSlotId);
            if (slot == null) return row.limitValue;
            return Math.Max(0, slot.LimitValue - slot.PurchasedCount);
        }

        public bool CanPurchaseShopItem(
            DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row,
            int purchaseCount = 1
        )
        {
            var slot = FindShopSlot(row.shopSlotId);
            if (slot == null) return true;
            return slot.LimitValue <= 0 || slot.PurchasedCount + purchaseCount <= slot.LimitValue;
        }

        public bool IsFreeClaimed(int eventId, int milestoneId)
        {
            return FindMilestone(milestoneId)?.FreeClaimed ?? false;
        }

        public bool IsPremiumClaimed(int eventId, int milestoneId)
        {
            return FindMilestone(milestoneId)?.PaidClaimed ?? false;
        }

        public bool IsPremiumPurchased(int eventId)
        {
            return State?.Progress?.IsPremium ?? false;
        }

        /// <summary>Còn track free có thể nhận ở mốc này không (không xét track paid).</summary>
        public bool CanClaimFree(int eventId, DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row)
        {
            var milestone = FindMilestone(row.milestoneId);
            return milestone != null && milestone.IsEligible && !milestone.FreeClaimed;
        }

        /// <summary>Còn track paid có thể nhận ở mốc này không — cần đã mua Premium Pass.</summary>
        public bool CanClaimPaid(int eventId, DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row)
        {
            var milestone = FindMilestone(row.milestoneId);
            return milestone != null && milestone.IsEligible && !milestone.PaidClaimed && IsPremiumPurchased(eventId);
        }

        /// <summary>Còn ít nhất 1 track (free hoặc paid) có thể nhận ở mốc này — dùng cho nút Claim
        /// gộp 1 lần (UIEventWheelPassItem) và điều kiện enable "Claim All".</summary>
        public bool CanClaim(int eventId, DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row)
        {
            return CanClaimFree(eventId, row) || CanClaimPaid(eventId, row);
        }

        public bool HasClaimable(
            int eventId,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        )
        {
            if (rows == null) return false;

            for (int i = 0; i < rows.Count; i++)
            {
                if (CanClaim(eventId, rows[i])) return true;
            }

            return false;
        }

        // ── Mutating actions — gọi RPC, refresh cache, trả reward cho UI hiển thị popup ─────

        /// <summary>Nhận thưởng 1 mốc — tự động nhận CẢ 2 track (free + paid nếu đã mua Premium
        /// Pass và còn track paid chưa nhận), khớp với UIEventWheelPassItem chỉ có 1 nút Claim.</summary>
        public async UniTask<(List<ItemData> rewards, string error)> ClaimMilestoneAsync(
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row
        )
        {
            var rewards   = new List<ItemData>();
            string error = null;

            if (CanClaimFree(0, row))
            {
                var (item, err) = await ClaimTrackAsync(row.level, "free");
                if (item != null) rewards.Add(item); else error = err;
            }

            if (CanClaimPaid(0, row))
            {
                var (item, err) = await ClaimTrackAsync(row.level, "paid");
                if (item != null) rewards.Add(item); else error = error ?? err;
            }

            if (rewards.Count > 0) await RefreshAsync();

            return (rewards, rewards.Count > 0 ? null : error);
        }

        public async UniTask<(List<ItemData> rewards, string error)> ClaimAllAsync(
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        )
        {
            var rewards = new List<ItemData>();
            if (rows == null) return (rewards, null);

            // Chốt danh sách đủ điều kiện trước khi bắt đầu vòng lặp — mỗi lần claim gọi RPC
            // tuần tự (không song song) để tránh 2 request cùng ghi player_event_wheel storage.
            var freeClaimLevels = rows.Where(row => CanClaimFree(0, row)).Select(row => row.level).ToList();
            var paidClaimLevels = rows.Where(row => CanClaimPaid(0, row)).Select(row => row.level).ToList();

            string lastError = null;

            foreach (var level in freeClaimLevels)
            {
                var (item, err) = await ClaimTrackAsync(level, "free");
                if (item != null) rewards.Add(item); else lastError = err;
            }

            foreach (var level in paidClaimLevels)
            {
                var (item, err) = await ClaimTrackAsync(level, "paid");
                if (item != null) rewards.Add(item); else lastError = err;
            }

            await RefreshAsync();
            return (rewards, rewards.Count > 0 ? null : lastError);
        }

        private async UniTask<(ItemData item, string error)> ClaimTrackAsync(int level, string track)
        {
            EventWheelPassClaimResponse response;

            try
            {
                response = await NakamaClient.Instance.EventWheelPassClaimAsync(level, track);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventWheelPassManager] eventwheel/pass_claim error {ex.StatusCode}: {ex.Message}");
                return (null, "NETWORK_ERROR");
            }

            if (!response.Success)
            {
                Debug.LogWarning($"[EventWheelPassManager] eventwheel/pass_claim level={level} track={track} failed: {response.Error}");
                return (null, response.Error);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);

            return response.Item != null
                ? (new ItemData(response.Item.ItemId, response.Item.Amount), null)
                : (null, null);
        }

        public async UniTask<(bool success, List<ItemData> rewards, string error)> ShopBuyAsync(int shopSlotId)
        {
            EventWheelShopBuyResponse response;

            try
            {
                response = await NakamaClient.Instance.EventWheelShopBuyAsync(shopSlotId);
            }
            catch (ApiResponseException ex)
            {
                Debug.LogError($"[EventWheelPassManager] eventwheel/shop_buy error {ex.StatusCode}: {ex.Message}");
                return (false, new List<ItemData>(), "NETWORK_ERROR");
            }

            if (!response.Success)
            {
                return (false, new List<ItemData>(), response.Error);
            }

            CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
            await RefreshAsync();

            var rewards = response.Item != null
                ? new List<ItemData> { new ItemData(response.Item.ItemId, response.Item.Amount) }
                : new List<ItemData>();

            return (true, rewards, null);
        }

        /// <summary>Mua Premium Pass qua Unity IAP thật (server: eventwheel/pass_buy_premium).
        /// storeProductId lấy từ State.PremiumPack (server) — KHÔNG dùng DatabaseManager cục bộ,
        /// tránh lệch product_id giữa client/server (xem IAPManager.BuyEventPassProduct).</summary>
        public UniTask<(bool success, string error)> BuyPremiumAsync()
        {
            var pack = State?.PremiumPack;
            if (pack == null)
            {
                return UniTask.FromResult((false, "PASS_NOT_CONFIGURED"));
            }

#if UNITY_IOS
            var storeProductId = pack.AppleProductId;
#else
            var storeProductId = pack.GoogleProductId;
#endif

            if (string.IsNullOrEmpty(storeProductId))
            {
                return UniTask.FromResult((false, "PRODUCT_NOT_CONFIGURED"));
            }

            var tcs = new UniTaskCompletionSource<(bool, string)>();

            IAPManager.Instance.BuyEventPassProduct(storeProductId, (success, error) =>
            {
                tcs.TrySetResult((success, error));
            });

            return tcs.Task;
        }

        // ── lookups ──────────────────────────────────────────────────────────────────────

        private EventWheelShopSlotDto FindShopSlot(int shopSlotId)
        {
            var shop = State?.Shop;
            if (shop == null) return null;

            for (int i = 0; i < shop.Count; i++)
            {
                if (shop[i].ShopSlotId == shopSlotId) return shop[i];
            }

            return null;
        }

        private EventWheelPassMilestoneDto FindMilestone(int milestoneId)
        {
            var pass = State?.Pass;
            if (pass == null) return null;

            // milestone_id và level là 1:1 trong game_event_wheel_pass_config.js (cùng chạy 1..100).
            for (int i = 0; i < pass.Count; i++)
            {
                if (pass[i].MilestoneId == milestoneId) return pass[i];
            }

            return null;
        }
    }
}
