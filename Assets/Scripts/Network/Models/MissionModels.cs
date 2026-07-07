using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Network DTOs for mission/* RPCs.
/// State field uses JToken so callers can ToObject&lt;MissionSystemData&gt;()
/// without coupling NakamaClient to the mission namespace.
/// </summary>

public class MissionStateResponse
{
    [JsonProperty("success")]           public bool   Success;
    [JsonProperty("has_state")]         public bool   HasState;
    [JsonProperty("state")]             public JToken State;
    [JsonProperty("last_daily_reset")]  public string LastDailyReset;
    [JsonProperty("last_weekly_reset")] public string LastWeeklyReset;
}

public class MissionClaimRequest
{
    [JsonProperty("mission_id")]   public string               MissionId;
    [JsonProperty("mission_type")] public string               MissionType;
    [JsonProperty("points")]       public int                  Points;
    [JsonProperty("rewards")]      public List<MissionRewardDto> Rewards;
}

public class MissionClaimGroupRequest
{
    [JsonProperty("scope")]            public string               Scope;
    [JsonProperty("point_threshold")]  public int                  PointThreshold;
    [JsonProperty("rewards")]          public List<MissionRewardDto> Rewards;
    [JsonProperty("is_ads_x2")]        public bool                 IsAdsX2;
}

public class MissionRewardDto
{
    [JsonProperty("item_key")]  public string ItemKey;
    [JsonProperty("quantity")]  public long   Quantity;
}

public class MissionClaimResponse
{
    [JsonProperty("success")]  public bool            Success;
    [JsonProperty("error")]    public string          Error;
    [JsonProperty("rewards")]  public List<RewardDto> Rewards;
    [JsonProperty("balances")] public List<RewardDto> Balances;
}
