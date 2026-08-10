using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;
using Newtonsoft.Json;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 local battle result service (DOCX §5, §15). Idempotent per BattleId: apply
    /// rank/rewards/history exactly once, mark pending resolved, mark processed transaction.
    /// Rank tier recomputed từ RankPoint (simple thresholds — FLAGGED).
    /// </summary>
    internal sealed class LocalPvpBattleResultService : IPvPBattleResultService
    {
        private readonly IPvPRepository _repo;
        private readonly IPvPProfileService _profile;

        public LocalPvpBattleResultService(IPvPRepository repo, IPvPProfileService profile)
        {
            _repo = repo;
            _profile = profile;
        }

        public UniTask<PvPBattleResolveResult> SubmitResultAsync(PvPBattleResultRequest request, CancellationToken token)
        {
            if (request == null || string.IsNullOrEmpty(request.BattleId))
                return UniTask.FromResult(new PvPBattleResolveResult
                    { Success = false, Message = "Invalid result request." });

            // Idempotency (DOCX §15/§36 — BattleId grants rank/rewards only once).
            var processed = _repo.Load<PvpProcessedTransactionsData>(PvpEs3Keys.ProcessedTransactions);
            if (processed.IsProcessed(request.BattleId))
                return UniTask.FromResult(PvPBattleResolveResult.AlreadyDone(request.BattleId));

            var pending = _repo.Load<PvpPendingBattleData>(PvpEs3Keys.PendingBattle);

            // Reward/rank (DOCX §32 — simulation config).
            (int rankDelta, long tokenDelta) = ResolveReward(request.Result);

            var profile = _profile.GetCurrent();
            if (profile != null)
            {
                ApplyRankChange(profile, rankDelta);
                profile.ArenaToken += tokenDelta;
                _repo.Save(PvpEs3Keys.PlayerData, profile);
            }

            // History (cap retention — Markdown §13).
            var history = _repo.Load<PvpBattleHistoryData>(PvpEs3Keys.BattleHistory);
            history.Entries.Add(new PvpBattleHistoryEntry
            {
                BattleId = request.BattleId,
                Result = request.Result.ToString(),
                OpponentName = "Mock Opponent", // FLAGGED: opponent name không có trong request (DOCX §34)
                RankChange = rankDelta,
                DurationMs = request.DurationMs,
                RandomSeed = pending != null && pending.BattleId == request.BattleId ? pending.RandomSeed : 0,
                BattleRulesVersion = request.BattleRulesVersion,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SummaryJson = JsonConvert.SerializeObject(request)
            });
            while (history.Entries.Count > Math.Max(1, history.MaxRetention))
                history.Entries.RemoveAt(0);
            _repo.Save(PvpEs3Keys.BattleHistory, history);

            // Mark pending resolved + store result JSON (DOCX §6).
            if (pending != null && pending.BattleId == request.BattleId)
            {
                pending.IsResolved = true;
                pending.IsSurrendered = request.Result == PvPBattleResult.Surrender;
                pending.ResultJson = JsonConvert.SerializeObject(request);
                _repo.Save(PvpEs3Keys.PendingBattle, pending);
            }

            // Mark processed.
            processed.MarkProcessed(request.BattleId);
            _repo.Save(PvpEs3Keys.ProcessedTransactions, processed);

            GameEventManager.Trigger(GameEvents.ON_PVP_BATTLE_END);
            return UniTask.FromResult(PvPBattleResolveResult.Ok(rankDelta, tokenDelta));
        }

        public UniTask<PvPBattleResolveResult> SurrenderAsync(string battleId, CancellationToken token)
        {
            // Surrender = Defeat (DOCX §2 Exit rule, §15).
            var request = new PvPBattleResultRequest
            {
                BattleId = battleId,
                Result = PvPBattleResult.Surrender,
                DurationMs = 0,
                CombatLogHash = PvpCombatLogHasher.Hash("surrender:" + battleId),
                BattleRulesVersion = 1,
                SnapshotVersion = 1
            };
            return SubmitResultAsync(request, token);
        }

        private static (int rankDelta, long tokenDelta) ResolveReward(PvPBattleResult result)
        {
            return result switch
            {
                PvPBattleResult.Victory => (PvpDefaults.VictoryRankGain, PvpDefaults.VictoryArenaToken),
                PvPBattleResult.Defeat => (-PvpDefaults.DefeatRankLoss, PvpDefaults.DefeatArenaToken),
                PvPBattleResult.Surrender => (-PvpDefaults.DefeatRankLoss, PvpDefaults.DefeatArenaToken),
                _ => (0, 0)
            };
        }

        private static void ApplyRankChange(PvpPlayerData p, int delta)
        {
            p.RankPoint = Math.Max(0, p.RankPoint + delta);
            p.RankTier = TierFor(p.RankPoint);
        }

        // FLAGGED: simple tier thresholds (DOCX không định nghĩa thresholds; Markdown/wireframe Bronze→Diamond).
        private static PvpRankTier TierFor(int points)
        {
            if (points >= 2000) return PvpRankTier.Diamond;
            if (points >= 1500) return PvpRankTier.Platinum;
            if (points >= 1000) return PvpRankTier.Gold;
            if (points >= 500) return PvpRankTier.Silver;
            return PvpRankTier.Bronze;
        }
    }
}
