using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── event/config_windows ─────────────────────────────────────────────────────
// Snapshot nhẹ, dùng chung cho mọi event_id trong game_config_event.js — xem handler/event_config.js.

[Serializable]
public class EventConfigWindowDto
{
    [JsonProperty("event_id")]  public int  EventId;
    [JsonProperty("is_active")] public bool IsActive;
    [JsonProperty("start_ms")]  public long StartMs;
    [JsonProperty("end_ms")]    public long EndMs;
}

[Serializable]
public class EventConfigWindowsResponse
{
    [JsonProperty("events")] public List<EventConfigWindowDto> Events;
}
