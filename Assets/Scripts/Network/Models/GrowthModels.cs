using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── growth/state ──────────────────────────────────────────────────────────────
// Xem Docs/be-growth-rpc-spec.md mục 3 — tier/stack đã unlock của player (không gắn theo hero/weapon).

[Serializable]
public class GrowthStatStackDto
{
    /// <summary>Tên StatType (Atk, MaxHp, Def, ...).</summary>
    [JsonProperty("stat")]          public string Stat;
    [JsonProperty("current_stack")] public int    CurrentStack;
}

/// <summary>Response từ growth/state (và field player/me.growth) — toàn bộ tiến trình hiện tại của user.</summary>
[Serializable]
public class GrowthStateResponse
{
    [JsonProperty("current_unlocked_tier")] public int                       CurrentUnlockedTier;
    /// <summary>Chỉ chứa stat đã mua >= 1 stack — stat khác tự default CurrentStack = 0.</summary>
    [JsonProperty("stats")]                 public List<GrowthStatStackDto> Stats;
}

// ── growth/upgrade ────────────────────────────────────────────────────────────

[Serializable]
public class GrowthUpgradeRequest
{
    [JsonProperty("stat")]   public string Stat;
    /// <summary>Số stack MONG MUỐN mua — server tự clamp theo trần stack còn lại, không lỗi nếu chỉ mua được ít hơn.</summary>
    [JsonProperty("amount")] public int    Amount;
}

/// <summary>Response từ growth/upgrade. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class GrowthUpgradeResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"INVALID_STAT" | "STAT_NOT_UNLOCKED" | "ALREADY_MAXED" | "AMOUNT_INVALID"</summary>
    [JsonProperty("error")]   public string Error;

    [JsonProperty("stat")]          public string Stat;
    /// <summary>Số stack thực mua được — có thể nhỏ hơn amount đã request nếu chạm trần trước.</summary>
    [JsonProperty("bought_amount")] public int    BoughtAmount;
    [JsonProperty("gold_spent")]    public int    GoldSpent;
    [JsonProperty("old_stack")]     public int    OldStack;
    [JsonProperty("new_stack")]     public int    NewStack;
    /// <summary>Chỉ để hiển thị UI — KHÔNG phải số dư Gold đã được server persist (xem Docs/be-growth-rpc-spec.md mục 7).</summary>
    [JsonProperty("gold_balance")]  public int    GoldBalance;
    /// <summary>true nếu new_stack đã chạm trần của tier đang unlock.</summary>
    [JsonProperty("is_maxed")]      public bool   IsMaxed;
}

// ── growth/unlocktier ─────────────────────────────────────────────────────────
// Không cần request riêng — server tự suy next_tier = current_unlocked_tier + 1.

/// <summary>Response từ growth/unlocktier. Khi Success = false, chỉ Error có giá trị.</summary>
[Serializable]
public class GrowthUnlockTierResponse
{
    [JsonProperty("success")]  public bool   Success;
    /// <summary>"NO_NEXT_TIER" | "TIER_NOT_FULLY_MAXED"</summary>
    [JsonProperty("error")]    public string Error;
    [JsonProperty("old_tier")] public int    OldTier;
    [JsonProperty("new_tier")] public int    NewTier;
}
