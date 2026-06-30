using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── weapon/list ───────────────────────────────────────────────────────────────
// Xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 3 — id-based, thay thế hoàn toàn shape
// owned[]/shards{}/equipped{} (uid-based) cũ vì thiếu limit_break_stage và chưa từng được client
// dùng. player/me.weapons cũng dùng chung shape WeaponListResponse này.

[Serializable]
public class WeaponStandardStateDto
{
    [JsonProperty("weapon_id")]         public int  WeaponId;
    [JsonProperty("is_unlocked")]       public bool IsUnlocked;
    [JsonProperty("level")]             public int  Level;
    [JsonProperty("limit_break_stage")] public int  LimitBreakStage;
    [JsonProperty("shard")]             public int  Shard;
}

[Serializable]
public class WeaponExclusiveStateDto
{
    [JsonProperty("exclusive_weapon_id")] public int  ExclusiveWeaponId;
    [JsonProperty("hero_id")]             public int  HeroId;
    [JsonProperty("is_unlocked")]         public bool IsUnlocked;
    [JsonProperty("level")]               public int  Level;
    [JsonProperty("limit_break_stage")]   public int  LimitBreakStage;
    [JsonProperty("shard")]               public int  Shard;
    [JsonProperty("star")]                public int  Star;
}

[Serializable]
public class WeaponHeroEquipDto
{
    [JsonProperty("hero_id")]                      public int  HeroId;
    [JsonProperty("equipped_standard_weapon_id")]  public int  EquippedStandardWeaponId;
    [JsonProperty("equipped_exclusive_weapon_id")] public int  EquippedExclusiveWeaponId;
    [JsonProperty("use_exclusive")]                public bool UseExclusive;
}

/// <summary>Response từ weapon/list (và field player/me.weapons) — toàn bộ state hiện tại của user.</summary>
[Serializable]
public class WeaponListResponse
{
    [JsonProperty("standard")]   public List<WeaponStandardStateDto>  Standard;
    [JsonProperty("exclusive")]  public List<WeaponExclusiveStateDto> Exclusive;
    [JsonProperty("hero_equip")] public List<WeaponHeroEquipDto>      HeroEquip;
}

// ── weapon/equip ──────────────────────────────────────────────────────────────
// Xem Docs/be-weapon-equip-upgrade-rpc-spec.md — id-based (weapon_id/hero_id), không dùng
// hero_uid/weapon_uid (contract nháp cũ, chưa từng được client gọi).

[Serializable]
public class WeaponEquipRequest
{
    [JsonProperty("hero_id")]   public int    HeroId;
    /// <summary>"standard" | "exclusive"</summary>
    [JsonProperty("category")] public string Category;
    /// <summary>weapon_id (master data). Bắt buộc khi category = "standard"; bỏ qua khi "exclusive".</summary>
    [JsonProperty("weapon_id")] public int    WeaponId;
}

/// <summary>Response từ weapon/equip. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class WeaponEquipResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_HERO_ID" | "INVALID_CATEGORY" | "INVALID_WEAPON_ID" | "WEAPON_CLASS_MISMATCH" | "WEAPON_NOT_OWNED" | "EXCLUSIVE_NOT_FOUND" | "EXCLUSIVE_NOT_OWNED"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("hero_id")] public int    HeroId;
    /// <summary>"standard" | "exclusive" | "none"</summary>
    [JsonProperty("active_source")] public string ActiveSource;
    [JsonProperty("equipped_standard_weapon_id")]  public int  EquippedStandardWeaponId;
    [JsonProperty("equipped_exclusive_weapon_id")] public int  EquippedExclusiveWeaponId;
    [JsonProperty("use_exclusive")] public bool UseExclusive;
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
    /// <summary>Shard của weapon_id mới sau khi cộng dồn phần dư từ weapon_id cũ (carry-forward) —
    /// dùng số này thay vì giả định 0, để Fuse All có thể chạy liên tiếp nhiều bậc nếu đã tích đủ shard.</summary>
    [JsonProperty("new_weapon_shard_balance")] public int NewWeaponShardBalance;
    /// <summary>true nếu node mới (sau khi fuse) không thể fuse tiếp.</summary>
    [JsonProperty("is_max_node")]   public bool   IsMaxNode;
}

// ── weapon/upgrade (level up) ─────────────────────────────────────────────────
// Xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 4.

[Serializable]
public class WeaponUpgradeRequest
{
    /// <summary>"standard" | "exclusive"</summary>
    [JsonProperty("category")]  public string Category;
    /// <summary>weapon_id (master data). Bắt buộc khi category = "standard".</summary>
    [JsonProperty("weapon_id")] public int    WeaponId;
    /// <summary>hero_id. Bắt buộc khi category = "exclusive".</summary>
    [JsonProperty("hero_id")]   public int    HeroId;
}

/// <summary>Response từ weapon/upgrade. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class WeaponUpgradeResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_CATEGORY" | "INVALID_WEAPON_ID" | "INVALID_HERO_ID" | "WEAPON_NOT_OWNED" | "EXCLUSIVE_NOT_OWNED" | "CONFIG_NOT_FOUND" | "MAX_LEVEL_REACHED"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("category")]  public string Category;
    [JsonProperty("weapon_id")] public int    WeaponId;
    [JsonProperty("hero_id")]   public int    HeroId;

    [JsonProperty("old_level")] public int OldLevel;
    [JsonProperty("new_level")] public int NewLevel;
    [JsonProperty("current_limit_break_stage")] public int CurrentLimitBreakStage;
    [JsonProperty("current_max_level")]         public int CurrentMaxLevel;

    /// <summary>Số weapon_ore đã trừ — server giờ validate/trừ thật qua bag.items, không còn local-only.</summary>
    [JsonProperty("stone_cost")]    public int  StoneCost;
    /// <summary>Số dư weapon_ore còn lại sau khi trừ — dùng để Set() tuyệt đối thay vì cộng dồn local.</summary>
    [JsonProperty("stone_balance")] public int  StoneBalance;
    [JsonProperty("is_max_level")]  public bool IsMaxLevel;
}

// ── weapon/limitbreak ─────────────────────────────────────────────────────────
// Xem Docs/be-weapon-equip-upgrade-rpc-spec.md mục 5.

[Serializable]
public class WeaponLimitBreakRequest
{
    /// <summary>"standard" | "exclusive"</summary>
    [JsonProperty("category")]  public string Category;
    /// <summary>weapon_id (master data). Bắt buộc khi category = "standard".</summary>
    [JsonProperty("weapon_id")] public int    WeaponId;
    /// <summary>hero_id. Bắt buộc khi category = "exclusive".</summary>
    [JsonProperty("hero_id")]   public int    HeroId;
}

/// <summary>Response từ weapon/limitbreak. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class WeaponLimitBreakResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_CATEGORY" | "INVALID_WEAPON_ID" | "INVALID_HERO_ID" | "WEAPON_NOT_OWNED" | "EXCLUSIVE_NOT_OWNED" | "CONFIG_NOT_FOUND" | "MAXED" | "REQUIRED_LEVEL_NOT_REACHED"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("category")]  public string Category;
    [JsonProperty("weapon_id")] public int    WeaponId;
    [JsonProperty("hero_id")]   public int    HeroId;

    /// <summary>"success" | "failed" — kết quả roll theo success_rate (server RNG).</summary>
    [JsonProperty("result")]        public string Result;
    [JsonProperty("old_stage")]     public int    OldStage;
    /// <summary>Giữ nguyên old_stage nếu result = "failed".</summary>
    [JsonProperty("new_stage")]     public int    NewStage;
    [JsonProperty("new_max_level")] public int    NewMaxLevel;

    /// <summary>Số WeaponBreakThroughStone đáng lẽ phải trừ — tốn cả khi fail. Server hiện KHÔNG validate/trừ thật, xem mục 7 doc.</summary>
    [JsonProperty("stone_cost")] public int  StoneCost;
    [JsonProperty("is_maxed")]   public bool IsMaxed;
}
