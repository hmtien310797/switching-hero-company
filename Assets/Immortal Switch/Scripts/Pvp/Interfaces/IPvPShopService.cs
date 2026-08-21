using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// PvP Shop service. Phase-1 dùng <c>LocalPvPShopService</c> (mock — đọc config local + persist
    /// số lần mua vào repository). Khi server làm xong, thay bằng impl gọi RPC (server trả
    /// <see cref="PvpShopDataModel.RefreshAtUtc"/> + <see cref="PvpShopItemModel.PurchasedCount"/>
    /// thật và xử lý trừ currency ở buy) — view không đổi.
    /// </summary>
    public interface IPvPShopService
    {
        /// <summary>Lấy toàn bộ dữ liệu PvP shop (thời gian refresh + danh sách item).</summary>
        UniTask<PvpShopDataModel> GetShopAsync(CancellationToken token);

        /// <summary>
        /// Mua <paramref name="quantity"/> lần item shop theo <paramref name="shopItemId"/>.
        /// <b>TODO server:</b> server kiểm duyệt (số dư + giới hạn tuần) và trừ payment currency.
        /// </summary>
        UniTask<PvpShopBuyResult> BuyAsync(int shopItemId, int quantity, CancellationToken token);
    }
}