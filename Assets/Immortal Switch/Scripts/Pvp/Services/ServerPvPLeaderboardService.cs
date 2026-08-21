using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Server-backed leaderboard — thay <see cref="LocalPvPLeaderboardService"/>. Gọi pvp/leaderboard/
    /// top + pvp/leaderboard/around_me (xem handler/pvp.js).
    /// <para>
    /// <b>Field còn thiếu so với contract cũ (không fake số):</b> server chưa lưu formation/hero data
    /// theo leaderboard record, nên <see cref="PvpLeaderboardEntryModel.FormationPower"/>/
    /// <see cref="PvpLeaderboardEntryModel.RankTierId"/>/<see cref="PvpLeaderboardEntryModel.RankTierLevel"/>/
    /// <see cref="PvpLeaderboardEntryModel.FormationHeroes"/> luôn rỗng/mặc định cho MỌI record trừ
    /// MyRank (tier của bản thân lấy được từ pvp/state qua IPvPProfileService). UI cần ẩn/không hiển
    /// thị các field này cho tới khi server bổ sung — xem class header cho lý do (chưa scope hôm nay).
    /// WeeklyResetAtUtc luôn 0 — chưa có season config server-side (xem handler/pvp.js file header).
    /// </para>
    /// </summary>
    internal sealed class ServerPvPLeaderboardService : IPvPLeaderboardService
    {
        private const int TopCount = 50;
        private const int AroundMeCount = 5;

        private readonly IPvPProfileService _profile;

        public ServerPvPLeaderboardService(IPvPProfileService profile)
        {
            _profile = profile;
        }

        public async UniTask<PvpLeaderboardResponseModel> GetLeaderboardAsync(CancellationToken token)
        {
            var top = await NakamaClient.Instance.PvpLeaderboardTopAsync(TopCount);
            var aroundMe = await NakamaClient.Instance.PvpLeaderboardAroundMeAsync(AroundMeCount);

            var rankings = top.Records.Select(MapRecord).ToList();

            var myUserId = NakamaClient.Instance.Session?.UserId;
            var myRecord = aroundMe.Records.FirstOrDefault(r => r.UserId == myUserId);

            return new PvpLeaderboardResponseModel
            {
                ServerTimeUtc = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                WeeklyResetAtUtc = 0, // chưa có season config server-side — xem class header
                Top1 = rankings.Count > 0 ? rankings[0] : null,
                Top2 = rankings.Count > 1 ? rankings[1] : null,
                Top3 = rankings.Count > 2 ? rankings[2] : null,
                Rankings = rankings,
                MyRank = myRecord != null ? MapRecord(myRecord) : BuildUnrankedMyRank()
            };
        }

        private static PvpLeaderboardEntryModel MapRecord(PvpLeaderboardRecord r)
        {
            return new PvpLeaderboardEntryModel
            {
                PlayerId = r.UserId,
                Nickname = r.DisplayName,
                Rank = r.Rank,
                RankPoints = r.RankPoints,
                IsRanked = true,
                // FormationPower/RankTierId/RankTierLevel/FormationHeroes: xem class header — server
                // chưa trả, cố tình để trống thay vì bịa số.
                FormationPower = string.Empty,
                RankTierId = null,
                RankTierLevel = 0,
                FormationHeroes = new()
            };
        }

        /// <summary>Người chơi chưa từng thắng/thua trận PvP nào chưa có record trên leaderboard
        /// (writePvpLeaderboardRecord chỉ ghi sau pvp/battle/end) — dùng rank/tier hiện biết từ
        /// pvp/state qua profile cache, đánh dấu IsRanked = false thay vì mock rank giả như bản Local.</summary>
        private PvpLeaderboardEntryModel BuildUnrankedMyRank()
        {
            var profile = _profile?.GetCurrent();
            return new PvpLeaderboardEntryModel
            {
                PlayerId = NakamaClient.Instance.Session?.UserId,
                Nickname = string.IsNullOrEmpty(profile?.DisplayName) ? "PlayerName" : profile.DisplayName,
                Rank = 0,
                RankPoints = profile?.RankPoint ?? 0,
                IsRanked = false,
                FormationPower = string.Empty,
                RankTierId = TierIdToString(profile?.RankTier ?? PvpRankTier.Bronze),
                RankTierLevel = 0,
                FormationHeroes = new()
            };
        }

        private static string TierIdToString(PvpRankTier tier)
        {
            switch (tier)
            {
                case PvpRankTier.Silver:   return "silver";
                case PvpRankTier.Gold:     return "gold";
                case PvpRankTier.Platinum: return "platinum";
                case PvpRankTier.Diamond:  return "diamond";
                default:                   return "bronze";
            }
        }
    }
}
