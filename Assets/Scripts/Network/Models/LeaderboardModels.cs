using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ── leaderboard/stage/top, leaderboard/stage/around_me ──────────────────────
// Xem nakama/src/handler/leaderboard.js — bảng xếp hạng theo highest_stage_cleared,
// dùng leaderboard built-in của Nakama (sortOrder desc, operator best).

[Serializable]
public class LeaderboardRecordDto
{
    [JsonProperty("user_id")]      public string UserId;
    [JsonProperty("display_name")] public string DisplayName;
    /// <summary>highest_stage_cleared tại thời điểm ghi — điểm xếp hạng.</summary>
    [JsonProperty("stage")]        public int    Stage;
    [JsonProperty("rank")]         public int    Rank;
}

[Serializable]
public class LeaderboardStageTopResponse
{
    [JsonProperty("records")]     public List<LeaderboardRecordDto> Records;
    [JsonProperty("next_cursor")] public string                     NextCursor;
    [JsonProperty("prev_cursor")] public string                     PrevCursor;
    /// <summary>Unix seconds của lần reset mùa kế tiếp — dùng để hiện countdown "mùa kết thúc trong...". Có thể null nếu config season_reset_time thiếu/sai định dạng.</summary>
    [JsonProperty("season_end_at")] public long? SeasonEndAt;
}

[Serializable]
public class LeaderboardStageAroundMeResponse
{
    [JsonProperty("records")] public List<LeaderboardRecordDto> Records;
}

// Thưởng cuối mùa không còn RPC/claim riêng — server gửi thẳng qua mailbox (mail/list, mail/claim)
// ngay khi mùa khóa, xem nakama/src/handler/leaderboard.js sendSeasonRewardMailIfNeeded.
