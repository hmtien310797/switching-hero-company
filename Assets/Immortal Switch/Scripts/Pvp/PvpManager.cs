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
    /// Được gọi từ <see cref="GameBootstrap"/> (DOCX §38). Phase-1 toàn bộ local — không đụng server.
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

            // M2 services (catalog + formation + buff inventory + profile). Inject repository.
            var catalog = new LocalFormationBuffCatalogService();
            var formation = new LocalPvpFormationService(_repository, catalog);
            var buffInventory = new LocalPvpBuffInventoryService(_repository, catalog);
            var profile = new LocalPvPProfileService(_repository);
            await profile.LoadAsync(System.Threading.CancellationToken.None);

            // M4 matchmaking (mock local — DOCX §31).
            var matchmaking = new MockPvpMatchmakingService(_repository, formation, profile, buffInventory);

            // M5 battle result + battle controller (seeded simulation — DOCX §38).
            var battleResult = new LocalPvpBattleResultService(_repository, profile);
            _battleController = new HeroVsHeroBattleController(battleResult, catalog);

            // M7 gacha / progression / history (DOCX §22, §38).
            var gacha = new LocalFormationBuffGachaService(_repository, catalog, profile);
            var progression = new LocalFormationBuffProgressionService(_repository, catalog, profile);
            var history = new LocalPvpHistoryService(_repository);

            // Leaderboard (mock 50 record — server chưa làm; thay sau qua IPvPLeaderboardService).
            var leaderboard = new LocalPvPLeaderboardService(profile);

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
                Leaderboard = leaderboard
            };

            if (enableLog)
                Debug.Log("[PvP] PvpManager initialized (local Phase-1).");

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
