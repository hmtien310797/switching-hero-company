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
    /// Server-backed battle result — thay <see cref="LocalPvpBattleResultService"/>. Rank/token/tier
    /// reward giờ do server tính (pvp/battle/end, xem handler/pvp.js rpcPvpBattleEnd) qua check hợp
    /// lý riêng của server — không còn tin ResolveReward cục bộ. Idempotency thật (theo BattleId) giờ
    /// nằm ở server (pending battle bị xoá sau khi resolve); PvpProcessedTransactionsData cục bộ chỉ
    /// còn tác dụng chặn double-submit trong 1 phiên (vd double-tap nút) trước khi request kịp tới
    /// server, không phải nguồn idempotency chính nữa.
    /// <para>
    /// Vẫn ghi <see cref="PvpBattleHistoryData"/> cục bộ (server pvp/state có history riêng nhưng
    /// đơn giản hơn — chưa có opponent info) để màn History không bị trống trong lúc UI chưa được nối
    /// sang đọc history từ server.
    /// </para>
    /// </summary>
    internal sealed class ServerPvpBattleResultService : IPvPBattleResultService
    {
        private readonly IPvPRepository _repo;
        private readonly IPvPProfileService _profile;

        public ServerPvpBattleResultService(IPvPRepository repo, IPvPProfileService profile)
        {
            _repo = repo;
            _profile = profile;
        }

        public async UniTask<PvPBattleResolveResult> SubmitResultAsync(PvPBattleResultRequest request, CancellationToken token)
        {
            if (request == null || string.IsNullOrEmpty(request.BattleId))
                return new PvPBattleResolveResult { Success = false, Message = "Invalid result request." };

            // Chặn double-submit trong phiên hiện tại trước khi request kịp tới server (server tự có
            // idempotency thật qua pending battle — đây chỉ là early-out cục bộ, không phải nguồn sự thật).
            var processed = _repo.Load<PvpProcessedTransactionsData>(PvpEs3Keys.ProcessedTransactions);
            if (processed.IsProcessed(request.BattleId))
                return PvPBattleResolveResult.AlreadyDone(request.BattleId);

            var pending = _repo.Load<PvpPendingBattleData>(PvpEs3Keys.PendingBattle);

            var serverResult = MapResultToServerString(request.Result);
            var response = await NakamaClient.Instance.PvpBattleEndAsync(new PvpBattleEndRequest
            {
                BattleId = request.BattleId,
                Result = serverResult
            });

            if (!response.Success)
            {
                // BATTLE_NOT_FOUND / BATTLE_EXPIRED / CHEAT_DETECTED — không ghi history, không mark
                // processed, để UI có thể xử lý lỗi (vd báo người chơi retry hoặc quay về main).
                return new PvPBattleResolveResult { Success = false, Message = response.Error };
            }

            if (_profile is ServerPvPProfileService serverProfile)
            {
                serverProfile.ApplyState(new PvpStateResponse
                {
                    RankPoints   = response.NewRankPoints,
                    TierId       = response.TierId,
                    TierName     = response.TierName,
                    TierLevel    = response.TierLevel,
                    Tickets      = response.Tickets,
                    MaxTickets   = PvpDefaults.MaxTicketsCap,
                    NextTicketAt = null, // không có trong response này — giữ nguyên cache cũ ở lần pvp/state kế tiếp
                    ArenaToken   = response.ArenaToken
                });
            }

            // History cục bộ (giữ hành vi UI như bản Local — xem class header).
            var history = _repo.Load<PvpBattleHistoryData>(PvpEs3Keys.BattleHistory);
            history.Entries.Add(new PvpBattleHistoryEntry
            {
                BattleId = request.BattleId,
                Result = request.Result.ToString(),
                OpponentName = "Mock Opponent", // FLAGGED: giống bản Local — opponent name không có trong request
                RankChange = response.RankDelta,
                DurationMs = request.DurationMs,
                RandomSeed = pending != null && pending.BattleId == request.BattleId ? pending.RandomSeed : 0,
                BattleRulesVersion = request.BattleRulesVersion,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SummaryJson = JsonConvert.SerializeObject(request)
            });
            while (history.Entries.Count > Math.Max(1, history.MaxRetention))
                history.Entries.RemoveAt(0);
            _repo.Save(PvpEs3Keys.BattleHistory, history);

            // Mark pending resolved + store result JSON (chỉ để UI/debug — server đã tự xoá pending của nó).
            if (pending != null && pending.BattleId == request.BattleId)
            {
                pending.IsResolved = true;
                pending.IsSurrendered = request.Result == PvPBattleResult.Surrender;
                pending.ResultJson = JsonConvert.SerializeObject(request);
                _repo.Save(PvpEs3Keys.PendingBattle, pending);
            }

            processed.MarkProcessed(request.BattleId);
            _repo.Save(PvpEs3Keys.ProcessedTransactions, processed);

            GameEventManager.Trigger(GameEvents.ON_PVP_BATTLE_END);
            return PvPBattleResolveResult.Ok(response.RankDelta, response.TokenDelta);
        }

        public UniTask<PvPBattleResolveResult> SurrenderAsync(string battleId, CancellationToken token)
        {
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

        // Server chỉ hiểu "Victory" | "Defeat" | "Draw" (xem handler/pvp.js) — Surrender map thành
        // Defeat, cùng cách bản Local xử lý Surrender (ResolveReward switch, không có case riêng).
        private static string MapResultToServerString(PvPBattleResult result)
        {
            switch (result)
            {
                case PvPBattleResult.Victory:   return "Victory";
                case PvPBattleResult.Draw:      return "Draw";
                case PvPBattleResult.Defeat:
                case PvPBattleResult.Surrender:
                default:                        return "Defeat";
            }
        }
    }
}
