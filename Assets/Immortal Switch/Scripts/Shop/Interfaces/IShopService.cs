using System;

namespace Immortal_Switch.Scripts.Shop.Interfaces
{
    public interface IShopService
    {
        /// <summary>Số lượt mua còn lại. limit &lt;= 0 → không giới hạn.</summary>
        int GetRemaining(int packId, int limitFromConfig);

        /// <summary>Số lần đã mua của 1 pack.</summary>
        int GetPurchasedCount(int packId);

        /// <summary>Tăng số lần mua, gọi sau khi IAP thành công.</summary>
        void RecordPurchase(int packId);

        /// <summary>Kiểm tra daily/weekly/monthly reset, xoá purchase count tương ứng.</summary>
        void CheckAndReset();

        // ── Topup / GloryPass ────────────────────────────────────────────────

        /// <summary>Tổng số lượt nạp (mỗi lần mua diamond +1).</summary>
        int GetTopupCount();

        /// <summary>Tăng 1 lượt nạp sau khi mua diamond thành công.</summary>
        void RecordTopup();

        /// <summary>Kiểm tra milestone đã nhận thưởng chưa.</summary>
        bool IsGloryPassClaimed(int milestoneId);

        /// <summary>Đánh dấu milestone đã nhận thưởng.</summary>
        void ClaimGloryPass(int milestoneId);

        /// <summary>Reset danh sách GloryPass đã nhận đầu tháng.</summary>
        void ResetGloryPassClaims();

        // ── Monthly Pass ──────────────────────────────────────────────────────

        /// <summary>Đã mua monthly pass packId chưa.</summary>
        bool IsMonthlyPassPurchased(int packId);

        /// <summary>Ngày mua monthly pass.</summary>
        DateTime? GetMonthlyPassPurchaseDate(int packId);

        /// <summary>Ngày hiện tại tính từ lúc mua (1-based).</summary>
        int GetMonthlyPassCurrentDay(int packId);

        /// <summary>Kiểm tra ngày thứ day đã nhận thưởng chưa.</summary>
        bool IsMonthlyPassDayClaimed(int packId, int day);

        /// <summary>Đánh dấu đã nhận thưởng cho ngày thứ day.</summary>
        void ClaimMonthlyPassDay(int packId, int day);

        /// <summary>Ghi nhận giao dịch mua monthly pass.</summary>
        void PurchaseMonthlyPass(int packId);

    }
}
