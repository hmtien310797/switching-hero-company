using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── monthlypass/state ────────────────────────────────────────────────────────
// Reward icon/quantity mỗi ngày đã có sẵn local qua DatabaseManager.GetPackMonthly (sinh từ
// pack_monthly.csv) — MonthlyPassDayDto chỉ cần is_claimed để biết trạng thái nút, không lặp
// lại item_id/quantity từ server (giống RechargeMilestoneDto).

// is_claimed nay có nghĩa "đã gửi thưởng vào mailbox chưa" — thưởng ngày không còn claim thủ
// công (xem handler/monthly_pass.js sendMonthlyPassDailyRewardsIfNeeded), field giữ nguyên tên
// vì vẫn đúng bản chất (ngày đó đã được server phát thưởng hay chưa).
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

// Thưởng ngày không còn RPC/claim riêng — server gửi thẳng qua mailbox (mail/list, mail/claim)
// mỗi lần login, xem nakama/src/handler/monthly_pass.js sendMonthlyPassDailyRewardsIfNeeded.
