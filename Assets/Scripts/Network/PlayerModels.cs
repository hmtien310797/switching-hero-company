using System;
using Newtonsoft.Json;

[Serializable]
public class PlayerMeResponse
{
    [JsonProperty("user_id")]        public string            user_id;
    [JsonProperty("username")]       public string            username;
    [JsonProperty("display_name")]   public string            display_name;
    [JsonProperty("avatar_url")]     public string            avatar_url;
    // Chỉ true khi account đã link Google/Apple — account guest/BD (username+password) đều false.
    // Xem NakamaClient.LinkGoogleAsync/LinkAppleAsync + nakama/src/handler/account.js.
    [JsonProperty("google_linked")]  public bool              google_linked;
    [JsonProperty("apple_linked")]   public bool              apple_linked;
    [JsonProperty("level")]          public int               level;
    [JsonProperty("exp")]            public int               exp;
    [JsonProperty("gems")]   public int gems;
    [JsonProperty("coins")]  public int coins;
    [JsonProperty("items")]  public System.Collections.Generic.Dictionary<string, int> items;
    // item_id 8 = summon_ticket_hero, 9 = summon_ticket_weapon, 46 = summon_ticket_skill (server
    // game_item.js). Ids 1/2 are gold/crystal as of the server's item table restructure — do not
    // revert to those.
    public int hero_ticket   => items != null && items.TryGetValue("8", out var h) ? h : 0;
    public int weapon_ticket => items != null && items.TryGetValue("9", out var w) ? w : 0;
    public int skill_ticket  => items != null && items.TryGetValue("46", out var s) ? s : 0;
    // item_id 43 = weapon_ore — material thật cho weapon/upgrade (Level Up), xem WeaponManager.cs.
    public int weapon_ore    => items != null && items.TryGetValue("43", out var o) ? o : 0;
    [JsonProperty("energy")]         public int                energy;
    [JsonProperty("rating")]         public int                rating;
    [JsonProperty("total_summons")]  public int                total_summons;
    // Số lần đã đổi tên — dùng để tính giá lần đổi tiếp theo, xem RenameFeeConfig.GetFee.
    [JsonProperty("rename_count")]   public int                rename_count;
    // Đã nhận thưởng liên kết Google/Apple chưa — xem account/claim_link_reward.
    [JsonProperty("link_reward_claimed")] public bool          link_reward_claimed;
    [JsonProperty("heroes")]         public HeroInventory      heroes;
    [JsonProperty("skills")]         public SkillListResponse  skills;
    [JsonProperty("weapons")]        public WeaponListResponse weapons;
    /// <summary>current_unlocked_tier + stack từng stat của hệ thống Growth — xem Docs/be-growth-rpc-spec.md mục 8.</summary>
    [JsonProperty("growth")]         public GrowthStateResponse growth;
    /// <summary>current_stage/current_chapter/highest_stage_cleared — nguồn sự thật cho stage, dùng ngay sau login, không cần gọi battle/progression riêng.</summary>
    [JsonProperty("progression")]    public BattleProgression  progression;
}

[Serializable]
public class PlayerUpdateRequest
{
    public string display_name;
    public string avatar_url;
    public string lang_tag;
    public string location;
    public string timezone;
}

[Serializable]
public class PlayerUpdateResponse
{
    public bool updated;
}

[Serializable]
public class PlayerRenameRequest
{
    public string display_name;
}

[Serializable]
public class PlayerRenameResponse
{
    public string display_name;
    public int    rename_fee;
    public int    rename_count;
}

[Serializable]
public class AccountLinkRewardItem
{
    [JsonProperty("item_id")] public int ItemId;
    [JsonProperty("amount")]  public int Amount;
}

[Serializable]
public class AccountClaimLinkRewardResponse
{
    [JsonProperty("claimed")] public bool claimed;
    [JsonProperty("rewards")] public System.Collections.Generic.List<AccountLinkRewardItem> rewards;
    [JsonProperty("gems")]    public int gems;
}
