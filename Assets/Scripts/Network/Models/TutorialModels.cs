using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── tutorial/state ───────────────────────────────────────────────────────────

[Serializable]
public class TutorialStateResponse
{
    [JsonProperty("completed_step_ids")] public List<int> CompletedStepIds;
}

// ── tutorial/complete_step ───────────────────────────────────────────────────

[Serializable]
public class TutorialCompleteStepRequest
{
    [JsonProperty("step_id")] public int StepId;
}

[Serializable]
public class TutorialCompleteStepResponse
{
    [JsonProperty("success")]           public bool                     Success;
    [JsonProperty("error")]             public string                   Error;
    [JsonProperty("step_id")]           public int                      StepId;
    [JsonProperty("already_completed")] public bool                     AlreadyCompleted;
    [JsonProperty("rewards")]           public Dictionary<string, long> Rewards;
    // Cùng shape RewardDto (currency_type/amount) dùng chung với afk/idle — xem AfkRewardModels.cs.
    [JsonProperty("balances")]          public List<RewardDto>          Balances;
}
