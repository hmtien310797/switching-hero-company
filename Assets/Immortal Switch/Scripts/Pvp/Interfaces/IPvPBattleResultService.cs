using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Battle;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Battle result service (DOCX §5, §15). SubmitResultAsync: idempotent per BattleId — apply
    /// rank/rewards/history exactly once, mark pending battle resolved. Phase-1 = Local; Phase-2 server.
    /// </summary>
    public interface IPvPBattleResultService
    {
        UniTask<PvPBattleResolveResult> SubmitResultAsync(PvPBattleResultRequest request, CancellationToken token);

        /// <summary>Đánh dấu battle surrender (rời sau BattleId → Defeat, không hoàn ticket, DOCX §2/§15).</summary>
        UniTask<PvPBattleResolveResult> SurrenderAsync(string battleId, CancellationToken token);
    }
}
