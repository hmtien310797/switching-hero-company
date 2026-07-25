using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── shared ────────────────────────────────────────────────────────────────────
// Reuses EventWheelItemDto (item_id/item_key/item_name/icon_key/amount) — identical shape,
// see EventWheelModels.cs. Mission/milestone rewards here are always plain items, never heroes.

// ── eventlogin/state ─────────────────────────────────────────────────────────

[Serializable]
public class EventLoginProgressDto
{
    [JsonProperty("current_day")] public int CurrentDay;
    [JsonProperty("total_day")]   public int TotalDay;
    [JsonProperty("points")]      public int Points;
}

[Serializable]
public class EventLoginMissionDto
{
    [JsonProperty("mission_id")] public string           MissionId;
    [JsonProperty("day")]        public int              Day;
    [JsonProperty("slot")]       public int              Slot;
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
public class EventLoginMilestoneDto
{
    [JsonProperty("milestone")]       public int              Milestone;
    [JsonProperty("points_required")] public int              PointsRequired;
    [JsonProperty("reward")]          public EventWheelItemDto Reward;
    [JsonProperty("is_claimed")]      public bool             IsClaimed;
}

[Serializable]
public class EventLoginStateRequest
{
    [JsonProperty("event_id")] public int EventId;
}

[Serializable]
public class EventLoginStateResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_EVENT" | "EVENT_NOT_ACTIVE"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("event_id")]   public int                          EventId;
    [JsonProperty("progress")]   public EventLoginProgressDto        Progress;
    [JsonProperty("missions")]   public List<EventLoginMissionDto>   Missions;
    [JsonProperty("milestones")] public List<EventLoginMilestoneDto> Milestones;
}

// ── eventlogin/mission_progress ─────────────────────────────────────────────
// Applies to every newbie event (7-day + 30-day) the account currently qualifies for — no
// event_id in the payload, see handler/event_login.js.

[Serializable]
public class EventLoginMissionProgressRequest
{
    [JsonProperty("trigger")] public string Trigger;
    [JsonProperty("value")]   public int    Value;
}

[Serializable]
public class EventLoginMissionProgressResponse
{
    [JsonProperty("success")] public bool Success;
}

// ── eventlogin/claim_mission ────────────────────────────────────────────────

[Serializable]
public class EventLoginClaimMissionRequest
{
    [JsonProperty("event_id")]   public int    EventId;
    [JsonProperty("mission_id")] public string MissionId;
}

[Serializable]
public class EventLoginClaimMissionResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_EVENT" | "EVENT_NOT_ACTIVE" | "CONFIG_NOT_FOUND" | "NOT_YET_ELIGIBLE" | "ALREADY_CLAIMED" | "NOT_ENOUGH_PROGRESS"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("event_id")]   public int              EventId;
    [JsonProperty("mission_id")] public string           MissionId;
    [JsonProperty("points")]     public int              Points;
    [JsonProperty("reward")]     public EventWheelItemDto Reward;
    [JsonProperty("balances")]   public List<RewardDto>  Balances;
}

// ── eventlogin/claim_all_missions ───────────────────────────────────────────

[Serializable]
public class EventLoginClaimAllMissionsRequest
{
    [JsonProperty("event_id")] public int EventId;
    [JsonProperty("day")]      public int Day;
}

[Serializable]
public class EventLoginClaimAllMissionsResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_EVENT" | "EVENT_NOT_ACTIVE" | "NOT_YET_ELIGIBLE"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("event_id")] public int                      EventId;
    [JsonProperty("day")]      public int                      Day;
    [JsonProperty("points")]   public int                      Points;
    [JsonProperty("rewards")]  public List<EventWheelItemDto>  Rewards;
    [JsonProperty("balances")] public List<RewardDto>          Balances;
}

// ── eventlogin/claim_milestone, eventlogin/claim_all_milestones ────────────

[Serializable]
public class EventLoginMilestoneRequest
{
    [JsonProperty("event_id")]  public int EventId;
    [JsonProperty("milestone")] public int Milestone;
}

[Serializable]
public class EventLoginMilestoneResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_EVENT" | "EVENT_NOT_ACTIVE" | "CONFIG_NOT_FOUND" | "ALREADY_CLAIMED" | "NOT_ENOUGH_POINTS"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("event_id")]  public int              EventId;
    [JsonProperty("milestone")] public int              Milestone;
    [JsonProperty("reward")]    public EventWheelItemDto Reward;
    [JsonProperty("balances")]  public List<RewardDto>  Balances;
}

[Serializable]
public class EventLoginClaimAllMilestonesRequest
{
    [JsonProperty("event_id")] public int EventId;
}

[Serializable]
public class EventLoginClaimAllMilestonesResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_EVENT" | "EVENT_NOT_ACTIVE"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("event_id")] public int                     EventId;
    [JsonProperty("rewards")]  public List<EventWheelItemDto> Rewards;
    [JsonProperty("balances")] public List<RewardDto>         Balances;
}
