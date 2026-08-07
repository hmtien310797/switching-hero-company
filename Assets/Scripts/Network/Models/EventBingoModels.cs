using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── shared ────────────────────────────────────────────────────────────────────

[Serializable]
public class EventBingoItemDto
{
    [JsonProperty("item_id")]   public int    ItemId;
    [JsonProperty("item_key")]  public string ItemKey;
    [JsonProperty("item_name")] public string ItemName;
    [JsonProperty("icon_key")]  public string IconKey;
    [JsonProperty("amount")]    public int    Amount;
}

// ── eventbingo/state ─────────────────────────────────────────────────────────

/// <summary>Cửa sổ thời gian sự kiện (game_config_event.js) — *_ms là Unix epoch ms (UTC thật,
/// đã quy đổi từ giờ tường thuật UTC+7 phía server). Null nếu config thiếu/không Active.</summary>
[Serializable]
public class EventBingoWindowDto
{
    [JsonProperty("is_active")] public bool  IsActive;
    [JsonProperty("start_ms")]  public long? StartMs;
    [JsonProperty("end_ms")]    public long? EndMs;
}

[Serializable]
public class EventBingoBoardEntryDto
{
    [JsonProperty("track_index")] public int              TrackIndex;
    [JsonProperty("unlocked")]    public bool              Unlocked;
    [JsonProperty("item")]        public EventBingoItemDto Item;
}

[Serializable]
public class EventBingoLineEntryDto
{
    [JsonProperty("line_id")]  public int                     LineId;
    [JsonProperty("unlocked")] public bool                    Unlocked;
    [JsonProperty("claimed")]  public bool                    Claimed;
    [JsonProperty("rewards")]  public List<EventBingoItemDto> Rewards;
}

[Serializable]
public class EventBingoMilestoneEntryDto
{
    [JsonProperty("milestone")]       public int              Milestone;
    [JsonProperty("points_required")] public int              PointsRequired;
    [JsonProperty("item")]            public EventBingoItemDto Item;
    [JsonProperty("claimed")]         public bool             Claimed;
    [JsonProperty("is_eligible")]     public bool             IsEligible;
}

[Serializable]
public class EventBingoProgressDto
{
    [JsonProperty("current_round")]    public int CurrentRound;
    [JsonProperty("current_pool_id")]  public int CurrentPoolId;
    [JsonProperty("current_progress")] public int CurrentProgress;
}

[Serializable]
public class EventBingoStateResponse
{
    [JsonProperty("event_id")]   public int                              EventId;
    [JsonProperty("window")]     public EventBingoWindowDto              Window;
    [JsonProperty("board")]      public List<EventBingoBoardEntryDto>    Board;
    [JsonProperty("lines")]      public List<EventBingoLineEntryDto>     Lines;
    [JsonProperty("milestones")] public List<EventBingoMilestoneEntryDto> Milestones;
    [JsonProperty("progress")]   public EventBingoProgressDto            Progress;
}

// ── eventbingo/draw ──────────────────────────────────────────────────────────

/// <summary>Response từ eventbingo/draw. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventBingoDrawResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"EVENT_NOT_ACTIVE" | "BOARD_EMPTY" | "ALL_TILES_UNLOCKED"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("track_index")]          public int                TrackIndex;
    [JsonProperty("reward")]               public EventBingoItemDto  Reward;
    /// <summary>ID các line vừa hoàn thành nhờ tile này (rỗng nếu không có).</summary>
    [JsonProperty("newly_unlocked_lines")] public List<int>          NewlyUnlockedLines;
    /// <summary>Số dư sau giao dịch — áp qua CurrencyManager.Instance.ApplyServerBalances(Balances).</summary>
    [JsonProperty("balances")]             public List<RewardDto>   Balances;
}

// ── eventbingo/claim_line ────────────────────────────────────────────────────

[Serializable]
public class EventBingoClaimLineRequest
{
    [JsonProperty("line_id")] public int LineId;
}

/// <summary>Response từ eventbingo/claim_line. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventBingoClaimLineResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"LINE_NOT_UNLOCKED" | "LINE_CHEST_ALREADY_CLAIMED" | "LINE_REWARD_NOT_FOUND"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("line_id")]         public int                     LineId;
    [JsonProperty("rewards")]         public List<EventBingoItemDto> Rewards;
    /// <summary>True nếu toàn bộ 12 rương của round hiện tại đã được nhận — server đã tự sang
    /// round mới (progress/pool mới) trong cùng lượt gọi này.</summary>
    [JsonProperty("round_completed")] public bool                    RoundCompleted;
    [JsonProperty("progress")]        public EventBingoProgressDto   Progress;
    [JsonProperty("balances")]        public List<RewardDto>         Balances;
}

// ── eventbingo/claim_milestone ───────────────────────────────────────────────

[Serializable]
public class EventBingoClaimMilestoneRequest
{
    [JsonProperty("milestone")] public int Milestone;
}

/// <summary>Response từ eventbingo/claim_milestone. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class EventBingoClaimMilestoneResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"MILESTONE_NOT_FOUND" | "REWARD_NOT_CONFIGURED" | "ALREADY_CLAIMED" | "NOT_YET_ELIGIBLE"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("milestone")] public int              Milestone;
    [JsonProperty("item")]      public EventBingoItemDto Item;
    [JsonProperty("balances")]  public List<RewardDto>   Balances;
}
