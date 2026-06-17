using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class HeroInstance
{
    [JsonProperty("uid")]          public string Uid;
    [JsonProperty("hero_id")]      public int    HeroId;
    [JsonProperty("name")]         public string Name;
    [JsonProperty("class")]        public string Class;
    [JsonProperty("rarity")]       public string Rarity;
    [JsonProperty("element")]      public string Element;
    [JsonProperty("level")]        public int    Level;
    [JsonProperty("exp")]          public int    Exp;
    [JsonProperty("star")]         public int    Star;
    [JsonProperty("hp")]           public float  Hp;
    [JsonProperty("atk")]          public float  Atk;
    [JsonProperty("def")]          public float  Def;
    [JsonProperty("crit_chance")]  public float  CritChance;
    [JsonProperty("crit_damage")]  public float  CritDamage;
    [JsonProperty("atk_spd")]      public float  AtkSpd;
    [JsonProperty("attack_range")] public float  AttackRange;
}

/// <summary>Response từ hero/list — owned + lineup (không có shards).</summary>
[Serializable]
public class HeroListResponse
{
    [JsonProperty("owned")]  public HeroInstance[] Owned;
    /// <summary>Mảng 2 phần tử — uid hoặc null (slot trống).</summary>
    [JsonProperty("lineup")] public string[]       Lineup;
}

/// <summary>Embedded trong player/me — owned + lineup + shards.</summary>
[Serializable]
public class HeroInventory
{
    [JsonProperty("owned")]  public HeroInstance[]          Owned;
    /// <summary>Mảng 2 phần tử — uid hoặc null (slot trống).</summary>
    [JsonProperty("lineup")] public string[]                Lineup;
    /// <summary>hero_id (string) → số shard. VD: { "101": 3 }</summary>
    [JsonProperty("shards")] public Dictionary<string, int> Shards;
}

[Serializable]
public class HeroSetLineupResponse
{
    [JsonProperty("updated")] public bool     Updated;
    /// <summary>Trạng thái lineup sau khi cập nhật — dùng để update UI trực tiếp.</summary>
    [JsonProperty("lineup")]  public string[] Lineup;
}

[Serializable]
public class SetLineupRequest
{
    /// <summary>Mảng 2 phần tử — uid hoặc null.</summary>
    [JsonProperty("lineup")] public string[] Lineup;
}
