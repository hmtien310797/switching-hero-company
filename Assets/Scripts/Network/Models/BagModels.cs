using System;
using Newtonsoft.Json;

[Serializable]
public class BagSlot
{
    [JsonProperty("item_id")]   public string ItemId;
    [JsonProperty("quantity")]  public int    Quantity;
    [JsonProperty("name")]      public string Name;
    [JsonProperty("type")]      public string Type;
    [JsonProperty("icon")]      public string Icon;
    [JsonProperty("max_stack")] public int    MaxStack;
}

[Serializable]
public class BagResponse
{
    [JsonProperty("items")] public BagSlot[] Items;
}
