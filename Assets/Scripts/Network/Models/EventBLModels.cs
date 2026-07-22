using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── shared ────────────────────────────────────────────────────────────────────
// Reuses EventWheelItemDto (item_id/item_key/item_name/icon_key/amount) — identical shape,
// see EventWheelModels.cs.

/// <summary>Một phần thưởng của event Lễ Hội Băng Long — reward_type "ITEM" (Item có giá trị) hoặc
/// "HERO" (hero_id/hero_name/rarity có giá trị, Item null). Chỉ 1 dòng trong event_BL_rate.js là
/// HERO (jackpot); mọi dòng check_in/check_in2/mission/milestone khác luôn là ITEM.</summary>
[Serializable]
public class EventBLRewardDto
{
    [JsonProperty("reward_type")] public string           RewardType;
    [JsonProperty("item")]        public EventWheelItemDto Item;
    [JsonProperty("hero_id")]     public int              HeroId;
    [JsonProperty("hero_name")]   public string           HeroName;
    [JsonProperty("rarity")]      public string           Rarity;
    [JsonProperty("amount")]      public int              Amount;

    public bool IsHero => RewardType == "HERO";

    /// <summary>Số lượng thực (item.amount cho ITEM, Amount cho HERO — luôn 1).</summary>
    public int EffectiveAmount => Item != null ? Item.Amount : Amount;
}

// ── eventbl/state ─────────────────────────────────────────────────────────────

[Serializable]
public class EventBLProgressDto
{
    [JsonProperty("login_day")]      public int LoginDay;
    [JsonProperty("mission_points")] public int MissionPoints;
    [JsonProperty("summon_points")]  public int SummonPoints;
}

[Serializable]
public class EventBLCheckInDto
{
    [JsonProperty("day")]     public int             Day;
    [JsonProperty("reward")]  public EventBLRewardDto Reward;
    [JsonProperty("claimed")] public bool             Claimed;
}

/// <summary>Bonus theo ngày (game_event_BL_check_in2.js). InstantRewards là track miễn phí —
/// cấp thật qua eventbl/claim_bonus_free. Track trả phí (PackId, 35-41) resolve qua server's
/// pack_event config (cùng shape pack_iap) và được cấp thật qua iap/pack_purchase khi mua —
/// xem EventLeHoiBangLongManager.RequestBonusPurchase.</summary>
[Serializable]
public class EventBLCheckInBonusDto
{
    [JsonProperty("day")]                public int                      Day;
    [JsonProperty("instant_rewards")]    public List<EventBLRewardDto>   InstantRewards;
    [JsonProperty("pack_id")]            public int                      PackId;
    [JsonProperty("claimed_free_bonus")] public bool                     ClaimedFreeBonus;
    [JsonProperty("purchased_bonus")]    public bool                     PurchasedBonus;
    [JsonProperty("claimed_bonus")]      public bool                     ClaimedBonus;
}

[Serializable]
public class EventBLMissionDto
{
    [JsonProperty("mission_id")] public string           MissionId;
    [JsonProperty("title_vi")]   public string           TitleVi;
    [JsonProperty("trigger")]    public string           Trigger;
    [JsonProperty("target")]     public int              Target;
    [JsonProperty("points")]     public int              Points;
    [JsonProperty("reward")]     public EventWheelItemDto Reward;
    [JsonProperty("sort_order")] public int              SortOrder;
    [JsonProperty("progress")]   public int              Progress;
    [JsonProperty("is_claimed")] public bool             IsClaimed;
}

[Serializable]
public class EventBLMilestoneDto
{
    [JsonProperty("milestone")]       public int              Milestone;
    [JsonProperty("points_required")] public int              PointsRequired;
    [JsonProperty("reward")]          public EventWheelItemDto Reward;
    [JsonProperty("is_claimed")]      public bool             IsClaimed;
}

[Serializable]
public class EventBLGachaRateDto
{
    [JsonProperty("reward")]       public EventBLRewardDto Reward;
    [JsonProperty("rate_percent")] public double           RatePercent;
    [JsonProperty("group")]        public string           Group;
    [JsonProperty("is_jackpot")]   public bool             IsJackpot;
}

[Serializable]
public class EventBLStateResponse
{
    [JsonProperty("progress")]           public EventBLProgressDto            Progress;
    [JsonProperty("check_in")]           public List<EventBLCheckInDto>       CheckIn;
    [JsonProperty("check_in_bonus")]     public List<EventBLCheckInBonusDto>  CheckInBonus;
    [JsonProperty("missions")]           public List<EventBLMissionDto>       Missions;
    [JsonProperty("mission_milestones")] public List<EventBLMilestoneDto>     MissionMilestones;
    [JsonProperty("summon_milestones")]  public List<EventBLMilestoneDto>     SummonMilestones;
    [JsonProperty("gacha_rates")]        public List<EventBLGachaRateDto>     GachaRates;
}

// ── eventbl/mission_progress ──────────────────────────────────────────────────

[Serializable]
public class EventBLMissionProgressRequest
{
    [JsonProperty("trigger")] public string Trigger;
    [JsonProperty("value")]   public int    Value;
}

[Serializable]
public class EventBLMissionProgressResponse
{
    [JsonProperty("success")] public bool   Success;
    [JsonProperty("error")]   public string Error;
}

// ── eventbl/claim_login ───────────────────────────────────────────────────────

[Serializable]
public class EventBLClaimLoginRequest
{
    [JsonProperty("day")] public int Day;
}

/// <summary>Response từ eventbl/claim_login. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventBLClaimLoginResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_DAY" | "NOT_YET_ELIGIBLE" | "ALREADY_CLAIMED" | "CONFIG_NOT_FOUND"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("day")]      public int              Day;
    [JsonProperty("reward")]   public EventBLRewardDto Reward;
    [JsonProperty("balances")] public List<RewardDto>  Balances;
}

// ── eventbl/claim_bonus_free ──────────────────────────────────────────────────

[Serializable]
public class EventBLClaimBonusFreeRequest
{
    [JsonProperty("day")] public int Day;
}

[Serializable]
public class EventBLClaimBonusFreeResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_DAY" | "NOT_YET_ELIGIBLE" | "ALREADY_CLAIMED" | "CONFIG_NOT_FOUND"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("day")]      public int                    Day;
    [JsonProperty("rewards")]  public List<EventBLRewardDto> Rewards;
    [JsonProperty("balances")] public List<RewardDto>        Balances;
}

// ── eventbl/confirm_bonus_purchase, eventbl/claim_bonus_paid ─────────────────
// Cả 2 chỉ là bookkeeping (đánh dấu trạng thái nút mua/nhận) — vật phẩm của gói trả phí đã được
// cấp thật ngay khi mua thành công qua iap/pack_purchase, xem comment EventBLCheckInBonusDto.

[Serializable]
public class EventBLDayRequest
{
    [JsonProperty("day")] public int Day;
}

[Serializable]
public class EventBLDayResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>confirm_bonus_purchase: "INVALID_DAY" | "FREE_BONUS_NOT_CLAIMED" | "ALREADY_PURCHASED"
    /// claim_bonus_paid: "INVALID_DAY" | "NOT_PURCHASED" | "ALREADY_CLAIMED"</summary>
    [JsonProperty("error")]   public string Error;
    [JsonProperty("day")]     public int    Day;
}

// ── eventbl/claim_mission ─────────────────────────────────────────────────────

[Serializable]
public class EventBLClaimMissionRequest
{
    [JsonProperty("mission_id")] public string MissionId;
}

[Serializable]
public class EventBLClaimMissionResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"NOT_FOUND" | "ALREADY_CLAIMED" | "CONFIG_NOT_FOUND" | "NOT_ENOUGH_PROGRESS"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("mission_id")]     public string            MissionId;
    [JsonProperty("mission_points")] public int               MissionPoints;
    [JsonProperty("reward")]         public EventWheelItemDto Reward;
    [JsonProperty("balances")]       public List<RewardDto>   Balances;
}

// ── eventbl/claim_mission_milestone, eventbl/claim_summon_milestone ──────────

[Serializable]
public class EventBLMilestoneRequest
{
    [JsonProperty("milestone")] public int Milestone;
}

[Serializable]
public class EventBLMilestoneResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"ALREADY_CLAIMED" | "CONFIG_NOT_FOUND" | "NOT_ENOUGH_POINTS"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("milestone")] public int              Milestone;
    [JsonProperty("reward")]    public EventWheelItemDto Reward;
    [JsonProperty("balances")]  public List<RewardDto>  Balances;
}

// ── eventbl/summon ─────────────────────────────────────────────────────────────

[Serializable]
public class EventBLSummonRequest
{
    /// <summary>1 hoặc 10 — khớp nút x1/x10, server từ chối giá trị khác.</summary>
    [JsonProperty("times")] public int Times;
}

[Serializable]
public class EventBLSummonEntryDto
{
    [JsonProperty("roll_index")] public int              RollIndex;
    [JsonProperty("reward")]     public EventBLRewardDto Reward;
    [JsonProperty("is_jackpot")] public bool             IsJackpot;
}

/// <summary>Response từ eventbl/summon. Trừ 1x item_id 47 (summon_ticket_hero_banner) mỗi lượt
/// (thêm 2026-07-22) — trước đó không trừ gì, port nguyên bản theo quyết định sản phẩm ban đầu.</summary>
[Serializable]
public class EventBLSummonResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_TIMES" | "POOL_EMPTY" | "INSUFFICIENT_TICKET"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("times")]         public int                          Times;
    [JsonProperty("ticket_spent")]  public int                          TicketSpent;
    [JsonProperty("entries")]       public List<EventBLSummonEntryDto> Entries;
    [JsonProperty("summon_points")] public int                          SummonPoints;
    [JsonProperty("balances")]      public List<RewardDto>             Balances;
}
