using System;
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
