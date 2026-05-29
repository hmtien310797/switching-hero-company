using System;
using Newtonsoft.Json;

[Serializable]
public class HeroInstance
{
    [JsonProperty("uid")]       public string Uid;
    [JsonProperty("hero_id")]   public string HeroId;
    [JsonProperty("hero_code")] public string HeroCode;
    [JsonProperty("hero_name")] public string HeroName;
    [JsonProperty("class")]     public string Class;
    [JsonProperty("element")]   public string Element;
    [JsonProperty("rarity")]    public string Rarity;
    [JsonProperty("level")]     public int    Level;
    [JsonProperty("exp")]       public int    Exp;
    [JsonProperty("star")]      public int    Star;
}

[Serializable]
public class HeroListResponse
{
    [JsonProperty("owned")]  public HeroInstance[] Owned;
    /// <summary>Always length 2. Null element = empty slot.</summary>
    [JsonProperty("lineup")] public string[]       Lineup;
}

[Serializable]
public class SetLineupRequest
{
    [JsonProperty("lineup")] public string[] Lineup; // length == 2
}
