using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── shared ────────────────────────────────────────────────────────────────────

[Serializable]
public class EventWheelItemDto
{
    [JsonProperty("item_id")]   public int    ItemId;
    [JsonProperty("item_key")]  public string ItemKey;
    [JsonProperty("item_name")] public string ItemName;
    [JsonProperty("icon_key")]  public string IconKey;
    [JsonProperty("amount")]    public int    Amount;
}

// ── eventwheel/state ─────────────────────────────────────────────────────────

[Serializable]
public class EventWheelRewardSlotDto
{
    [JsonProperty("slot_index")]   public int              SlotIndex;
    [JsonProperty("is_featured")]  public bool             IsFeatured;
    [JsonProperty("rate_display")] public string            RateDisplay;
    [JsonProperty("item")]         public EventWheelItemDto Item;
}

[Serializable]
public class EventWheelRewardsPoolDto
{
    [JsonProperty("normal")]  public List<EventWheelRewardSlotDto> Normal;
    [JsonProperty("premium")] public List<EventWheelRewardSlotDto> Premium;
}

[Serializable]
public class EventWheelShopSlotDto
{
    [JsonProperty("shop_slot_id")]    public int              ShopSlotId;
    [JsonProperty("item")]            public EventWheelItemDto Item;
    [JsonProperty("price_point")]     public int              PricePoint;
    /// <summary>0 = Account (lifetime), 1 = Daily — matches EEventWheelShopLimitType.</summary>
    [JsonProperty("limit_type")]      public int              LimitType;
    [JsonProperty("limit_value")]     public int              LimitValue;
    [JsonProperty("purchased_count")] public int              PurchasedCount;
    [JsonProperty("sort_order")]      public int              SortOrder;
}

[Serializable]
public class EventWheelPassMilestoneDto
{
    [JsonProperty("milestone_id")]  public int              MilestoneId;
    [JsonProperty("level")]         public int              Level;
    [JsonProperty("spin_required")] public int              SpinRequired;
    [JsonProperty("is_big_node")]   public bool             IsBigNode;
    [JsonProperty("free_item")]     public EventWheelItemDto FreeItem;
    [JsonProperty("paid_item")]     public EventWheelItemDto PaidItem;
    [JsonProperty("free_claimed")]  public bool             FreeClaimed;
    [JsonProperty("paid_claimed")]  public bool             PaidClaimed;
    [JsonProperty("is_eligible")]   public bool             IsEligible;
}

[Serializable]
public class EventWheelProgressDto
{
    [JsonProperty("spin_total")]             public int  SpinTotal;
    [JsonProperty("spin_cost_per_ticket")]   public int  SpinCostPerTicket;
    [JsonProperty("normal_ticket_balance")]  public int  NormalTicketBalance;
    [JsonProperty("premium_ticket_balance")] public int  PremiumTicketBalance;
    [JsonProperty("point_balance")]          public int  PointBalance;
    [JsonProperty("is_premium")]             public bool IsPremium;
}

/// <summary>Gói Premium Pass để mua (game_config_pass_event.js) — null nếu chưa cấu hình.
/// google_product_id/apple_product_id là nguồn đúng để resolve storeProductId khi mua, KHÔNG
/// dùng DatabaseManager.GetEventPassConfig cục bộ (đã disable — xem EventWheelView).</summary>
[Serializable]
public class EventWheelPremiumPackDto
{
    [JsonProperty("id")]                 public int    Id;
    [JsonProperty("product_id")]         public int    ProductId;
    [JsonProperty("name")]               public string Name;
    [JsonProperty("price_usd")]          public string PriceUsd;
    [JsonProperty("google_product_id")]  public string GoogleProductId;
    [JsonProperty("apple_product_id")]   public string AppleProductId;
}

/// <summary>Cửa sổ thời gian sự kiện (game_config_event.js) — *_ms là Unix epoch ms (UTC thật,
/// đã quy đổi từ giờ tường thuật UTC+7 phía server). Null nếu config thiếu/không Active.</summary>
[Serializable]
public class EventWheelWindowDto
{
    [JsonProperty("is_wheel_active")] public bool  IsWheelActive;
    [JsonProperty("is_shop_active")]  public bool  IsShopActive;
    [JsonProperty("wheel_start_ms")]  public long? WheelStartMs;
    [JsonProperty("wheel_end_ms")]    public long? WheelEndMs;
    [JsonProperty("shop_end_ms")]     public long? ShopEndMs;
}

[Serializable]
public class EventWheelStateResponse
{
    [JsonProperty("event_id")]     public int                          EventId;
    [JsonProperty("window")]       public EventWheelWindowDto          Window;
    [JsonProperty("rewards_pool")] public EventWheelRewardsPoolDto     RewardsPool;
    [JsonProperty("shop")]         public List<EventWheelShopSlotDto> Shop;
    [JsonProperty("pass")]         public List<EventWheelPassMilestoneDto> Pass;
    [JsonProperty("premium_pack")] public EventWheelPremiumPackDto     PremiumPack;
    [JsonProperty("progress")]     public EventWheelProgressDto       Progress;
}

// ── eventwheel/spin ──────────────────────────────────────────────────────────

[Serializable]
public class EventWheelSpinRequest
{
    /// <summary>1 = normal, 2 = premium — matches EEventCategory.</summary>
    [JsonProperty("category")] public int Category;
    /// <summary>1 or 10 — matches the client's x1/x10 buttons, server rejects other values.</summary>
    [JsonProperty("times")]    public int Times;
}

[Serializable]
public class EventWheelSpinEntryDto
{
    [JsonProperty("roll_index")]  public int              RollIndex;
    [JsonProperty("slot_index")]  public int              SlotIndex;
    [JsonProperty("is_featured")] public bool             IsFeatured;
    [JsonProperty("item")]        public EventWheelItemDto Item;
}

/// <summary>Response từ eventwheel/spin. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventWheelSpinResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"EVENT_NOT_ACTIVE" | "INVALID_CATEGORY" | "INVALID_TIMES" | "POOL_EMPTY" | "INSUFFICIENT_TICKET"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("category")]     public int  Category;
    [JsonProperty("times")]        public int  Times;
    [JsonProperty("ticket_spent")] public int  TicketSpent;
    [JsonProperty("spin_total")]   public int  SpinTotal;

    /// <summary>1 entry per spin, in landing order — drives the wheel's stop animation.</summary>
    [JsonProperty("entries")]  public List<EventWheelSpinEntryDto> Entries;
    /// <summary>Absolute post-transaction balances (ticket spent + every rewarded item_key) —
    /// apply via CurrencyManager.Instance.ApplyServerBalances(Balances).</summary>
    [JsonProperty("balances")] public List<RewardDto> Balances;
}

// ── eventwheel/shop_buy ──────────────────────────────────────────────────────

[Serializable]
public class EventWheelShopBuyRequest
{
    [JsonProperty("shop_slot_id")] public int ShopSlotId;
}

/// <summary>Response từ eventwheel/shop_buy. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventWheelShopBuyResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"EVENT_NOT_ACTIVE" | "SHOP_SLOT_NOT_FOUND" | "LIMIT_REACHED" | "INSUFFICIENT_POINT"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("shop_slot_id")]    public int              ShopSlotId;
    [JsonProperty("item")]            public EventWheelItemDto Item;
    [JsonProperty("point_spent")]     public int              PointSpent;
    [JsonProperty("purchased_count")] public int              PurchasedCount;
    [JsonProperty("limit_value")]     public int              LimitValue;
    [JsonProperty("balances")]        public List<RewardDto>  Balances;
}

// ── eventwheel/pass_claim ────────────────────────────────────────────────────

[Serializable]
public class EventWheelPassClaimRequest
{
    [JsonProperty("level")] public int    Level;
    /// <summary>"free" (default, có thể bỏ qua) | "paid" — "paid" cần is_premium = true.</summary>
    [JsonProperty("track")] public string Track;
}

/// <summary>Response từ eventwheel/pass_claim. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventWheelPassClaimResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"EVENT_NOT_ACTIVE" | "MILESTONE_NOT_FOUND" | "REWARD_NOT_CONFIGURED" | "PREMIUM_REQUIRED" | "NOT_YET_ELIGIBLE" | "ALREADY_CLAIMED"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("level")]    public int              Level;
    [JsonProperty("track")]    public string           Track;
    [JsonProperty("item")]     public EventWheelItemDto Item;
    [JsonProperty("balances")] public List<RewardDto>  Balances;
}

// ── eventwheel/pass_buy_premium ──────────────────────────────────────────────

[Serializable]
public class EventWheelPassBuyPremiumRequest
{
    [JsonProperty("store")]   public string Store;
    [JsonProperty("receipt")] public string Receipt;
}

/// <summary>Response từ eventwheel/pass_buy_premium. Khi Success = false, chỉ Error có giá trị —
/// KHÔNG confirm pending purchase phía client trong trường hợp đó (xem IAPManager.ValidateAndConfirmAsync).</summary>
[Serializable]
public class EventWheelPassBuyPremiumResponse
{
    [JsonProperty("success")]    public bool   Success;
    /// <summary>"INVALID_STORE" | "EVENT_NOT_ACTIVE" | "PASS_NOT_CONFIGURED" | "ALREADY_PURCHASED" | "PRODUCT_NOT_CONFIGURED" | "VALIDATION_FAILED"</summary>
    [JsonProperty("error")]      public string Error;
    [JsonProperty("is_premium")] public bool   IsPremium;
}
