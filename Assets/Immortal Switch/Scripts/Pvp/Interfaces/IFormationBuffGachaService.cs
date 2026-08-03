using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Models;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Formation Buff Gacha service (DOCX §22). Phase-1 = Local; Phase-2 server thay (rates/
    /// choices/cost/pity/currency authoritative server-side). Gacha RNG tách combat RNG (DOCX §20).
    /// Pending roll phải survive restart; không tạo roll mới khi pending chưa resolve; idempotent
    /// select (cùng RollId → cùng result, không grant 2 lần) (DOCX §24, §27).
    /// </summary>
    public interface IFormationBuffGachaService
    {
        UniTask<FormationBuffRollSession> CreateRollAsync(string poolId, CancellationToken token);

        UniTask<FormationBuffRollResolveResult> SelectAsync(string rollId, string selectedBuffId, CancellationToken token);

        UniTask<FormationBuffGachaState> LoadStateAsync(CancellationToken token);

        /// <summary>Đọc pending roll session hiện tại (FLAGGED convenience — DOCX §24 yêu cầu pending
        /// survive restart; service expose session để UI restore 3 choices). Null nếu không có.</summary>
        FormationBuffRollSession GetPendingRoll();
    }
}
