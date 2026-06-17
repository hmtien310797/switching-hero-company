using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── Weapon Instance ───────────────────────────────────────────────────────────

[Serializable]
public class WeaponInstance
{
    /// <summary>Unique instance ID — dùng để equip/unequip.</summary>
    [JsonProperty("uid")]       public string Uid;
    [JsonProperty("weapon_id")] public int    WeaponId;
    [JsonProperty("name")]      public string Name;
    /// <summary>"D"|"C"|"B"|"A"|"S"|"SS"</summary>
    [JsonProperty("grade")]     public string Grade;
    /// <summary>1–5</summary>
    [JsonProperty("star")]      public int    Star;
    [JsonProperty("level")]     public int    Level;
    [JsonProperty("exp")]       public int    Exp;
}

// ── weapon/list ───────────────────────────────────────────────────────────────

[Serializable]
public class WeaponListResponse
{
    [JsonProperty("owned")]    public WeaponInstance[]           Owned;
    /// <summary>weapon_id (string) → số lượng shard. VD: { "15": 2 }</summary>
    [JsonProperty("shards")]   public Dictionary<string, int>    Shards;
    /// <summary>hero_uid → weapon_uid | null (1 slot / hero)</summary>
    [JsonProperty("equipped")] public Dictionary<string, string> Equipped;
}

// ── weapon/equip ──────────────────────────────────────────────────────────────

[Serializable]
public class WeaponEquipRequest
{
    [JsonProperty("hero_uid")]   public string HeroUid;
    /// <summary>uid của weapon. null hoặc "" để bỏ trang bị.</summary>
    [JsonProperty("weapon_uid")] public string WeaponUid;
}

[Serializable]
public class WeaponEquipResponse
{
    [JsonProperty("updated")]  public bool                        Updated;
    [JsonProperty("equipped")] public Dictionary<string, string>  Equipped;
}

// ── weapon/unequip ────────────────────────────────────────────────────────────

[Serializable]
public class WeaponUnequipRequest
{
    [JsonProperty("weapon_uid")] public string WeaponUid;
}

[Serializable]
public class WeaponUnequipResponse
{
    [JsonProperty("updated")]  public bool                        Updated;
    [JsonProperty("reason")]   public string                      Reason;
    [JsonProperty("equipped")] public Dictionary<string, string>  Equipped;
}
