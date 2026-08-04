using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── shared ────────────────────────────────────────────────────────────────────

[Serializable]
public class EventDiceItemDto
{
    [JsonProperty("item_id")]   public int    ItemId;
    [JsonProperty("item_key")]  public string ItemKey;
    [JsonProperty("item_name")] public string ItemName;
    [JsonProperty("icon_key")]  public string IconKey;
    [JsonProperty("amount")]    public int    Amount;
}

// ── eventdice/state ──────────────────────────────────────────────────────────

/// <summary>Cửa sổ thời gian sự kiện (game_config_event.js) — *_ms là Unix epoch ms (UTC thật,
/// đã quy đổi từ giờ tường thuật UTC+7 phía server). Null nếu config thiếu/không Active.</summary>
[Serializable]
public class EventDiceWindowDto
{
    [JsonProperty("is_active")] public bool  IsActive;
    [JsonProperty("start_ms")]  public long? StartMs;
    [JsonProperty("end_ms")]    public long? EndMs;
}

[Serializable]
public class EventDiceBoardEntryDto
{
    [JsonProperty("track_index")] public int             TrackIndex;
    [JsonProperty("item")]        public EventDiceItemDto Item;
}

[Serializable]
public class EventDiceMilestoneEntryDto
{
    [JsonProperty("milestone")]       public int             Milestone;
    [JsonProperty("points_required")] public int             PointsRequired;
    [JsonProperty("item")]            public EventDiceItemDto Item;
    [JsonProperty("claimed")]         public bool            Claimed;
    [JsonProperty("is_eligible")]     public bool            IsEligible;
}

[Serializable]
public class EventDiceProgressDto
{
    [JsonProperty("current_index")]    public int                    CurrentIndex;
    [JsonProperty("current_progress")] public int                    CurrentProgress;
    [JsonProperty("pending_rewards")]  public List<EventDiceItemDto> PendingRewards;
    [JsonProperty("ticket_balance")]   public int                    TicketBalance;
    [JsonProperty("roll_cost")]        public int                    RollCost;
}

[Serializable]
public class EventDiceStateResponse
{
    [JsonProperty("event_id")]   public int                             EventId;
    [JsonProperty("window")]     public EventDiceWindowDto              Window;
    [JsonProperty("board")]      public List<EventDiceBoardEntryDto>    Board;
    [JsonProperty("milestones")] public List<EventDiceMilestoneEntryDto> Milestones;
    [JsonProperty("progress")]   public EventDiceProgressDto            Progress;
}

// ── eventdice/roll ───────────────────────────────────────────────────────────

/// <summary>Response từ eventdice/roll. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventDiceRollResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"EVENT_NOT_ACTIVE" | "BOARD_EMPTY" | "INSUFFICIENT_TICKET"</summary>
    [JsonProperty("error")]   public string Error;

    /// <summary>Mặt xúc xắc 1-6 — dùng để chạy animation flag di chuyển đúng số ô.</summary>
    [JsonProperty("result")]           public int  Result;
    [JsonProperty("current_index")]    public int  CurrentIndex;
    [JsonProperty("current_progress")] public int  CurrentProgress;
    [JsonProperty("completed_loops")]  public int  CompletedLoops;
    [JsonProperty("landed_rewards")]   public List<EventDiceItemDto> LandedRewards;
    [JsonProperty("pending_rewards")]  public List<EventDiceItemDto> PendingRewards;
    /// <summary>Số dư sau giao dịch (vé đã trừ) — áp qua CurrencyManager.Instance.ApplyServerBalances(Balances).</summary>
    [JsonProperty("balances")]         public List<RewardDto>        Balances;
}

// ── eventdice/claim_pending ──────────────────────────────────────────────────

/// <summary>Response từ eventdice/claim_pending. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventDiceClaimPendingResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"NOTHING_TO_CLAIM"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("rewards")]  public List<EventDiceItemDto> Rewards;
    [JsonProperty("balances")] public List<RewardDto>        Balances;
}

// ── eventdice/claim_milestone ────────────────────────────────────────────────

[Serializable]
public class EventDiceClaimMilestoneRequest
{
    [JsonProperty("milestone")] public int Milestone;
}

/// <summary>Response từ eventdice/claim_milestone. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventDiceClaimMilestoneResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"MILESTONE_NOT_FOUND" | "REWARD_NOT_CONFIGURED" | "ALREADY_CLAIMED" | "NOT_YET_ELIGIBLE"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("milestone")] public int             Milestone;
    [JsonProperty("item")]      public EventDiceItemDto Item;
    [JsonProperty("balances")]  public List<RewardDto>  Balances;
}
