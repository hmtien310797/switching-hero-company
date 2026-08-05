using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Leaderboard (bảng xếp hạng PvP Main) service. Phase-1 dùng <c>LocalPvPLeaderboardService</c>
    /// (mock). Khi server làm xong, thay bằng impl gọi RPC — view không đổi.
    /// </summary>
    public interface IPvPLeaderboardService
    {
        /// <summary>Lấy toàn bộ data leaderboard (serverTime, weeklyReset, top3, rankings, myRank).</summary>
        UniTask<PvpLeaderboardResponseModel> GetLeaderboardAsync(CancellationToken token);
    }
}