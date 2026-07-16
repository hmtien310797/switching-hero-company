using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventWheel.Interfaces;
using Immortal_Switch.Scripts.Event.EventWheel.Models;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Helper;
using UnityEngine;

namespace Immortal_Switch.Scripts.Event.EventWheel
{
    /// <summary>
    /// Xử lý toàn bộ nghiệp vụ của Event Wheel Pass.
    /// </summary>
    internal class EventWheelPassService : IEventWheelPassService
    {
        private readonly IEventWheelPassStorage _storage;

        /// <summary>Khởi tạo service với nơi lưu trữ dữ liệu Event Wheel Pass.</summary>
        public EventWheelPassService(IEventWheelPassStorage storage)
        {
            _storage = storage;
        }

        /// <summary>Lấy tổng số lượt quay đã mua của một sự kiện.</summary>
        public int GetPurchasedSpinCount(int eventId)
        {
            return GetOrCreateProgress(eventId).PurchasedSpinCount;
        }

        /// <summary>Ghi nhận số lượt quay vừa mua và trả về dữ liệu có thay đổi hay không.</summary>
        public bool RecordSpinPurchase(int eventId, int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            var progress = GetOrCreateProgress(eventId);
            progress.PurchasedSpinCount += amount;
            _storage.Save();
            return true;
        }

        /// <summary>Lấy số lần đã mua của một ô shop theo loại giới hạn trong cấu hình.</summary>
        public int GetShopPurchasedCount(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row)
        {
            if (row == null)
            {
                return 0;
            }

            var progress = GetOrCreateProgress(row.eventId);
            CheckAndResetDailyShop(progress);
            var purchaseCounts = GetShopPurchaseCounts(progress, row.limitType);
            return purchaseCounts?.GetValueOrDefault(row.shopSlotId) ?? 0;
        }

        /// <summary>Lấy số lượt mua còn lại của một ô shop.</summary>
        public int GetShopRemaining(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row)
        {
            if (row == null)
            {
                return 0;
            }

            if (row.limitValue <= 0)
            {
                return int.MaxValue;
            }

            return Mathf.Max(0, row.limitValue - GetShopPurchasedCount(row));
        }

        /// <summary>Kiểm tra ô shop có thể mua thêm số lượt yêu cầu hay không.</summary>
        public bool CanPurchaseShopItem(
            DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row,
            int purchaseCount = 1
        )
        {
            if (row == null ||
                purchaseCount <= 0 ||
                GetShopPurchaseCounts(
                    GetOrCreateProgress(row.eventId),
                    row.limitType
                ) ==
                null)
            {
                return false;
            }

            return row.limitValue <= 0 || GetShopRemaining(row) >= purchaseCount;
        }

        /// <summary>Ghi nhận lượt mua shop nếu chưa vượt giới hạn và trả về thao tác có thành công hay không.</summary>
        public bool RecordShopPurchase(
            DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row,
            int purchaseCount = 1
        )
        {
            if (!CanPurchaseShopItem(row, purchaseCount))
            {
                return false;
            }

            var progress = GetOrCreateProgress(row.eventId);
            CheckAndResetDailyShop(progress);

            var purchaseCounts = GetShopPurchaseCounts(progress, row.limitType);
            var purchased = purchaseCounts.GetValueOrDefault(row.shopSlotId);
            purchaseCounts[row.shopSlotId] = purchased + purchaseCount;

            _storage.Save();
            return true;
        }

        /// <summary>Kiểm tra Premium Pass của sự kiện đã được mua hay chưa.</summary>
        public bool IsPremiumPurchased(int eventId)
        {
            return GetOrCreateProgress(eventId).PremiumPurchased;
        }

        /// <summary>Đánh dấu Premium Pass đã được mua và trả về dữ liệu có thay đổi hay không.</summary>
        public bool PurchasePremium(int eventId)
        {
            var progress = GetOrCreateProgress(eventId);

            if (progress.PremiumPurchased)
            {
                return false;
            }

            progress.PremiumPurchased = true;
            _storage.Save();
            return true;
        }

        /// <summary>Kiểm tra phần thưởng miễn phí của milestone đã được nhận hay chưa.</summary>
        public bool IsFreeClaimed(int eventId, int milestoneId)
        {
            return GetOrCreateProgress(eventId).ClaimedFreeMilestoneIds.Contains(milestoneId);
        }

        /// <summary>Kiểm tra phần thưởng Premium của milestone đã được nhận hay chưa.</summary>
        public bool IsPremiumClaimed(int eventId, int milestoneId)
        {
            return GetOrCreateProgress(eventId).ClaimedPremiumMilestoneIds.Contains(milestoneId);
        }

        /// <summary>Kiểm tra milestone có ít nhất một phần thưởng có thể nhận hay không.</summary>
        public bool CanClaim(
            int eventId,
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row
        )
        {
            if (row == null ||
                row.eventId != eventId)
            {
                return false;
            }

            var progress = GetOrCreateProgress(eventId);

            if (progress.PurchasedSpinCount < row.spinRequired)
            {
                return false;
            }

            return !progress.ClaimedFreeMilestoneIds.Contains(row.milestoneId) ||
                   progress.PremiumPurchased &&
                   !progress.ClaimedPremiumMilestoneIds.Contains(row.milestoneId);
        }

        /// <summary>Kiểm tra danh sách pass có milestone nào có thể nhận thưởng hay không.</summary>
        public bool HasClaimable(
            int eventId,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        )
        {
            return rows != null && rows.Any(row => CanClaim(eventId, row));
        }

        /// <summary>Nhận tất cả phần thưởng khả dụng của một milestone.</summary>
        public EventWheelPassClaimResult ClaimMilestone(
            int eventId,
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row
        )
        {
            var result = new EventWheelPassClaimResult();

            if (row == null ||
                row.eventId != eventId)
            {
                return result;
            }

            var progress = GetOrCreateProgress(eventId);
            result.Changed = CollectClaimableRewards(progress, row, result.Rewards);

            if (result.Changed)
            {
                _storage.Save();
            }

            return result;
        }

        /// <summary>Nhận tất cả phần thưởng khả dụng trong danh sách pass.</summary>
        public EventWheelPassClaimResult ClaimAll(
            int eventId,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        )
        {
            var result = new EventWheelPassClaimResult();

            if (rows == null)
            {
                return result;
            }

            var progress = GetOrCreateProgress(eventId);

            foreach (var row in rows)
            {
                if (row != null &&
                    row.eventId == eventId)
                {
                    result.Changed |= CollectClaimableRewards(progress, row, result.Rewards);
                }
            }

            if (result.Changed)
            {
                _storage.Save();
            }

            return result;
        }

        /// <summary>Xóa toàn bộ tiến trình pass của sự kiện và trả về dữ liệu có thay đổi hay không.</summary>
        public bool ResetEvent(int eventId)
        {
            if (!_storage.Data.Events.Remove(eventId))
            {
                return false;
            }

            _storage.Save();
            return true;
        }

        /// <summary>Thu thập các phần thưởng miễn phí và Premium mới có thể nhận của một milestone.</summary>
        private static bool CollectClaimableRewards(
            EventWheelPassProgressData progress,
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row,
            ICollection<ItemRewardData> rewards
        )
        {
            if (progress.PurchasedSpinCount < row.spinRequired)
            {
                return false;
            }

            var changed = false;

            if (!progress.ClaimedFreeMilestoneIds.Contains(row.milestoneId))
            {
                progress.ClaimedFreeMilestoneIds.Add(row.milestoneId);
                AddRewards(row.freeItem, rewards);
                changed = true;
            }

            if (progress.PremiumPurchased &&
                !progress.ClaimedPremiumMilestoneIds.Contains(row.milestoneId))
            {
                progress.ClaimedPremiumMilestoneIds.Add(row.milestoneId);
                AddRewards(row.paidItem, rewards);
                changed = true;
            }

            return changed;
        }

        /// <summary>Phân tích chuỗi cấu hình reward và thêm các phần thưởng runtime vào kết quả.</summary>
        private static void AddRewards(string rewardConfig, ICollection<ItemRewardData> output)
        {
            var rewards = DatabaseManager.Instance.GetRewards(rewardConfig);

            foreach (var reward in rewards)
            {
                output.Add(reward);
            }
        }

        /// <summary>Lấy tiến trình của sự kiện hoặc tạo mới và chuẩn hóa dữ liệu nếu chưa tồn tại.</summary>
        private EventWheelPassProgressData GetOrCreateProgress(int eventId)
        {
            _storage.Data.Events ??= new Dictionary<int, EventWheelPassProgressData>();

            if (!_storage.Data.Events.TryGetValue(eventId, out var progress) ||
                progress == null)
            {
                progress = new EventWheelPassProgressData();
                _storage.Data.Events[eventId] = progress;
            }

            progress.ClaimedFreeMilestoneIds ??= new List<int>();
            progress.ClaimedPremiumMilestoneIds ??= new List<int>();
            progress.AccountShopPurchaseCounts ??= new Dictionary<int, int>();
            progress.DailyShopPurchaseCounts ??= new Dictionary<int, int>();
            return progress;
        }

        /// <summary>Trả về bộ đếm lượt mua tương ứng với loại giới hạn shop.</summary>
        private static Dictionary<int, int> GetShopPurchaseCounts(
            EventWheelPassProgressData progress,
            int limitType
        )
        {
            return (EEventWheelShopLimitType)limitType switch
            {
                EEventWheelShopLimitType.Account => progress.AccountShopPurchaseCounts,
                EEventWheelShopLimitType.Daily => progress.DailyShopPurchaseCounts,
                _ => null,
            };
        }

        /// <summary>Reset bộ đếm giới hạn mua hằng ngày khi đã sang ngày UTC mới.</summary>
        private void CheckAndResetDailyShop(EventWheelPassProgressData progress)
        {
            var now = DateTime.UtcNow;

            if (progress.DailyShopResetDate.HasValue &&
                !DateTimeHelper.IsNewDay(progress.DailyShopResetDate.Value))
            {
                return;
            }

            progress.DailyShopResetDate = now;
            progress.DailyShopPurchaseCounts.Clear();
            _storage.Save();
        }
    }
}