using System;
using Newtonsoft.Json;

[Serializable]
public class PlayerMeResponse
{
    [JsonProperty("user_id")]        public string            user_id;
    [JsonProperty("username")]       public string            username;
    [JsonProperty("display_name")]   public string            display_name;
    [JsonProperty("avatar_url")]     public string            avatar_url;
    [JsonProperty("level")]          public int               level;
    [JsonProperty("exp")]            public int               exp;
    [JsonProperty("gems")]   public int gems;
    [JsonProperty("coins")]  public int coins;
    [JsonProperty("items")]  public System.Collections.Generic.Dictionary<string, int> items;
    public int hero_ticket   => items != null && items.TryGetValue("1", out var h) ? h : 0;
    public int skill_ticket  => items != null && items.TryGetValue("2", out var s) ? s : 0;
    [JsonProperty("energy")]         public int                energy;
    [JsonProperty("rating")]         public int                rating;
    [JsonProperty("total_summons")]  public int                total_summons;
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
