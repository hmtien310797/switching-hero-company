using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── Shared ────────────────────────────────────────────────────────────────────

[Serializable]
public class RewardDto
{
    [JsonProperty("currency_type")] public string CurrencyType;
    [JsonProperty("amount")]        public string Amount;
}

// ── afk/checkpoint ────────────────────────────────────────────────────────────

[Serializable]
public class AfkCheckpointRequest
{
    [JsonProperty("afk_stage")] public int AfkStage;
}

[Serializable]
public class AfkCheckpointResponse
{
    [JsonProperty("success")]         public bool Success;
    [JsonProperty("checkpoint_unix")] public long CheckpointUnix;
}

// ── afk/claim ─────────────────────────────────────────────────────────────────

[Serializable]
public class AfkClaimResponse
{
    [JsonProperty("success")]             public bool            Success;
    [JsonProperty("has_reward")]          public bool            HasReward;
    [JsonProperty("afk_stage")]           public int             AfkStage;
    [JsonProperty("elapsed_seconds")]     public int             ElapsedSeconds;
    [JsonProperty("max_offline_seconds")] public int             MaxOfflineSeconds;
    [JsonProperty("monsters_defeated")]   public int             MonstersDefeated;
    [JsonProperty("defeats_per_minute")]  public int             DefeatsPerMinute;
    [JsonProperty("rewards")]             public List<RewardDto> Rewards;
    [JsonProperty("balances")]            public List<RewardDto> Balances;
}
