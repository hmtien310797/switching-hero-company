using System;
using Newtonsoft.Json;

// ── Requests ─────────────────────────────────────────────────────────────────

[Serializable]
public class SummonRequest
{
    /// <summary>"summon_30" (x10) | "summon_50" (x20)</summary>
    [JsonProperty("option_id")] public string OptionId;
}

[Serializable]
public class ClaimRewardRequest
{
    [JsonProperty("summon_level")] public int SummonLevel;
}

// ── Response: execute ────────────────────────────────────────────────────────

[Serializable]
public class SummonExecuteResponse
{
    [JsonProperty("success")]      public bool   Success;
    [JsonProperty("error")]        public string Error;

    /// <summary>"ticket" | "gem"</summary>
    [JsonProperty("payment_type")] public string PaymentType;
    [JsonProperty("paid_amount")]  public int    PaidAmount;

    [JsonProperty("old_total_roll")]   public int OldTotalRoll;
    [JsonProperty("new_total_roll")]   public int NewTotalRoll;
    [JsonProperty("old_summon_level")] public int OldSummonLevel;
    [JsonProperty("new_summon_level")] public int NewSummonLevel;

    /// <summary>Milestone level vừa mở khoá trong lần summon này (chưa claim).</summary>
    [JsonProperty("newly_unlocked_reward_levels")] public int[] NewlyUnlockedRewardLevels;

    [JsonProperty("entries")]           public SummonEntry[]    Entries;
    [JsonProperty("currency_balances")] public CurrencyBalances CurrencyBalances;
}

[Serializable]
public class SummonEntry
{
    [JsonProperty("roll_index")] public int RollIndex;

    // Hero summon
    [JsonProperty("hero_id")]     public int    HeroId;
    [JsonProperty("hero_name")]   public string HeroName;
    /// <summary>"Common"|"UnCommon"|"Rare"|"Epic"|"Legendary"|"Mythic"</summary>
    [JsonProperty("rarity")]      public string Rarity;
    [JsonProperty("is_pity_hit")] public bool   IsPityHit;

    // Skill summon
    [JsonProperty("skill_id")]   public int    SkillId;
    [JsonProperty("skill_name")] public string SkillName;
    [JsonProperty("skill_uid")]  public string SkillUid;

    // Weapon summon
    [JsonProperty("weapon_id")]   public int    WeaponId;
    [JsonProperty("weapon_name")] public string WeaponName;
    [JsonProperty("star")]        public int    Star;

    // Skill & Weapon shared
    /// <summary>Skill grade: "B"|"A"|"S"|"SS" — Weapon grade: "D"|"C"|"B"|"A"|"S"|"SS"</summary>
    [JsonProperty("grade")] public string Grade;

    // Common
    [JsonProperty("is_new")]       public bool IsNew;
    [JsonProperty("shard_gained")] public int  ShardGained;
}

[Serializable]
public class CurrencyBalances
{
    [JsonProperty("hero_ticket")]   public int HeroTicket;
    [JsonProperty("skill_ticket")]  public int SkillTicket;
    [JsonProperty("weapon_ticket")] public int WeaponTicket;
    [JsonProperty("diamond")]       public int Diamond;
}

// ── Response: claim_reward ───────────────────────────────────────────────────

[Serializable]
public class ClaimRewardResponse
{
    [JsonProperty("success")]           public bool                    Success;
    [JsonProperty("error")]             public string                  Error;
    [JsonProperty("rewards")]           public NakamaSummonRewardItem[] Rewards;
    [JsonProperty("currency_balances")] public CurrencyBalances         CurrencyBalances;
}

[Serializable]
public class NakamaSummonRewardItem
{
    /// <summary>"Currency" | "RandomHero" | "RandomSkill" | "Item"</summary>
    [JsonProperty("reward_type")]   public string RewardType;

    // Currency
    [JsonProperty("currency_type")] public string CurrencyType;
    [JsonProperty("amount")]        public int    Amount;

    // RandomHero
    [JsonProperty("hero_id")]       public int    HeroId;
    [JsonProperty("hero_name")]     public string HeroName;
    [JsonProperty("rarity")]        public string Rarity;

    // RandomSkill / RandomWeapon
    [JsonProperty("skill_id")]      public int    SkillId;
    [JsonProperty("skill_name")]    public string SkillName;
    [JsonProperty("grade")]         public string Grade;
    [JsonProperty("skill_uid")]     public string SkillUid;
    [JsonProperty("is_new")]        public bool   IsNew;
    [JsonProperty("shard_gained")]  public int    ShardGained;

    /// <summary>Item — flat bonus item from that SummonLevel's *_Levels row (game_hero_levels.js
    /// etc. ItemId/ItemQuantity columns), layered on top of the milestone reward above. Uses
    /// "amount" (shared with Currency) for the quantity.</summary>
    [JsonProperty("item_id")]       public int    ItemId;

    /// <summary>Milestone level this reward entry came from. A single claim_reward call can
    /// now return rewards for several skipped milestones at once — use this to mark each one
    /// claimed locally instead of assuming it's always the level that was requested.</summary>
    [JsonProperty("summon_level")]  public int    SummonLevel;
}

// ── Request/Response: summon/peek ───────────────────────────────────────────────
// Random preview roll using the player's current rates (incl. hero pity) — costs nothing,
// writes no state/inventory. See nakama/src/handler/summon.js doPeekRandom.

[Serializable]
public class SummonPeekRequest
{
    /// <summary>"hero" | "skill" | "weapon"</summary>
    [JsonProperty("type")] public string Type;
}

[Serializable]
public class SummonPeekResponse
{
    [JsonProperty("success")] public bool   Success;
    [JsonProperty("error")]   public string Error;

    /// <summary>"hero" | "skill" | "weapon"</summary>
    [JsonProperty("type")] public string Type;

    // Hero peek
    [JsonProperty("hero_id")]   public int    HeroId;
    [JsonProperty("hero_name")] public string HeroName;
    /// <summary>Hero: "Common"|"UnCommon"|"Rare"|"Epic"|"Legendary"|"Mythic" — Weapon: "D"|"C"|"B"|"A"|"S"|"SS"</summary>
    [JsonProperty("rarity")] public string Rarity;

    // Skill peek
    [JsonProperty("skill_id")]   public int    SkillId;
    [JsonProperty("skill_name")] public string SkillName;

    // Weapon peek
    [JsonProperty("weapon_id")]   public int    WeaponId;
    [JsonProperty("weapon_name")] public string WeaponName;
    [JsonProperty("star")]        public int    Star;

    /// <summary>Skill grade: "B"|"A"|"S"|"SS" — Weapon grade: "D"|"C"|"B"|"A"|"S"|"SS"</summary>
    [JsonProperty("grade")] public string Grade;
}

// ── Response: summon/state ────────────────────────────────────────────────────

[Serializable]
public class SummonStateResponse
{
    [JsonProperty("hero")]   public HeroSummonState  Hero;
    [JsonProperty("skill")]  public BasicSummonState Skill;
    [JsonProperty("weapon")] public BasicSummonState Weapon;
}

[Serializable]
public class HeroSummonState
{
    [JsonProperty("total_roll")]            public int   TotalRoll;
    [JsonProperty("pity_miss_counter")]     public int   PityMissCounter;
    [JsonProperty("summon_level")]          public int   SummonLevel;
    [JsonProperty("claimed_reward_levels")] public int[] ClaimedRewardLevels;
}

/// <summary>Dùng cho Skill và Weapon (không có pity).</summary>
[Serializable]
public class BasicSummonState
{
    [JsonProperty("total_roll")]            public int   TotalRoll;
    [JsonProperty("summon_level")]          public int   SummonLevel;
    [JsonProperty("claimed_reward_levels")] public int[] ClaimedRewardLevels;
}
