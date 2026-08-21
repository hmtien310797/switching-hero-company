using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.Items.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;
using Newtonsoft.Json;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Một item trong PvP Shop: combine config từ <c>PvpShopInfo</c> + số lần đã mua trong tuần
    /// hiện tại. Response cho PvP Shop view (server contract) — shape khớp JSON server sẽ trả;
    /// client Phase-1 dùng <see cref="LocalPvPShopService"/> để mock.
    /// </summary>
    [Serializable]
    public sealed class PvpShopItemModel
    {
        [JsonProperty("shopItemId")] public int ShopItemId;
        [JsonProperty("rewardItemId")] public int RewardItemId;
        [JsonProperty("rewardQuantity")] public int RewardQuantity;
        [JsonProperty("paymentCurrencyId")] public int PaymentCurrencyId;
        [JsonProperty("paymentAmount")] public int PaymentAmount;
        /// <summary>Giới hạn mua trong tuần — {1} của text "0/5".</summary>
        [JsonProperty("maxPurchasePerWeek")] public int MaxPurchasePerWeek;
        /// <summary>Số lần ĐÃ MUA trong tuần — {0} của text "0/5". <b>TODO server:</b> server trả.</summary>
        [JsonProperty("purchasedCount")] public int PurchasedCount;
    }

    /// <summary>Response toàn shop: thời gian cho tới lần refresh + danh sách item.</summary>
    [Serializable]
    public sealed class PvpShopDataModel
    {
        [JsonProperty("serverTimeUtc")] public long ServerTimeUtc;

        /// <summary>Thời điểm (Unix s) toàn bộ shop refresh. <b>TODO server:</b> server trả.</summary>
        [JsonProperty("refreshAtUtc")] public long RefreshAtUtc;

        [JsonProperty("items")] public List<PvpShopItemModel> Items = new();
    }

    /// <summary>Kết quả mua item. Server thật sẽ trừ payment currency + kiểm duyệt ở server.</summary>
    public sealed class PvpShopBuyResult
    {
        public bool Success;
        public string Error;
        public List<ItemData> Rewards = new();
    }

    /// <summary>
    /// Data-group root (ES3) lưu số lần đã mua theo item trong kỳ hiện tại (mock local).
    /// Phase-2 server quản lý hoàn toàn — chỉ còn dùng để mock test.
    /// </summary>
    [Serializable]
    public sealed class PvpShopPurchaseData : IPvPSaveData
    {
        public int SchemaVersion { get; set; } = 1;

        /// <summary>shopItemId -> số lần đã mua trong kỳ hiện tại.</summary>
        public Dictionary<int, int> Items = new();
    }
}