using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── dungeon/state ─────────────────────────────────────────────────────────────

/// <summary>Per-dungeon info from dungeon/state. Stage model is "highest unlocked stage",
/// not lockstep like BattleProgression — any stage 1..HighestStageCleared can be replayed.</summary>
[Serializable]
public class DungeonInfo
{
    [JsonProperty("highest_stage_cleared")] public int    HighestStageCleared;
    [JsonProperty("next_stage")]            public int    NextStage;
    /// <summary>"CLEAR" | "DEFENSE" — matches DungeonModeType (KillAllEnemies/DefendObjective).</summary>
    [JsonProperty("mode")]                  public string Mode;
    [JsonProperty("stage_count")]           public int    StageCount;
    [JsonProperty("ticket_request")]        public int    TicketRequest;
}

[Serializable]
public class DungeonStateResponse
{
    /// <summary>Keyed by dungeon_key ("treasure"/"weapon"/"relic") — matches DungeonDefinitionData.DungeonKey.</summary>
    [JsonProperty("dungeons")]       public Dictionary<string, DungeonInfo> Dungeons;
    [JsonProperty("ticket_balance")] public double                          TicketBalance;
}

// ── dungeon/end ───────────────────────────────────────────────────────────────

[Serializable]
public class DungeonEndRequest
{
    [JsonProperty("dungeon_key")] public string DungeonKey;
    [JsonProperty("stage")]       public int    Stage;
    /// <summary>"Victory" | "Defeat" — case-sensitive, matches DungeonBattleResult.ToString().</summary>
    [JsonProperty("result")]     public string Result;
}

/// <summary>Response từ dungeon/end. Khi Success = false, chỉ Error có giá trị (và Balances rỗng).</summary>
[Serializable]
public class DungeonEndResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_DUNGEON" | "STAGE_LOCKED" | "INVALID_STAGE" | "INSUFFICIENT_TICKET"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("dungeon_key")] public string DungeonKey;
    [JsonProperty("mode")]        public string Mode;
    /// <summary>"Victory" | "Defeat"</summary>
    [JsonProperty("result")]     public string Result;
    [JsonProperty("stage")]       public int    Stage;

    [JsonProperty("is_new_highest")]        public bool IsNewHighest;
    [JsonProperty("highest_stage_cleared")] public int  HighestStageCleared;
    [JsonProperty("next_stage")]            public int  NextStage;

    /// <summary>item_key (CurrencyType name) → amount granted this call. Rỗng khi Defeat. Key không cố định — iterate, đừng hardcode.</summary>
    [JsonProperty("rewards")]      public Dictionary<string, double> Rewards;
    [JsonProperty("ticket_spent")] public int                        TicketSpent;

    /// <summary>Absolute post-transaction balances for the ticket item + every rewarded item_key —
    /// apply directly via CurrencyManager.Instance.ApplyServerBalances(Balances).</summary>
    [JsonProperty("balances")] public List<RewardDto> Balances;
}

// ── dungeon/stage_table ────────────────────────────────────────────────────────
// Static per-stage reward preview — same baked numbers dungeon/end grants, exposed for the
// stage browser (DungeonView.cs Next/Prev) so the client stops computing its own copy via
// DungeonRewardResolver/DungeonFormulaData (which could silently drift from the server).

[Serializable]
public class DungeonStageRewardEntry
{
    [JsonProperty("stage")]   public int                       Stage;
    /// <summary>item_key (CurrencyType name) → amount for this stage. Key không cố định — iterate.</summary>
    [JsonProperty("rewards")] public Dictionary<string, double> Rewards;
}

[Serializable]
public class DungeonStageTableResponse
{
    [JsonProperty("success")]     public bool                          Success;
    /// <summary>"INVALID_DUNGEON" khi Success = false.</summary>
    [JsonProperty("error")]       public string                        Error;
    [JsonProperty("dungeon_key")] public string                        DungeonKey;
    [JsonProperty("stage_count")] public int                            StageCount;
    [JsonProperty("stages")]      public List<DungeonStageRewardEntry> Stages;
}
