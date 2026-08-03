using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Mock;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;
using Immortal_Switch.Scripts.Pvp.Snapshot;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

namespace Immortal_Switch.Scripts.Pvp.DevTools
{
    /// <summary>
    /// Dev/QA tools (DOCX §14 Markdown, §41 QA). Add currency/shards/tickets, unlock all buffs,
    /// regenerate opponents, export/import BattleSnapshot JSON, clear PvP ES3, replay (re-simulate),
    /// simulate 1000 rolls distribution. Phase-1 local.
    /// </summary>
    public static class PvpDebugService
    {
        public static void AddArenaToken(long amount) => PvpManager.Instance?.Facade?.Profile?.AddArenaToken(amount);
        public static void AddTickets(int count) => PvpManager.Instance?.Facade?.Profile?.AddTickets(count);
        public static void AddShards(string buffId, int amount) => PvpManager.Instance?.Facade?.BuffInventory?.AddShards(buffId, amount);

        public static void UnlockAllBuffs()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null) return;
            var owned = facade.BuffInventory.LoadOwnedBuffs();
            foreach (var def in facade.BuffCatalog.GetAll())
            {
                if (def == null || string.IsNullOrEmpty(def.BuffId)) continue;
                if (!owned.Buffs.Exists(b => b != null && b.BuffId == def.BuffId))
                    owned.Buffs.Add(new PvpOwnedBuff { BuffId = def.BuffId, IsUnlocked = true, Level = 1, Shards = 0 });
            }
            facade.BuffInventory.SaveOwnedBuffs(owned);
        }

        public static void ClearAllPvpData() => PvpDevResetService.ResetAll();

        public static void RegenerateMockOpponents()
        {
            var facade = PvpManager.Instance?.Facade;
            if (facade == null) return;
            var repo = facade.Repository;
            int rank = facade.Profile.GetCurrent()?.RankPoint ?? 0;
            var data = repo.Load<PvpMockOpponentData>(PvpEs3Keys.MockOpponents);
            data.Opponents = MockPvpOpponentGenerator.Generate(MockPvpOpponentGenerator.DefaultCount, rank);
            repo.Save(PvpEs3Keys.MockOpponents, data);
        }

        /// <summary>Export pending BattleSnapshot JSON (DOCX §14 — "Export/Import BattleSnapshot JSON").</summary>
        public static string ExportSnapshotJson()
        {
            var repo = PvpManager.Instance?.Facade?.Repository;
            if (repo == null) return null;
            var pending = repo.Load<PvpPendingBattleData>(PvpEs3Keys.PendingBattle);
            return pending?.SnapshotJson;
        }

        public static HeroVsHeroBattleSnapshot ImportSnapshot(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<HeroVsHeroBattleSnapshot>(json); }
            catch (Exception e) { Debug.LogError($"[PvP] Import snapshot failed: {e.Message}"); return null; }
        }

        /// <summary>Re-simulate snapshot (deterministic — same seed → same outcome). Does NOT re-submit.</summary>
        public static PvPBattleResultRequest ReplaySnapshot(HeroVsHeroBattleSnapshot snapshot)
        {
            var controller = PvpManager.Instance?.BattleController;
            if (controller == null || snapshot == null) return null;
            return controller.Simulate(snapshot);
        }

        /// <summary>
        /// Lightweight rarity distribution test (DOCX §14 — "Simulate 1,000 Gacha Rolls"). <b>FLAGGED:</b>
        /// in-memory only (no ES3 writes); dùng default rates. Real roll distribution QA nên gọi
        /// CreateRoll+Select loop (heavier).
        /// </summary>
        public static string Simulate1000Rolls()
        {
            var counts = new int[5]; // Common..Legendary
            var rng = new Random();
            float[] rates = { 50f, 28f, 15f, 6f, 1f };
            const float total = 100f;
            for (int i = 0; i < 1000; i++)
            {
                float roll = (float)rng.NextDouble() * total;
                float acc = 0f;
                int picked = 0;
                for (int r = 0; r < 5; r++)
                {
                    acc += rates[r];
                    if (roll <= acc) { picked = r; break; }
                }
                counts[picked]++;
            }
            return $"1000 rolls: Common={counts[0]} Uncommon={counts[1]} Rare={counts[2]} Epic={counts[3]} Legendary={counts[4]}";
        }

        public static string VerifyLastResult()
        {
            var repo = PvpManager.Instance?.Facade?.Repository;
            if (repo == null) return "No repo.";
            var pending = repo.Load<PvpPendingBattleData>(PvpEs3Keys.PendingBattle);
            if (pending == null || string.IsNullOrEmpty(pending.ResultJson))
                return "No resolved battle to verify.";
            try
            {
                var req = JsonConvert.DeserializeObject<PvPBattleResultRequest>(pending.ResultJson);
                var report = Verify.LocalPvpBattleVerifier.Verify(req, repo, new PvPVerificationConfig());
                return report.ToString();
            }
            catch (Exception e) { return $"Verify failed: {e.Message}"; }
        }
    }
}
