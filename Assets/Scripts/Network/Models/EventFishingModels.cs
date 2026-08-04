using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── shared ────────────────────────────────────────────────────────────────────

[Serializable]
public class EventFishingItemDto
{
    [JsonProperty("item_id")]   public int    ItemId;
    [JsonProperty("item_key")]  public string ItemKey;
    [JsonProperty("item_name")] public string ItemName;
    [JsonProperty("icon_key")]  public string IconKey;
    [JsonProperty("amount")]    public int    Amount;
}

// ── eventfishing/state ───────────────────────────────────────────────────────

/// <summary>Cửa sổ thời gian sự kiện (game_config_event.js) — *_ms là Unix epoch ms (UTC thật,
/// đã quy đổi từ giờ tường thuật UTC+7 phía server). Null nếu config thiếu/không Active.</summary>
[Serializable]
public class EventFishingWindowDto
{
    [JsonProperty("is_cast_active")] public bool  IsCastActive;
    [JsonProperty("is_shop_active")] public bool  IsShopActive;
    [JsonProperty("start_ms")]       public long? StartMs;
    [JsonProperty("end_ms")]         public long? EndMs;
    [JsonProperty("shop_end_ms")]    public long? ShopEndMs;
}

[Serializable]
public class EventFishingCollectionEntryDto
{
    [JsonProperty("collection_id")] public int    CollectionId;
    [JsonProperty("fish_icon")]     public string FishIcon;
    [JsonProperty("icon_rarity")]   public string IconRarity;
    [JsonProperty("caught")]        public bool   Caught;
}

[Serializable]
public class EventFishingShopEntryDto
{
    [JsonProperty("shop_id")]    public int                 ShopId;
    [JsonProperty("price")]      public int                 Price;
    [JsonProperty("is_premium")] public bool                IsPremium;
    /// <summary>item_key của loại tiền bị trừ — "gold" (is_premium=false) hoặc "diamond" (is_premium=true). Cả 2 dòng đều cấp thưởng fishing_food (mồi câu).</summary>
    [JsonProperty("currency")]   public string              Currency;
    [JsonProperty("item")]       public EventFishingItemDto Item;
}

[Serializable]
public class EventFishingProgressDto
{
    [JsonProperty("caught_count")]    public int CaughtCount;
    [JsonProperty("collection_size")] public int CollectionSize;
    [JsonProperty("bait_balance")]    public int BaitBalance;
    [JsonProperty("cast_cost")]       public int CastCost;
}

[Serializable]
public class EventFishingStateResponse
{
    [JsonProperty("event_id")]   public int                                  EventId;
    [JsonProperty("window")]     public EventFishingWindowDto               Window;
    [JsonProperty("collection")] public List<EventFishingCollectionEntryDto> Collection;
    [JsonProperty("shop")]       public List<EventFishingShopEntryDto>      Shop;
    [JsonProperty("progress")]   public EventFishingProgressDto             Progress;
}

// ── eventfishing/cast ────────────────────────────────────────────────────────

[Serializable]
public class EventFishingFishDto
{
    [JsonProperty("collection_id")] public int    CollectionId;
    [JsonProperty("fish_icon")]     public string FishIcon;
    [JsonProperty("icon_rarity")]   public string IconRarity;
}

/// <summary>Response từ eventfishing/cast. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventFishingCastResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"EVENT_NOT_ACTIVE" | "COLLECTION_EMPTY" | "INSUFFICIENT_BAIT"</summary>
    [JsonProperty("error")]   public string Error;

    /// <summary>"win" | "lose" — server tự random 50/50, client chỉ hiển thị kết quả.</summary>
    [JsonProperty("result")]         public string              Result;
    /// <summary>null khi Result = "lose".</summary>
    [JsonProperty("fish")]           public EventFishingFishDto Fish;
    [JsonProperty("is_first_catch")] public bool                IsFirstCatch;
    [JsonProperty("balances")]       public List<RewardDto>     Balances;
}

// ── eventfishing/shop_buy ────────────────────────────────────────────────────

[Serializable]
public class EventFishingShopBuyRequest
{
    [JsonProperty("shop_id")] public int ShopId;
}

/// <summary>Response từ eventfishing/shop_buy. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventFishingShopBuyResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"EVENT_NOT_ACTIVE" | "SHOP_ITEM_NOT_FOUND" | "REWARD_NOT_CONFIGURED" | "INSUFFICIENT_CURRENCY"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("shop_id")]  public int                 ShopId;
    [JsonProperty("item")]     public EventFishingItemDto Item;
    [JsonProperty("spent")]    public int                 Spent;
    [JsonProperty("currency")] public string              Currency;
    [JsonProperty("balances")] public List<RewardDto>     Balances;
}
