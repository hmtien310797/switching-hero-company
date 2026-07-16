using System.Collections.Generic;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Event.EventWheel.Models;

namespace Immortal_Switch.Scripts.Event.EventWheel.Interfaces
{
    /// <summary>
    /// Định nghĩa các nghiệp vụ của Event Wheel Pass.
    /// </summary>
    public interface IEventWheelPassService
    {
        /// <summary>Lấy tổng số lượt quay đã mua của một sự kiện.</summary>
        int GetPurchasedSpinCount(int eventId);

        /// <summary>Ghi nhận số lượt quay vừa mua và trả về dữ liệu có thay đổi hay không.</summary>
        bool RecordSpinPurchase(int eventId, int amount);

        /// <summary>Lấy số lần đã mua của một ô shop theo loại giới hạn trong cấu hình.</summary>
        int GetShopPurchasedCount(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row);

        /// <summary>Lấy số lượt mua còn lại của một ô shop.</summary>
        int GetShopRemaining(DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row);

        /// <summary>Kiểm tra ô shop có thể mua thêm số lượt yêu cầu hay không.</summary>
        bool CanPurchaseShopItem(
            DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row,
            int purchaseCount = 1
        );

        /// <summary>Ghi nhận lượt mua shop nếu chưa vượt giới hạn và trả về thao tác có thành công hay không.</summary>
        bool RecordShopPurchase(
            DynamicHeroesGlobalSpecificationsEventWheelShopConfigRow row,
            int purchaseCount = 1
        );

        /// <summary>Kiểm tra Premium Pass của sự kiện đã được mua hay chưa.</summary>
        bool IsPremiumPurchased(int eventId);

        /// <summary>Đánh dấu Premium Pass đã được mua và trả về dữ liệu có thay đổi hay không.</summary>
        bool PurchasePremium(int eventId);

        /// <summary>Kiểm tra phần thưởng miễn phí của milestone đã được nhận hay chưa.</summary>
        bool IsFreeClaimed(int eventId, int milestoneId);

        /// <summary>Kiểm tra phần thưởng Premium của milestone đã được nhận hay chưa.</summary>
        bool IsPremiumClaimed(int eventId, int milestoneId);

        /// <summary>Kiểm tra milestone có ít nhất một phần thưởng có thể nhận hay không.</summary>
        bool CanClaim(int eventId, DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row);

        /// <summary>Kiểm tra danh sách pass có milestone nào có thể nhận thưởng hay không.</summary>
        bool HasClaimable(
            int eventId,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        );

        /// <summary>Nhận tất cả phần thưởng khả dụng của một milestone.</summary>
        EventWheelPassClaimResult ClaimMilestone(
            int eventId,
            DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow row
        );

        /// <summary>Nhận tất cả phần thưởng khả dụng trong danh sách pass.</summary>
        EventWheelPassClaimResult ClaimAll(
            int eventId,
            IReadOnlyList<DynamicHeroesGlobalSpecificationsEventWheelPassConfigRow> rows
        );

        /// <summary>Xóa toàn bộ tiến trình pass của sự kiện và trả về dữ liệu có thay đổi hay không.</summary>
        bool ResetEvent(int eventId);
    }
}