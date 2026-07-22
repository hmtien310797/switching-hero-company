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

// ── leaderboard/season_reward/state, leaderboard/season_reward/claim ────────
// Xem nakama/src/handler/leaderboard.js — thưởng cuối mùa không đi qua mailbox (chưa có UI):
// server giữ 1 "pending reward" duy nhất/người, client tự hỏi + tự claim ngay trong màn Leaderboard.
// Qua khỏi expire_at mà chưa claim thì server tự cộng thẳng vào bag ở lần player/me kế tiếp —
// claim ở đây chỉ là đường tắt để người chơi thấy ngay, không claim cũng không mất thưởng.

[Serializable]
public class LeaderboardSeasonRewardItemDto
{
    [JsonProperty("item_key")] public string ItemKey;
    [JsonProperty("amount")]   public double Amount;
}

[Serializable]
public class LeaderboardSeasonRewardStateResponse
{
    [JsonProperty("has_reward")]    public bool HasReward;
    [JsonProperty("rank")]          public int  Rank;
    [JsonProperty("stage")]         public int  Stage;
    [JsonProperty("rewards")]       public List<LeaderboardSeasonRewardItemDto> Rewards;
    [JsonProperty("season_end_at")] public long SeasonEndAt;
    [JsonProperty("expire_at")]     public long ExpireAt;
}

[Serializable]
public class LeaderboardSeasonRewardClaimResponse
{
    [JsonProperty("success")] public bool   Success;
    /// <summary>"NO_REWARD" | "EXPIRED" khi Success = false.</summary>
    [JsonProperty("error")]   public string Error;
    [JsonProperty("rewards")] public List<LeaderboardSeasonRewardItemDto> Rewards;
    [JsonProperty("updated_resources")] public BattleUpdatedResources UpdatedResources;
}
