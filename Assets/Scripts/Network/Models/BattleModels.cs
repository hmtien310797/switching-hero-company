using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── battle/progression ───────────────────────────────────────────────────────

/// <summary>Progression hiện tại của user. Server là nguồn sự thật — dùng cho cả battle/progression và updated_progression của battle/end.</summary>
[Serializable]
public class BattleProgression
{
    [JsonProperty("current_stage")]         public int CurrentStage;
    [JsonProperty("current_chapter")]       public int CurrentChapter;
    [JsonProperty("highest_stage_cleared")] public int HighestStageCleared;
}

// ── battle/end ────────────────────────────────────────────────────────────────

[Serializable]
public class BattleEndRequest
{
    /// <summary>"Victory" | "Defeat" — case-sensitive.</summary>
    [JsonProperty("result")]           public string   Result;
    /// <summary>Global stage vừa đánh — phải khớp current_stage server đang lưu.</summary>
    [JsonProperty("stage")]            public int      Stage;
    [JsonProperty("creep_kills")]      public int      CreepKills;
    [JsonProperty("total_creeps")]     public int      TotalCreeps;
    [JsonProperty("hero_dead_count")]  public int      HeroDeadCount;
    [JsonProperty("duration_seconds")] public float    DurationSeconds;
    /// <summary>hero_id (master data) của lineup đã dùng trong trận, dạng string theo đúng contract RPC.</summary>
    [JsonProperty("hero_ids")]         public string[] HeroIds;
}

/// <summary>Response từ battle/end. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class BattleEndResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_STAGE" | "STAGE_MISMATCH"</summary>
    [JsonProperty("error")]   public string Error;

    /// <summary>"Victory" | "Defeat"</summary>
    [JsonProperty("result")]         public string Result;
    [JsonProperty("old_stage")]      public int    OldStage;
    [JsonProperty("new_stage")]      public int    NewStage;
    [JsonProperty("old_chapter")]    public int    OldChapter;
    [JsonProperty("new_chapter")]    public int    NewChapter;
    [JsonProperty("is_new_highest")] public bool   IsNewHighest;

    /// <summary>resource_type (CurrencyType name) → amount. Rỗng khi Defeat. Key không cố định — iterate, đừng hardcode.</summary>
    [JsonProperty("rewards")]             public Dictionary<string, double> Rewards;
    [JsonProperty("updated_progression")] public BattleProgression          UpdatedProgression;
    [JsonProperty("updated_resources")]   public BattleUpdatedResources     UpdatedResources;
}

[Serializable]
public class BattleUpdatedResources
{
    [JsonProperty("gold")]     public long Gold;
    [JsonProperty("diamonds")] public long Diamonds;
    [JsonProperty("energy")]   public int  Energy;
    /// <summary>profile.items đầy đủ sau khi cộng reward — key là resource_type (CurrencyType name) hoặc item_id, tuỳ loại.</summary>
    [JsonProperty("items")]   public Dictionary<string, double> Items;
}
