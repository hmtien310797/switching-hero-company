using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── monthlypass/state ────────────────────────────────────────────────────────
// Reward icon/quantity mỗi ngày đã có sẵn local qua DatabaseManager.GetPackMonthly (sinh từ
// pack_monthly.csv) — MonthlyPassDayDto chỉ cần is_claimed để biết trạng thái nút, không lặp
// lại item_id/quantity từ server (giống RechargeMilestoneDto).

[Serializable]
public class MonthlyPassDayDto
{
    [JsonProperty("day")]        public int  Day;
    [JsonProperty("is_claimed")] public bool IsClaimed;
}

[Serializable]
public class MonthlyPassDto
{
    [JsonProperty("id")]           public int  Id;
    [JsonProperty("drip_days")]    public int  DripDays;
    [JsonProperty("is_purchased")] public bool IsPurchased;
    [JsonProperty("is_active")]    public bool IsActive;
    [JsonProperty("current_day")]  public int  CurrentDay;
    [JsonProperty("expires_at")]   public long ExpiresAt;
    [JsonProperty("days")]         public List<MonthlyPassDayDto> Days;
}

/// <summary>Response từ monthlypass/state — nguồn sự thật cho trạng thái mua/nhận thưởng Monthly
/// Pass (xem ShopManager.SyncMonthlyPassStateAsync), thay cho ShopStorage local trước đây.</summary>
[Serializable]
public class MonthlyPassStateResponse
{
    [JsonProperty("passes")] public List<MonthlyPassDto> Passes;
}

// ── monthlypass/claim ────────────────────────────────────────────────────────

[Serializable]
public class MonthlyPassClaimRequest
{
    [JsonProperty("pack_id")] public int PackId;
}

/// <summary>Response từ monthlypass/claim. Day là ngày server vừa cộng thưởng (tự tính từ
/// purchased_at, không nhận day từ client) — dùng để tra reward hiển thị qua
/// DatabaseManager.GetPackMonthly(packId, Day).</summary>
[Serializable]
public class MonthlyPassClaimResponse
{
    [JsonProperty("pack_id")]  public int             PackId;
    [JsonProperty("day")]      public int             Day;
    [JsonProperty("rewards")]  public List<RewardDto> Rewards;
    [JsonProperty("balances")] public List<RewardDto> Balances;
}
