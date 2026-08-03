using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Snapshot;

namespace Immortal_Switch.Scripts.Pvp.Interfaces
{
    /// <summary>
    /// Matchmaking service (DOCX §5, §31). Phase-1 = MockPvpMatchmakingService (local). FindMatchAsync:
    /// validate formation + ticket → select mock opponent → consume ticket → tạo BattleId + RandomSeed
    /// + immutable snapshot → save PendingBattle. Phase-2 server thay (ServerPvPMatchmakingService).
    /// </summary>
    public interface IPvPMatchmakingService
    {
        UniTask<HeroVsHeroBattleSnapshot> FindMatchAsync(CancellationToken token);
    }
}
