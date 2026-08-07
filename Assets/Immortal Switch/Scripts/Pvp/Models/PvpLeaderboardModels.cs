using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Immortal_Switch.Scripts.Pvp.Models
{
    /// <summary>
    /// Leaderboard response cho PvP Main view (server contract). Shape khớp chính xác với JSON mà
    /// server sẽ trả — client Phase-1 dùng <c>LocalPvPLeaderboardService</c> để mock.
    /// <para>TODO server: serverTimeUtc / weeklyResetAtUtc / top1-3 / rankings[50] / myRank hiện do
    /// client mock.</para>
    /// </summary>
    [Serializable]
    public sealed class PvpLeaderboardResponseModel
    {
        [JsonProperty("serverTimeUtc")] public long ServerTimeUtc;
        [JsonProperty("weeklyResetAtUtc")] public long WeeklyResetAtUtc;

        [JsonProperty("top1")] public PvpLeaderboardEntryModel Top1;
        [JsonProperty("top2")] public PvpLeaderboardEntryModel Top2;
        [JsonProperty("top3")] public PvpLeaderboardEntryModel Top3;

        [JsonProperty("rankings")] public List<PvpLeaderboardEntryModel> Rankings = new();

        [JsonProperty("myRank")] public PvpLeaderboardEntryModel MyRank;
    }

    /// <summary>Một dòng bảng xếp hạng PvP.</summary>
    [Serializable]
    public sealed class PvpLeaderboardEntryModel
    {
        [JsonProperty("playerId")] public string PlayerId;
        [JsonProperty("nickname")] public string Nickname;
        [JsonProperty("rank")] public int Rank;
        [JsonProperty("formationPower")] public string FormationPower;
        //bronze, silver, gold, ruby, diamond,....
        [JsonProperty("rankTierId")] public string RankTierId;
        [JsonProperty("rankTierLevel")] public int RankTierLevel;
        [JsonProperty("rankPoints")] public int RankPoints;
        [JsonProperty("isRanked")] public bool IsRanked;

        [JsonProperty("formationHeroes")] public List<PvpLeaderboardHeroModel> FormationHeroes = new();
    }

    /// <summary>Hero trong đội hình của một dòng leaderboard.</summary>
    [Serializable]
    public sealed class PvpLeaderboardHeroModel
    {
        [JsonProperty("heroId")] public int HeroId;
        [JsonProperty("formationSlot")] public int FormationSlot;
        [JsonProperty("star")] public int Star;
        [JsonProperty("tier")] public string Tier;
    }
}