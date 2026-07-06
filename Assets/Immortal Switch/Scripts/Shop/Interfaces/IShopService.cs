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
    }
}
