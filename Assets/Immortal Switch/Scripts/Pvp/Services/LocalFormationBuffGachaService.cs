using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pvp.Data;
using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Mock;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;
using UnityEngine;
using Random = System.Random;

namespace Immortal_Switch.Scripts.Pvp.Services
{
    /// <summary>
    /// Phase-1 local gacha service (DOCX §20, §22, §24, §26, §27). 3 choices/pity/duplicate→shards/
    /// pending-roll recovery/idempotency. Gacha RNG tách combat RNG (§20) — seeded từ RollId cho
    /// repeatable QA. Phase-2 server thay (rates/choices/cost/pity authoritative server-side).
    /// </summary>
    internal sealed class LocalFormationBuffGachaService : IFormationBuffGachaService
    {
        private const string PoolResourcesPath = "PvP/FormationBuffRollPool";

        private readonly IPvPRepository _repo;
        private readonly IFormationBuffCatalogService _catalog;
        private readonly IPvPProfileService _profile;

        public LocalFormationBuffGachaService(IPvPRepository repo, IFormationBuffCatalogService catalog, IPvPProfileService profile)
        {
            _repo = repo;
            _catalog = catalog;
            _profile = profile;
        }

        public UniTask<FormationBuffGachaState> LoadStateAsync(CancellationToken token)
        {
            return UniTask.FromResult(_repo.Load<FormationBuffGachaState>(PvpEs3Keys.BuffGachaState));
        }

        public FormationBuffRollSession GetPendingRoll()
        {
            var pending = _repo.Load<PvpPendingBuffRollData>(PvpEs3Keys.PendingBuffRoll);
            return pending?.Pending;
        }

        public async UniTask<FormationBuffRollSession> CreateRollAsync(string poolId, CancellationToken token)
        {
            // §24: block new roll while pending unresolved.
            var pending = _repo.Load<PvpPendingBuffRollData>(PvpEs3Keys.PendingBuffRoll);
            if (pending?.Pending != null && !pending.Pending.IsResolved)
                throw new InvalidOperationException($"Cannot create roll — pending roll {pending.Pending.RollId} unresolved.");

            var pool = GetPool(poolId);
            var profile = _profile.GetCurrent();
            if (profile == null || profile.ArenaToken < pool.RollCost)
                throw new InvalidOperationException("Not enough Arena Token.");

            var gachaState = _repo.Load<FormationBuffGachaState>(PvpEs3Keys.BuffGachaState);
            var pity = GetOrAddPity(gachaState, poolId);

            // Gacha RNG — seeded từ RollId (separate from combat RNG, repeatable QA — §20/§26).
            string rollId = $"roll-{Guid.NewGuid():N}";
            var rng = new Random((int)(RandomSeedFactory.StableSeed(rollId) % int.MaxValue));

            var choices = GenerateChoices(pool, pity, rng);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var session = new FormationBuffRollSession
            {
                RollId = rollId,
                PoolId = poolId,
                CreatedAtUnix = now,
                ExpiresAtUnix = now + 24 * 3600,
                Cost = pool.RollCost,
                CurrencyId = pool.CurrencyType.ToString(),
                Choices = choices,
                IsResolved = false
            };

            // Deduct cost + save pending roll + gacha state (§27). Pity NOT reset on create (§26).
            profile.ArenaToken -= pool.RollCost;
            await _profile.SaveAsync(profile, token);

            _repo.Save(PvpEs3Keys.PendingBuffRoll, new PvpPendingBuffRollData { Pending = session });
            gachaState.PendingRollId = rollId;
            _repo.Save(PvpEs3Keys.BuffGachaState, gachaState);

            return session;
        }

        public async UniTask<FormationBuffRollResolveResult> SelectAsync(string rollId, string selectedBuffId, CancellationToken token)
        {
            var pending = _repo.Load<PvpPendingBuffRollData>(PvpEs3Keys.PendingBuffRoll);
            if (pending?.Pending == null || pending.Pending.RollId != rollId)
                throw new InvalidOperationException($"No pending roll {rollId}.");

            var session = pending.Pending;

            // Idempotent re-select (§24): return stored result, không grant lại.
            if (session.IsResolved && session.StoredResult != null)
                return session.StoredResult;

            FormationBuffRollChoice chosen = null;
            if (session.Choices != null)
                foreach (var c in session.Choices)
                    if (c != null && c.BuffId == selectedBuffId) { chosen = c; break; }
            if (chosen == null)
                throw new InvalidOperationException($"Selected buff {selectedBuffId} is not among the choices.");

            var owned = _repo.Load<PvpOwnedBuffData>(PvpEs3Keys.OwnedBuffs);
            var ob = owned.Buffs.Find(b => b != null && b.BuffId == selectedBuffId);
            var pool = GetPool(session.PoolId);

            bool isNew = ob == null || !ob.IsUnlocked;
            var result = new FormationBuffRollResolveResult
            {
                RollId = rollId,
                SelectedBuffId = selectedBuffId,
                SelectedRarity = chosen.Rarity
            };

            if (isNew)
            {
                // Grant new ownership (DOCX §10 gacha — IsUnlocked=true, init level 1).
                if (ob == null) { ob = new PvpOwnedBuff { BuffId = selectedBuffId }; owned.Buffs.Add(ob); }
                ob.IsUnlocked = true;
                ob.Level = 1;
                result.GrantedNew = true;
                result.Message = $"New buff unlocked: {selectedBuffId}.";
            }
            else
            {
                // Duplicate → shards (DOCX §21). No second instance.
                int shard = pool.Duplicate != null ? pool.Duplicate.GetShardValue(chosen.Rarity) : 0;
                ob.Shards += shard;
                result.IsDuplicate = true;
                result.ShardsGained = shard;
                result.Message = $"Duplicate → {shard} shards.";
            }
            _repo.Save(PvpEs3Keys.OwnedBuffs, owned);

            // Advance pity based on selected rarity (§26 — reset/advance after select, not on create).
            var gachaState = _repo.Load<FormationBuffGachaState>(PvpEs3Keys.BuffGachaState);
            var pity = GetOrAddPity(gachaState, session.PoolId);
            AdvancePity(pity, chosen.Rarity);

            // Roll history (QA/debug — §24).
            var rollHistory = _repo.Load<PvpBuffRollHistoryData>(PvpEs3Keys.BuffRollHistory);
            session.IsResolved = true;
            session.SelectedBuffId = selectedBuffId;
            session.StoredResult = result;
            rollHistory.Entries.Add(CloneForHistory(session));
            while (rollHistory.Entries.Count > Math.Max(1, rollHistory.MaxRetention))
                rollHistory.Entries.RemoveAt(0);

            gachaState.PendingRollId = null;
            _repo.Save(PvpEs3Keys.BuffGachaState, gachaState);
            _repo.Save(PvpEs3Keys.BuffRollHistory, rollHistory);

            // Mark processed + clear pending (§27 — clear pending roll after resolve).
            var processed = _repo.Load<PvpProcessedTransactionsData>(PvpEs3Keys.ProcessedTransactions);
            processed.MarkProcessed(rollId);
            _repo.Save(PvpEs3Keys.ProcessedTransactions, processed);

            pending.Pending = null;
            _repo.Save(PvpEs3Keys.PendingBuffRoll, pending);

            GameEventManager.Trigger(GameEvents.ON_PVP_GACHA_ROLL_RESOLVED);
            return result;
        }

        // ── Pool config (SO from Resources or default fallback) ─────────────────

        private FormationBuffRollPoolConfigSO GetPool(string poolId)
        {
            var so = Resources.Load<FormationBuffRollPoolConfigSO>(PoolResourcesPath);
            if (so != null && (string.IsNullOrEmpty(poolId) || so.PoolId == poolId))
                return so;

            // FLAGGED: SO chưa tạo — fallback default pool (rates per DOCX §25 defaults).
            return BuildDefaultPool(poolId);
        }

        private FormationBuffRollPoolConfigSO BuildDefaultPool(string poolId)
        {
            var so = ScriptableObject.CreateInstance<FormationBuffRollPoolConfigSO>();
            so.PoolId = string.IsNullOrEmpty(poolId) ? "standard" : poolId;
            so.RollCost = 500;
            so.CurrencyType = PvpCurrencyType.ArenaToken;
            so.RarityRates = new[]
            {
                new FormationBuffRarityRate { Rarity = BuffRarity.Common, Rate = 50f, Enabled = true },
                new FormationBuffRarityRate { Rarity = BuffRarity.Uncommon, Rate = 28f, Enabled = true },
                new FormationBuffRarityRate { Rarity = BuffRarity.Rare, Rate = 15f, Enabled = true },
                new FormationBuffRarityRate { Rarity = BuffRarity.Epic, Rate = 6f, Enabled = true },
                new FormationBuffRarityRate { Rarity = BuffRarity.Legendary, Rate = 1f, Enabled = true }
            };
            var included = new List<string>();
            foreach (var id in PvpTestBuffIds.All) included.Add(id);
            so.IncludedBuffIds = included.ToArray();
            return so;
        }

        // ── Choice generation (§26 pity algorithm) ───────────────────────────────

        private FormationBuffRollChoice[] GenerateChoices(FormationBuffRollPoolConfigSO pool, FormationBuffPityState pity, Random rng)
        {
            var choices = new FormationBuffRollChoice[3];
            var used = new HashSet<string>();
            var owned = _repo.Load<PvpOwnedBuffData>(PvpEs3Keys.OwnedBuffs);

            for (int i = 0; i < 3; i++)
            {
                BuffRarity rarity = RollRarity(pool, pity, rng);
                string buffId = PickBuffOfRarity(pool, rarity, used, rng);
                if (string.IsNullOrEmpty(buffId))
                    buffId = PickAnyUnused(pool, used, rng);   // fallback if no buff of rolled rarity
                if (!string.IsNullOrEmpty(buffId)) used.Add(buffId);

                bool alreadyOwned = false;
                int dupShard = pool.Duplicate != null ? pool.Duplicate.GetShardValue(rarity) : 0;
                if (!string.IsNullOrEmpty(buffId))
                    alreadyOwned = owned.Buffs.Find(b => b != null && b.BuffId == buffId)?.IsUnlocked == true;

                choices[i] = new FormationBuffRollChoice
                {
                    BuffId = buffId,
                    Rarity = rarity,
                    IsAlreadyOwned = alreadyOwned,
                    DuplicateShardValue = dupShard
                };
            }
            return choices;
        }

        private BuffRarity RollRarity(FormationBuffRollPoolConfigSO pool, FormationBuffPityState pity, Random rng)
        {
            // Hard pity (§26): force min rarity.
            if (pool.Pity != null && pity.RollsSinceLegendary >= pool.Pity.HardPityThreshold)
                return pool.Pity.ForceMinimumRarity;

            float total = 0f;
            if (pool.RarityRates != null)
                foreach (var r in pool.RarityRates)
                    if (r != null && r.Enabled && r.Rate > 0f) total += r.Rate;

            float roll = (float)rng.NextDouble() * total;
            float acc = 0f;
            BuffRarity picked = BuffRarity.Common;
            if (pool.RarityRates != null)
            {
                foreach (var r in pool.RarityRates)
                {
                    if (r == null || !r.Enabled || r.Rate <= 0f) continue;
                    acc += r.Rate;
                    if (roll <= acc) { picked = r.Rarity; break; }
                }
            }

            // Soft pity (§26): past soft threshold + Common → bump to Uncommon.
            if (pool.Pity != null && pity.RollsSinceRareOrHigher >= pool.Pity.SoftPityThreshold && picked == BuffRarity.Common)
                picked = BuffRarity.Uncommon;

            return picked;
        }

        private string PickBuffOfRarity(FormationBuffRollPoolConfigSO pool, BuffRarity rarity, HashSet<string> used, Random rng)
        {
            var candidates = new List<string>();
            if (pool.IncludedBuffIds != null)
            {
                foreach (var id in pool.IncludedBuffIds)
                {
                    if (string.IsNullOrEmpty(id) || used.Contains(id)) continue;
                    if (_catalog.TryGet(id, out var def) && def.Rarity == rarity)
                        candidates.Add(id);
                }
            }
            if (candidates.Count == 0) return null;
            return candidates[rng.Next(candidates.Count)];
        }

        private string PickAnyUnused(FormationBuffRollPoolConfigSO pool, HashSet<string> used, Random rng)
        {
            var candidates = new List<string>();
            if (pool.IncludedBuffIds != null)
                foreach (var id in pool.IncludedBuffIds)
                    if (!string.IsNullOrEmpty(id) && !used.Contains(id)) candidates.Add(id);
            if (candidates.Count == 0) return PvpTestBuffIds.IronCore;
            return candidates[rng.Next(candidates.Count)];
        }

        private static FormationBuffPityState GetOrAddPity(FormationBuffGachaState state, string poolId)
        {
            if (state.PityByPool == null) state.PityByPool = new Dictionary<string, FormationBuffPityState>();
            if (!state.PityByPool.TryGetValue(poolId, out var pity))
            {
                pity = new FormationBuffPityState { PoolId = poolId };
                state.PityByPool[poolId] = pity;
            }
            return pity;
        }

        private static void AdvancePity(FormationBuffPityState pity, BuffRarity selected)
        {
            pity.TotalRollCount++;
            if (selected >= BuffRarity.Rare) pity.RollsSinceRareOrHigher = 0; else pity.RollsSinceRareOrHigher++;
            if (selected >= BuffRarity.Epic) pity.RollsSinceEpicOrHigher = 0; else pity.RollsSinceEpicOrHigher++;
            if (selected == BuffRarity.Legendary) pity.RollsSinceLegendary = 0; else pity.RollsSinceLegendary++;
        }

        private static FormationBuffRollSession CloneForHistory(FormationBuffRollSession s)
        {
            // Shallow clone cho history (Choices ref shared — read-only history).
            return new FormationBuffRollSession
            {
                RollId = s.RollId,
                PoolId = s.PoolId,
                CreatedAtUnix = s.CreatedAtUnix,
                ExpiresAtUnix = s.ExpiresAtUnix,
                Cost = s.Cost,
                CurrencyId = s.CurrencyId,
                Choices = s.Choices,
                IsResolved = true,
                SelectedBuffId = s.SelectedBuffId
            };
        }
    }
}
