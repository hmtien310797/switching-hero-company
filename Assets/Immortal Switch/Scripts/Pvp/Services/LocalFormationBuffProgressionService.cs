using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 buff progression (upgrade) service (DOCX §22, §28). Atomic: validate ownership/
    /// level/shards/token/max → deduct → increment level → save. Idempotent by transactionId.
    /// Upgrade cost formula = §28 config examples (FLAGGED: formula-based; SO-driven configs deferred).
    /// </summary>
    internal sealed class LocalFormationBuffProgressionService : IFormationBuffProgressionService
    {
        private readonly IPvPRepository _repo;
        private readonly IFormationBuffCatalogService _catalog;
        private readonly IPvPProfileService _profile;

        public LocalFormationBuffProgressionService(IPvPRepository repo, IFormationBuffCatalogService catalog, IPvPProfileService profile)
        {
            _repo = repo;
            _catalog = catalog;
            _profile = profile;
        }

        public UniTask<FormationBuffUpgradePreview> PreviewUpgradeAsync(string buffId, CancellationToken token)
        {
            var preview = new FormationBuffUpgradePreview { BuffId = buffId };
            var owned = _repo.Load<PvpOwnedBuffData>(PvpEs3Keys.OwnedBuffs);
            var ob = owned.Buffs.Find(b => b != null && b.BuffId == buffId);

            if (ob == null || !ob.IsUnlocked) { preview.CanUpgrade = false; preview.Reason = "Buff not owned."; return UniTask.FromResult(preview); }
            if (!_catalog.TryGet(buffId, out var def)) { preview.CanUpgrade = false; preview.Reason = "Buff not in catalog."; return UniTask.FromResult(preview); }

            preview.CurrentLevel = ob.Level;
            preview.NextLevel = ob.Level + 1;
            int reqShard = RequiredShards(ob.Level);
            long reqToken = RequiredArenaToken(ob.Level);
            preview.RequiredShards = reqShard;
            preview.RequiredArenaToken = reqToken;

            var profile = _profile.GetCurrent();
            bool can = ob.Level < def.MaxLevel && ob.Shards >= reqShard && profile != null && profile.ArenaToken >= reqToken;
            preview.CanUpgrade = can;
            if (!can)
                preview.Reason = ob.Level >= def.MaxLevel ? "Already at max level."
                    : ob.Shards < reqShard ? "Not enough shards."
                    : "Not enough Arena Token.";

            // Effect values preview (current vs next).
            if (def.StatEffects != null)
            {
                foreach (var eff in def.StatEffects)
                {
                    if (eff == null) continue;
                    preview.CurrentValues.Add($"{eff.StatType}: {eff.GetValue(ob.Level)}");
                    if (can) preview.NextValues.Add($"{eff.StatType}: {eff.GetValue(ob.Level + 1)}");
                }
            }

            return UniTask.FromResult(preview);
        }

        public UniTask<FormationBuffUpgradeResult> UpgradeAsync(string transactionId, string buffId, CancellationToken token)
        {
            if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(buffId))
                return UniTask.FromResult(new FormationBuffUpgradeResult { TransactionId = transactionId, BuffId = buffId, Success = false, Message = "Invalid args." });

            // Idempotency (DOCX §27).
            var processed = _repo.Load<PvpProcessedTransactionsData>(PvpEs3Keys.ProcessedTransactions);
            if (processed.IsProcessed(transactionId))
                return UniTask.FromResult(new FormationBuffUpgradeResult { TransactionId = transactionId, BuffId = buffId, Success = true, Message = "Already processed." });

            var owned = _repo.Load<PvpOwnedBuffData>(PvpEs3Keys.OwnedBuffs);
            var ob = owned.Buffs.Find(b => b != null && b.BuffId == buffId);
            if (ob == null || !ob.IsUnlocked)
                return UniTask.FromResult(Fail(transactionId, buffId, "Buff not owned."));

            if (!_catalog.TryGet(buffId, out var def))
                return UniTask.FromResult(Fail(transactionId, buffId, "Buff not in catalog."));

            if (ob.Level >= def.MaxLevel)
                return UniTask.FromResult(Fail(transactionId, buffId, "Already at max level."));

            int reqShard = RequiredShards(ob.Level);
            long reqToken = RequiredArenaToken(ob.Level);
            if (ob.Shards < reqShard)
                return UniTask.FromResult(Fail(transactionId, buffId, "Not enough shards."));
            var profile = _profile.GetCurrent();
            if (profile == null || profile.ArenaToken < reqToken)
                return UniTask.FromResult(Fail(transactionId, buffId, "Not enough Arena Token."));

            // Atomic: deduct shards + token, increment level, save all.
            ob.Shards -= reqShard;
            ob.Level += 1;
            profile.ArenaToken -= reqToken;

            _repo.Save(PvpEs3Keys.OwnedBuffs, owned);
            _profile.SaveAsync(profile, token).Forget();   // persist + keep cache in sync (same ref)
            processed.MarkProcessed(transactionId);
            _repo.Save(PvpEs3Keys.ProcessedTransactions, processed);

            return UniTask.FromResult(new FormationBuffUpgradeResult
            {
                TransactionId = transactionId,
                BuffId = buffId,
                Success = true,
                NewLevel = ob.Level,
                Message = $"Upgraded to Lv.{ob.Level}."
            });
        }

        private static FormationBuffUpgradeResult Fail(string tx, string buffId, string msg) =>
            new() { TransactionId = tx, BuffId = buffId, Success = false, Message = msg };

        // §28 config examples (FLAGGED: formula; SO-driven configs deferred).
        private static int RequiredShards(int fromLevel) => 10 * (1 << (fromLevel - 1));
        private static long RequiredArenaToken(int fromLevel) => 500L * (1L << (fromLevel - 1));
    }
}
