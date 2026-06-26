using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── Skill Instance ────────────────────────────────────────────────────────────

[Serializable]
public class SkillInstance
{
    /// <summary>Unique instance ID — dùng để equip/unequip.</summary>
    [JsonProperty("uid")]      public string Uid;
    [JsonProperty("skill_id")] public int    SkillId;
    [JsonProperty("name")]     public string Name;
    /// <summary>"B"|"A"|"S"|"SS"</summary>
    [JsonProperty("grade")]    public string Grade;
    [JsonProperty("level")]    public int    Level;
    [JsonProperty("exp")]      public int    Exp;
}

// ── skill/list ────────────────────────────────────────────────────────────────

[Serializable]
public class SkillListResponse
{
    [JsonProperty("owned")]    public SkillInstance[]              Owned;
    /// <summary>skill_id (string) → số lượng shard. VD: { "1": 3 }</summary>
    [JsonProperty("shards")] 
    public Dictionary<string, int>      Shards;
    /// <summary>hero_uid → [skill_uid slot0, skill_uid slot1]. null = slot trống.</summary>
    [JsonProperty("equipped")] 
    public Dictionary<string, string[]> Equipped;
}

// ── skill/equip ───────────────────────────────────────────────────────────────

[Serializable]
public class SkillEquipRequest
{
    [JsonProperty("hero_uid")]   public string HeroUid;
    /// <summary>0 hoặc 1</summary>
    [JsonProperty("slot_index")] public int    SlotIndex;
    /// <summary>uid của skill cần trang bị. null hoặc "" để bỏ trang bị slot đó.</summary>
    [JsonProperty("skill_uid")]  public string SkillUid;
}

[Serializable]
public class SkillEquipResponse
{
    [JsonProperty("updated")]  public bool                          Updated;
    [JsonProperty("equipped")] public Dictionary<string, string[]> Equipped;
}

// ── skill/unequip ─────────────────────────────────────────────────────────────

[Serializable]
public class SkillUnequipRequest
{
    [JsonProperty("skill_uid")] public string SkillUid;
}

[Serializable]
public class SkillUnequipResponse
{
    [JsonProperty("updated")]  public bool                          Updated;
    [JsonProperty("reason")]   public string                        Reason;
    [JsonProperty("equipped")] public Dictionary<string, string[]> Equipped;
}

// ── skill/enhance_all ────────────────────────────────────────────────────────

[Serializable]
public class SkillEnhanceAllResponse
{
    [JsonProperty("success")]               public bool                Success;
    [JsonProperty("processed_skill_count")] public int                 ProcessedSkillCount;
    [JsonProperty("upgraded_skill_count")]  public int                 UpgradedSkillCount;
    [JsonProperty("total_level_gained")]    public int                 TotalLevelGained;
    [JsonProperty("total_shard_spent")]     public int                 TotalShardSpent;
    [JsonProperty("entries")]               public SkillEnhanceEntry[] Entries;
}

[Serializable]
public class SkillEnhanceEntry
{
    [JsonProperty("skill_id")]    public int SkillId;
    [JsonProperty("old_level")]   public int OldLevel;
    [JsonProperty("new_level")]   public int NewLevel;
    [JsonProperty("old_shard")]   public int OldShard;
    [JsonProperty("new_shard")]   public int NewShard;
    [JsonProperty("shard_spent")] public int ShardSpent;
}
