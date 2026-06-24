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

// ── weapon/fuse ───────────────────────────────────────────────────────────────

[Serializable]
public class WeaponFuseRequest
{
    /// <summary>weapon_id (master data) hiện tại — không phải weapon_uid.</summary>
    [JsonProperty("weapon_id")] public int WeaponId;
}

/// <summary>Response từ weapon/fuse. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class WeaponFuseResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_WEAPON_ID" | "WEAPON_NOT_OWNED" | "MAX_NODE_REACHED" | "CONFIG_NOT_FOUND" | "TARGET_ALREADY_OWNED" | "NOT_ENOUGH_SHARD"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("old_weapon_id")] public int    OldWeaponId;
    [JsonProperty("old_name")]      public string OldName;
    [JsonProperty("old_grade")]     public string OldGrade;
    [JsonProperty("old_star")]      public int    OldStar;

    [JsonProperty("new_weapon_id")] public int    NewWeaponId;
    [JsonProperty("new_name")]      public string NewName;
    [JsonProperty("new_grade")]     public string NewGrade;
    [JsonProperty("new_star")]      public int    NewStar;

    [JsonProperty("shard_spent")]   public int    ShardSpent;
    /// <summary>Shard còn lại của weapon_id cũ — luôn là 0 sau khi fuse thành công.</summary>
    [JsonProperty("shard_balance")] public int    ShardBalance;
    /// <summary>true nếu node mới (sau khi fuse) không thể fuse tiếp.</summary>
    [JsonProperty("is_max_node")]   public bool   IsMaxNode;
}
