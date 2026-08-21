using Immortal_Switch.Scripts.Pvp.Interfaces;
using Immortal_Switch.Scripts.Pvp.Repositories;

namespace Immortal_Switch.Scripts.Pvp
{
    /// <summary>
    /// Composition root cho PvP service (DOCX §38 — "PvPFacade"). UI / Battle Controller chỉ phụ
    /// thuộc facade + service interface — không thấy ES3 hay mock. Services được PvpManager gắn
    /// vào facade khi InitializeAsync (mỗi milestone thêm service tương ứng).
    /// </summary>
    public sealed class PvPFacade
    {
        public IPvPRepository Repository { get; }

        public PvPFacade(IPvPRepository repository)
        {
            Repository = repository;
        }

        // ── M2: catalog + formation + buff inventory + profile ───────────────────
        public IFormationBuffCatalogService BuffCatalog { get; internal set; }
        public IPvPFormationService Formation { get; internal set; }
        public IPvPBuffInventoryService BuffInventory { get; internal set; }
        public IPvPProfileService Profile { get; internal set; }

        // ── M4: matchmaking ──
        public IPvPMatchmakingService Matchmaking { get; internal set; }
        // ── M5: battle result ──
        public IPvPBattleResultService BattleResult { get; internal set; }
        // ── M7: gacha / progression / history ──
        public IFormationBuffGachaService Gacha { get; internal set; }
        public IFormationBuffProgressionService Progression { get; internal set; }
        public IPvPHistoryService History { get; internal set; }

        // ── Leaderboard (rankings PvP Main) ──
        public IPvPLeaderboardService Leaderboard { get; internal set; }

        // ── Shop (PvP Shop) ──
        public IPvPShopService Shop { get; internal set; }
    }
}
