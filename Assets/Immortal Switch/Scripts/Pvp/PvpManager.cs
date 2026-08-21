using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Pvp.Battle;
using Immortal_Switch.Scripts.Pvp.Repositories;
using Immortal_Switch.Scripts.Pvp.Services;
using UnityEngine;

namespace Immortal_Switch.Scripts.Pvp
{
    /// <summary>
    /// Singleton facade root cho PvP. Sở hữu <see cref="IPvPRepository"/> + <see cref="PvPFacade"/>.
    /// <see cref="InitializeAsync"/> load repository, chạy first-run bootstrap, dựng facade.
    /// Được gọi từ <see cref="GameBootstrap"/> (DOCX §38). Phase 2: rank/tier/vé/token/battle result/
    /// leaderboard server-authoritative (xem handler/pvp.js) — formation/buff-gacha/history vẫn local.
    /// </summary>
    public class PvpManager : Singleton<PvpManager>
    {
        [SerializeField] private bool enableLog = true;

        private IPvPRepository _repository;
        private PvPFacade _facade;
        private HeroVsHeroBattleController _battleController;

        public PvPFacade Facade => _facade;
        public IPvPRepository Repository => _repository;
        public HeroVsHeroBattleController BattleController => _battleController;

        // PvpManager phải tồn tại qua scene transition (main → battle → result) vì nó nắm giữ
        // repository/facade runtime. Base Singleton chỉ DontDestroyOnLoad khi flag được set TRƯỚC
        // khi Awake đọc — override Awake để bật flag cho cả trường hợp auto-create (AddComponent).
        protected override void Awake()
        {
            DontDestroyOnLoadEnabled = true;
            base.Awake();
        }

        public override async UniTask InitializeAsync()
        {
            _repository = new Es3PvPRepository();
            PvpBootstrap.Run(_repository);

            // M2 services (catalog + formation + buff inventory). Formation/buff inventory vẫn local
            // (chưa có RPC server cho formation buff — xem ServerPvPMatchmakingService header).
            var catalog = new LocalFormationBuffCatalogService();
            var formation = new LocalPvpFormationService(_repository, catalog);
            var buffInventory = new LocalPvpBuffInventoryService(_repository, catalog);

            // Profile: rank/tier/vé/arena_token giờ do server sở hữu (pvp/state) — thay
            // LocalPvPProfileService (ES3) bằng ServerPvPProfileService (xem handler/pvp.js).
            var profile = new ServerPvPProfileService();
            await profile.LoadAsync(System.Threading.CancellationToken.None);

            // M4 matchmaking — ticket/BattleId/RandomSeed server-authoritative (pvp/matchmaking),
            // đối thủ hiển thị/chiến đấu vẫn client tự sinh (xem ServerPvPMatchmakingService header).
            var matchmaking = new ServerPvPMatchmakingService(_repository, formation, profile, buffInventory);

            // M5 battle result (pvp/battle/end, server tính rank/token/tier reward) + battle
            // controller (seeded simulation — không đổi, vẫn chạy client-side).
            var battleResult = new ServerPvpBattleResultService(_repository, profile);
            _battleController = new HeroVsHeroBattleController(battleResult, catalog);

            // M7 gacha / progression / history — vẫn local (chưa có RPC server, ngoài phạm vi đợt này).
            var gacha = new LocalFormationBuffGachaService(_repository, catalog, profile);
            var progression = new LocalFormationBuffProgressionService(_repository, catalog, profile);
            var history = new LocalPvpHistoryService(_repository);

            // Leaderboard server-backed (pvp/leaderboard/top + around_me) — xem
            // ServerPvPLeaderboardService header cho field còn thiếu so với contract cũ.
            var leaderboard = new ServerPvPLeaderboardService(profile);

            // PvP Shop (mock local — server chưa làm; thay sau qua IPvPShopService).
            var shop = new LocalPvPShopService(_repository);

            _facade = new PvPFacade(_repository)
            {
                BuffCatalog = catalog,
                Formation = formation,
                BuffInventory = buffInventory,
                Profile = profile,
                Matchmaking = matchmaking,
                BattleResult = battleResult,
                Gacha = gacha,
                Progression = progression,
                History = history,
                Leaderboard = leaderboard,
                Shop = shop
            };

            if (enableLog)
                Debug.Log("[PvP] PvpManager initialized (server-backed rank/vé/battle/leaderboard).");

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Dev-only: xoá toàn bộ PvP ES3 rồi re-bootstrap (DOCX §6 — "Reset PvP Local Data",
        /// §14 Markdown screen 14).
        /// </summary>
        public void ResetLocalData()
        {
            _repository?.ResetAll();
            PvpBootstrap.Run(_repository);
            Debug.Log("[PvP] Local PvP data reset & re-bootstrapped.");
        }
    }
}
