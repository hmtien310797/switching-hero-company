using Common;
using Immortal_Switch.Scripts.Pvp.Models;
using Immortal_Switch.Scripts.Pvp.Repositories;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp
{
    /// <summary>
    /// First-run bootstrap: nếu chưa có key <see cref="PvpEs3Keys.PlayerData"/> thì tạo default
    /// PvpPlayerData, cấp test buff collection (Core/Support/Trigger), tạo formation mặc định từ
    /// lineup player đang sở hữu, seed các data group khác rỗng (DOCX §7).
    /// <para>
    /// <b>FLAGGED (reconciliation):</b> DOCX §7 liệt kê "Generate 10–20 mock opponents" trong
    /// bootstrap, nhưng mock-opponent <i>generator</i> là C4 (§39). M1 seed list rỗng; M4
    /// <c>MockPvpOpponentGenerator</c> populate lazy khi Find Match nếu list rỗng.
    /// </para>
    /// </summary>
    internal static class PvpBootstrap
    {
        public static void Run(IPvPRepository repo)
        {
            if (repo == null)
                return;

            // Đã bootstrap rồi → không ghi đè (idempotent).
            if (repo.HasKey(PvpEs3Keys.PlayerData))
                return;

            // 1) Player data mặc định (DOCX §7 — "create default PvPPlayerData").
            var player = new PvpPlayerData
            {
                DisplayName = ResolveDisplayName(),
                RankTier = PvpRankTier.Bronze,
                RankPoint = 0,
                ArenaTicket = PvpDefaults.StartingTickets,
                ArenaToken = PvpDefaults.StartingArenaToken,
                SeasonId = PvpDefaults.SeasonId,
                SeasonEndsAtUnix =
                    System.DateTimeOffset.UtcNow
                        .AddDays(PvpDefaults.SeasonDurationDays)
                        .ToUnixTimeSeconds()
            };
            repo.Save(PvpEs3Keys.PlayerData, player);

            // 2) Formation mặc định từ lineup player (Front=slot0, Back=slot1). Dev fallback -1 nếu
            // player sở hữu dưới 2 hero (DOCX §7 — "if fewer than two exist, use a development-only
            // fallback").
            var (front, back) = ResolveDefaultLineup();
            var formation = new PvpFormationSaveData
            {
                FrontHeroId = front,
                BackHeroId = back
            };
            repo.Save(PvpEs3Keys.Formation, formation);

            // 3) Cấp test buff (ownership only; SO effect defined ở M2). DOCX §7 — "Grant a minimal
            // test buff collection covering Core, Support, and Trigger slots."
            var owned = new PvpOwnedBuffData();
            foreach (var buffId in PvpTestBuffIds.All)
            {
                owned.Buffs.Add(new PvpOwnedBuff
                {
                    BuffId = buffId,
                    IsUnlocked = true,
                    Level = 1,
                    Shards = 0
                });
            }
            repo.Save(PvpEs3Keys.OwnedBuffs, owned);

            // 4) Seed rỗng cho các data group còn lại. Pending battle / pending roll = null (chưa có).
            repo.Save(PvpEs3Keys.MockOpponents, new PvpMockOpponentData());
            repo.Save(PvpEs3Keys.BattleHistory, new PvpBattleHistoryData());
            repo.Save(PvpEs3Keys.BuffGachaState, new FormationBuffGachaState());
            repo.Save(PvpEs3Keys.BuffRollHistory, new PvpBuffRollHistoryData());
            repo.Save(PvpEs3Keys.ProcessedTransactions, new PvpProcessedTransactionsData());

            Debug.Log("[PvP] First-run bootstrap complete — local ES3 defaults created.");
        }

        private static string ResolveDisplayName()
        {
            try
            {
                var cache = UserDataCache.Instance;
                return !string.IsNullOrEmpty(cache?.DisplayName) ? cache.DisplayName : "Player";
            }
            catch
            {
                return "Player";
            }
        }

        private static (int front, int back) ResolveDefaultLineup()
        {
            try
            {
                var cache = UserDataCache.Instance;
                if (cache == null)
                    return (-1, -1);

                var resolved = cache.ResolveLineupHeroIds();
                if (resolved == null || resolved.Count < 2)
                    return (-1, -1);

                int front = resolved[0] > 0 ? resolved[0] : -1;
                int back = resolved[1] > 0 ? resolved[1] : -1;

                // Không trùng hero giữa 2 slot (DOCX §17 — "Front and Back heroes must be different").
                if (front == back)
                    back = -1;

                return (front, back);
            }
            catch
            {
                return (-1, -1);
            }
        }
    }
}
