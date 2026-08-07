using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── shared ────────────────────────────────────────────────────────────────────
// Xem handler/mail.js. Server là nguồn sự thật duy nhất cho thư và phần thưởng.

[Serializable]
public class MailItemDto
{
    [JsonProperty("item_id")]   public int    ItemId;
    [JsonProperty("item_key")]  public string ItemKey;
    [JsonProperty("item_name")] public string ItemName;
    [JsonProperty("icon_key")]  public string IconKey;
    [JsonProperty("amount")]    public int    Amount;
}

[Serializable]
public class MailDto
{
    [JsonProperty("mail_id")]    public string           MailId;
    /// <summary>"personal" | "global"</summary>
    [JsonProperty("source")]     public string           Source;
    [JsonProperty("title")]      public string           Title;
    [JsonProperty("content")]    public string           Content;
    [JsonProperty("rewards")]    public List<MailItemDto> Rewards;
    [JsonProperty("created_at")] public long              CreatedAt;
    [JsonProperty("expire_at")]  public long              ExpireAt;
    [JsonProperty("claimed")]    public bool              Claimed;
}

// ── mail/list ────────────────────────────────────────────────────────────────

[Serializable]
public class MailListResponse
{
    [JsonProperty("mails")] public List<MailDto> Mails;
}

// ── mail/claim ───────────────────────────────────────────────────────────────

[Serializable]
public class MailClaimRequest
{
    [JsonProperty("mail_id")] public string MailId;
}

[Serializable]
public class MailClaimResponse
{
    [JsonProperty("success")]         public bool               Success;
    [JsonProperty("already_claimed")] public bool               AlreadyClaimed;
    [JsonProperty("rewards")]         public List<MailItemDto>  Rewards;
    /// <summary>Số dư sau giao dịch — áp qua CurrencyManager.Instance.ApplyServerBalances.</summary>
    [JsonProperty("balances")]        public List<RewardDto>    Balances;
}

// ── mail/claim_all ───────────────────────────────────────────────────────────

[Serializable]
public class MailClaimAllResponse
{
    [JsonProperty("success")]       public bool              Success;
    [JsonProperty("claimed_count")] public int                ClaimedCount;
    [JsonProperty("rewards")]       public List<MailItemDto> Rewards;
    [JsonProperty("balances")]      public List<RewardDto>   Balances;
}

