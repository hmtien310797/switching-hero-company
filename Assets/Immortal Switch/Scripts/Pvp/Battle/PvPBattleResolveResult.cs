using System;

namespace Immortal_Switch.Scripts.Pvp.Battle
{
    /// <summary>
    /// Kết quả resolve local result (DOCX §5 — IPvPBattleResultService.SubmitResultAsync trả về).
    /// AlreadyResolved = true khi BattleId đã được xử lý (idempotent, §15/§36).
    /// </summary>
    [Serializable]
    public sealed class PvPBattleResolveResult
    {
        public bool Success;
        public bool AlreadyResolved;
        public int RankChange;
        public long ArenaTokenDelta;
        public string Message;

        public static PvPBattleResolveResult AlreadyDone(string battleId) =>
            new() { Success = true, AlreadyResolved = true, Message = $"Battle {battleId} already resolved." };

        public static PvPBattleResolveResult Ok(int rankChange, long tokenDelta) =>
            new() { Success = true, AlreadyResolved = false, RankChange = rankChange, ArenaTokenDelta = tokenDelta };
    }
}
