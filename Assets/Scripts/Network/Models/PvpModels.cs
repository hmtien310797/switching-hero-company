using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── pvp/state ────────────────────────────────────────────────────────────────
// Xem handler/pvp.js rpcPvpState. Server sở hữu rank/tier/vé/arena_token — client Phase-1
// local (PvpDefaults/PvpPlayerData) không còn là nguồn sự thật cho các field này.

[Serializable]
public class PvpStateResponse
{
    [JsonProperty("rank_points")]    public int    RankPoints;
    /// <summary>0 = chưa đạt tier nào.</summary>
    [JsonProperty("tier_id")]        public int    TierId;
    /// <summary>"bronze".."diamond" — null khi TierId == 0.</summary>
    [JsonProperty("tier_name")]      public string TierName;
    [JsonProperty("tier_level")]     public int    TierLevel;
    [JsonProperty("tickets")]        public int    Tickets;
    [JsonProperty("max_tickets")]    public int    MaxTickets;
    /// <summary>Unix giây lần hồi vé kế tiếp — null khi vé đã đầy.</summary>
    [JsonProperty("next_ticket_at")] public long?  NextTicketAt;
    [JsonProperty("arena_token")]    public long   ArenaToken;
    [JsonProperty("history")]        public List<PvpHistoryEntry> History = new();
}

[Serializable]
public class PvpHistoryEntry
{
    [JsonProperty("battle_id")]   public string BattleId;
    /// <summary>"Victory" | "Defeat" | "Draw"</summary>
    [JsonProperty("result")]      public string Result;
    [JsonProperty("rank_delta")]  public int    RankDelta;
    [JsonProperty("token_delta")] public long   TokenDelta;
    [JsonProperty("at")]          public long   At;
}

// ── pvp/matchmaking ─────────────────────────────────────────────────────────
// payload rỗng ("{}") — không có request DTO riêng.

[Serializable]
public class PvpMatchmakingResponse
{
    [JsonProperty("success")]     public bool   Success;
    /// <summary>Chỉ có giá trị khi Success == false — hiện chỉ có "NOT_ENOUGH_TICKETS".</summary>
    [JsonProperty("error")]       public string Error;
    [JsonProperty("battle_id")]   public string BattleId;
    [JsonProperty("random_seed")] public ulong  RandomSeed;
    [JsonProperty("opponent")]    public PvpOpponentSnapshot Opponent;
    [JsonProperty("tickets")]     public int    Tickets;
}

/// <summary>Sức mạnh đối thủ do server tính (Phase 1: mirror của chính đội hình người chơi, nhân hệ
/// số ngẫu nhiên — KHÔNG phải hero thật của người chơi khác). Chỉ dùng làm baseline cho check hợp lý
/// phía server ở pvp/battle/end — client vẫn tự sinh đối thủ hiển thị/chiến đấu qua
/// MockPvpOpponentGenerator khi IsBot == true. IsBot == false nghĩa là server đã ghép được 1
/// người chơi thật gần rank_points (qua leaderboard pvp_rank) — <see cref="Heroes"/> lúc đó có
/// hero_id/tier/star/equipment/skill thật của họ (front=index 0, back=index 1, có thể null nếu
/// slot trống), đủ để build snapshot chiến đấu thật qua DefenderTestStatsBuilder (xem
/// ServerPvPMatchmakingService). <see cref="Growth"/>/<see cref="TransmutationModifiers"/> là bonus
/// account-wide của họ (không theo từng hero) — áp lên StatModule của cả 2 hero defender ở
/// PvpRealBattleController.ApplyOpponentGrowthAndTransmutation.</summary>
[Serializable]
public class PvpOpponentSnapshot
{
    [JsonProperty("atk")]     public double Atk;
    [JsonProperty("hp")]      public double Hp;
    [JsonProperty("def")]     public double Def;
    [JsonProperty("atk_spd")] public double AtkSpd;
    [JsonProperty("is_bot")]  public bool   IsBot;
    /// <summary>Chỉ có giá trị khi IsBot == false.</summary>
    [JsonProperty("opponent_user_id")]      public string OpponentUserId;
    [JsonProperty("opponent_display_name")] public string OpponentDisplayName;
    [JsonProperty("opponent_rank_points")]  public int?   OpponentRankPoints;
    /// <summary>[front, back] — chỉ có khi IsBot == false. Từng phần tử có thể null (slot trống
    /// hoặc lookup lỗi phía server).</summary>
    [JsonProperty("heroes")] public List<PvpOpponentHeroLoadout> Heroes;
    /// <summary>Growth stack thô (current_unlocked_tier + stat_stack) — client tự resolve thành
    /// StatModifier bằng GrowthSystemService (cùng thuật toán/config với người chơi thật).</summary>
    [JsonProperty("growth")] public PvpOpponentGrowth Growth;
    /// <summary>Modifier Transmutation ĐÃ resolve sẵn từ server (stat_type/op/value bake lúc roll) —
    /// dùng thẳng TransmutationSystemHelper.ToModifiers, không cần tính lại.</summary>
    [JsonProperty("transmutation_modifiers")] public List<TransmutationModifierDto> TransmutationModifiers;
}

[Serializable]
public class PvpOpponentGrowth
{
    [JsonProperty("current_unlocked_tier")] public int CurrentUnlockedTier;
    [JsonProperty("stats")] public List<PvpOpponentGrowthStat> Stats = new();
}

[Serializable]
public class PvpOpponentGrowthStat
{
    /// <summary>Tên StatType (vd "Atk", "MaxHp") — parse bằng Enum.TryParse&lt;StatType&gt;.</summary>
    [JsonProperty("stat")] public string Stat;
    [JsonProperty("current_stack")] public int CurrentStack;
}

/// <summary>Đội hình thật 1 hero của đối thủ thật (server đọc từ hero/weapon/skill storage của họ —
/// xem buildOpponentHeroLoadout trong handler/pvp.js). Map trực tiếp sang
/// Pvp.DevTools.DefenderSlotConfig để tái dùng pipeline DefenderTestStatsBuilder có sẵn.</summary>
[Serializable]
public class PvpOpponentHeroLoadout
{
    [JsonProperty("hero_id")] public int HeroId;
    /// <summary>0=Common,1=UnCommon,2=Rare,3=Epic,4=Legendary,5=Mythic — khớp enum HeroProgressTier.</summary>
    [JsonProperty("tier")]    public int Tier;
    [JsonProperty("star")]    public int Star;
    [JsonProperty("level")]   public int Level;
    [JsonProperty("equipment")] public PvpOpponentEquipment Equipment;
    [JsonProperty("skills")]    public PvpOpponentSkills Skills;
}

[Serializable]
public class PvpOpponentEquipment
{
    /// <summary>-1 = không trang bị.</summary>
    [JsonProperty("standard_weapon_id")]         public int StandardWeaponId;
    [JsonProperty("standard_weapon_level")]      public int StandardWeaponLevel;
    [JsonProperty("standard_weapon_limitbreak")] public int StandardWeaponLimitbreak;
    /// <summary>-1 = không trang bị.</summary>
    [JsonProperty("exclusive_weapon_id")]    public int ExclusiveWeaponId;
    [JsonProperty("exclusive_weapon_level")] public int ExclusiveWeaponLevel;
    [JsonProperty("exclusive_weapon_star")]  public int ExclusiveWeaponStar;
    [JsonProperty("use_exclusive")]          public bool UseExclusive;
}

[Serializable]
public class PvpOpponentSkills
{
    [JsonProperty("equipped")] public List<PvpOpponentSkillSlot> Equipped = new();
}

[Serializable]
public class PvpOpponentSkillSlot
{
    [JsonProperty("skill_id")] public int SkillId;
    [JsonProperty("level")]    public int Level;
}

// ── pvp/battle/end ───────────────────────────────────────────────────────────

[Serializable]
public class PvpBattleEndRequest
{
    [JsonProperty("battle_id")] public string BattleId;
    /// <summary>"Victory" | "Defeat" | "Draw" — case-sensitive. Surrender client-side map thành "Defeat".</summary>
    [JsonProperty("result")]    public string Result;
}

/// <summary>Response từ pvp/battle/end. Khi Success == false, chỉ Error có giá trị.</summary>
[Serializable]
public class PvpBattleEndResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"BATTLE_NOT_FOUND" | "BATTLE_EXPIRED" | "CHEAT_DETECTED"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("result")]           public string Result;
    [JsonProperty("old_rank_points")]  public int    OldRankPoints;
    [JsonProperty("new_rank_points")]  public int    NewRankPoints;
    [JsonProperty("rank_delta")]       public int    RankDelta;
    [JsonProperty("token_delta")]      public long   TokenDelta;
    [JsonProperty("arena_token")]      public long   ArenaToken;
    [JsonProperty("tier_id")]          public int    TierId;
    [JsonProperty("tier_name")]        public string TierName;
    [JsonProperty("tier_level")]       public int    TierLevel;
    [JsonProperty("new_tier_rewards")] public List<PvpTierRewardGrant> NewTierRewards = new();
    [JsonProperty("tickets")]          public int    Tickets;
}

/// <summary>1 tier vừa vượt qua trong lần battle/end này (có thể nhiều tier cùng lúc nếu rank nhảy xa).</summary>
[Serializable]
public class PvpTierRewardGrant
{
    [JsonProperty("tier_id")]    public int    TierId;
    [JsonProperty("tier_name")]  public string TierName;
    [JsonProperty("tier_level")] public int    TierLevel;
    [JsonProperty("rewards")]    public List<PvpRewardItem> Rewards = new();
}

[Serializable]
public class PvpRewardItem
{
    [JsonProperty("item_id")] public int ItemId;
    [JsonProperty("amount")]  public int Amount;
}

// ── pvp/leaderboard/top, pvp/leaderboard/around_me ────────────────────────────
// FormationPower/RankTierId/RankTierLevel/FormationHeroes trong PvpLeaderboardEntryModel (client
// model cũ, dùng cho UI) KHÔNG có ở đây — server chưa lưu formation/hero data cho leaderboard, xem
// ServerPvPLeaderboardService cho cách map field còn thiếu.

[Serializable]
public class PvpLeaderboardTopResponse
{
    [JsonProperty("records")]     public List<PvpLeaderboardRecord> Records = new();
    [JsonProperty("next_cursor")] public string NextCursor;
    [JsonProperty("prev_cursor")] public string PrevCursor;
}

[Serializable]
public class PvpLeaderboardAroundMeResponse
{
    [JsonProperty("records")] public List<PvpLeaderboardRecord> Records = new();
}

[Serializable]
public class PvpLeaderboardRecord
{
    [JsonProperty("user_id")]      public string UserId;
    [JsonProperty("display_name")] public string DisplayName;
    [JsonProperty("rank_points")]  public int    RankPoints;
    [JsonProperty("rank")]         public int    Rank;
    [JsonProperty("update_time")]  public string UpdateTime;
}
