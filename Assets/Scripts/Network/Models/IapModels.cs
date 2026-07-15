using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class IapPurchaseRequest
{
    [JsonProperty("pack_id")] public int    PackId;
    [JsonProperty("store")]   public string Store;
    [JsonProperty("receipt")] public string Receipt;
}

/// <summary>Response từ iap/purchase.</summary>
[Serializable]
public class IapPurchaseResponse
{
    [JsonProperty("pack_id")]      public int  PackId;
    [JsonProperty("item_id")]      public int  ItemId;
    [JsonProperty("gems_granted")] public int  GemsGranted;
    [JsonProperty("is_first_buy")] public bool IsFirstBuy;
    [JsonProperty("balance")]      public int  Balance;
}

/// <summary>Request cho iap/pack_purchase — cùng field với iap/purchase, khác pack_id thuộc
/// pack_iap (bundle nhiều item) thay vì pack_diamond (1 loại tiền).</summary>
[Serializable]
public class IapPackPurchaseRequest
{
    [JsonProperty("pack_id")] public int    PackId;
    [JsonProperty("store")]   public string Store;
    [JsonProperty("receipt")] public string Receipt;
}

/// <summary>Response từ iap/pack_purchase. Rewards/Balances dùng chung RewardDto (currency_type/amount)
/// như recharge/claim — CurrencyManager.ApplyServerBalances(Balances) áp thẳng được cho các item
/// có CurrencyType tương ứng (gold/diamond/crystal...); item không thuộc CurrencyType (material,
/// ticket...) đọc lại qua player/me khi màn hình liên quan mở lại, không cache riêng ở đây.</summary>
[Serializable]
public class IapPackPurchaseResponse
{
    [JsonProperty("pack_id")]        public int              PackId;
    [JsonProperty("rewards")]        public List<RewardDto>  Rewards;
    [JsonProperty("balances")]       public List<RewardDto>  Balances;
    [JsonProperty("purchase_count")] public int              PurchaseCount;
    [JsonProperty("limit")]          public int              Limit;
    [JsonProperty("limit_reset")]    public string           LimitReset;
}
