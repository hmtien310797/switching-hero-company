using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class RechargeClaimRequest
{
    [JsonProperty("milestone_id")] public int MilestoneId;
}

/// <summary>Response từ recharge/claim. Rewards/Balances dùng chung RewardDto (currency_type/amount)
/// như mission/afk/idle — CurrencyManager.ApplyServerBalances(Balances) áp thẳng được, không cần
/// mapping riêng cho numeric item_id.</summary>
[Serializable]
public class RechargeClaimResponse
{
    [JsonProperty("milestone_id")] public int             MilestoneId;
    [JsonProperty("rewards")]      public List<RewardDto> Rewards;
    [JsonProperty("balances")]     public List<RewardDto> Balances;
    [JsonProperty("points")]       public int              Points;
}

// ── recharge/state ────────────────────────────────────────────────────────────
// Reward icons/quantities đã có sẵn local qua DatabaseManager.GetShopPacksGloryPass()
// (SO sinh từ game_recharge_milestone.csv) — RechargeMilestoneDto chỉ cần require_points +
// is_claimed để biết trạng thái nút, không cần lặp lại item_id/quantity từ server.

[Serializable]
public class RechargeMilestoneDto
{
    [JsonProperty("id")]             public int    Id;
    [JsonProperty("require_points")] public int    RequirePoints;
    [JsonProperty("is_claimed")]     public bool   IsClaimed;
}

/// <summary>Response từ recharge/state — nguồn sự thật cho số lượt tích nạp trong tháng hiện
/// tại và trạng thái đã nhận của từng mốc GloryPass (xem ShopManager.SyncRechargeStateAsync).</summary>
[Serializable]
public class RechargeStateResponse
{
    [JsonProperty("points")]     public int                       Points;
    [JsonProperty("period")]     public string                    Period;
    [JsonProperty("milestones")] public List<RechargeMilestoneDto> Milestones;
}
