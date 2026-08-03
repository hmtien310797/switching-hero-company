using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Profile service (DOCX §5). Load/save <see cref="PvpPlayerData"/> (rank, ticket, token, season).
    /// Phase-1 = LocalPvPProfileService (ES3 qua repo). <b>FLAGGED:</b> rank REWARD application
    /// (rank change sau battle) ở M5/M7; M2 chỉ load/save + ticket/token helper cho PvpMainView.
    /// </summary>
    public interface IPvPProfileService
    {
        UniTask<PvpPlayerData> LoadAsync(CancellationToken token);
        UniTask SaveAsync(PvpPlayerData data, CancellationToken token);

        /// <summary>Sync cached getter (FLAGGED: convenience — không có trong DOCX §5 nhưng UI cần).</summary>
        PvpPlayerData GetCurrent();

        void ConsumeTicket(int count = 1);
        void AddTickets(int count);
        void AddArenaToken(long amount);
    }
}
