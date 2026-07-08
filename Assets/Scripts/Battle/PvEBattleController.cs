using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Pattern;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Pooling;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using Random = UnityEngine.Random;
using Immortal_Switch.Scripts.StatSystem;
using Sirenix.OdinInspector;

namespace Battle
{
    public enum BattleState
    {
        None,
        Initializing,
        FightingCreeps,
        FightingBoss,
        Ended
    }

    public enum BattleResult
    {
        None,
        Victory,
        Defeat
    }

    public partial class PvEBattleController : Singleton<PvEBattleController>, IHeroBattleContext
    {
        [Header("Refs")] [SerializeField] FollowHeroController[] enemySpawnerCollection;
        [SerializeField] PvEMapController pvEMapController;
        [SerializeField] BattleCoinView coinPrefab;

        [Header("Spawn Positions ")] [SerializeField]
        private List<Transform> spawnPoss;

        [SerializeField] private CreepSpawnPatternCollectionSO creepSpawnPatternCollection;
        [SerializeField] private SpawnRatePatternSO spawnRatePattern;

        [Header("Stage")]
        [field: SerializeField]
        public int CurrentStage { get; private set; } = 1;

        [SerializeField] private int stagesPerPattern = 10;
        [SerializeField] private int battleTime = 20;

        [Header("Stage Resolver")] [SerializeField]
        private StageDataResolverSO stageDataResolver;

        [SerializeField] private RewardSyncService rewardSyncService;
        [SerializeField] private OfflineAfkRewardService offlineAfkRewardService;

        [Header("Hero Session")] [SerializeField] private BattleHeroSessionController battleHeroSessionController;

        [Header("Creep Spawn Formation")]
        [SerializeField] private BattleSpawnFormationService spawnFormationService;

        private readonly List<Vector3> cachedSpawnFormationPositions = new();

        [Header("Creep Addressable Pools")] [SerializeField]
        private BattleCreepPoolService creepPoolService;

        [SerializeField, Min(1)] private int minimumWarmupPerCreepType = 20;

        [ShowInInspector] private readonly List<EnemyActor> creeps = new();

        [Header("Battle Flow")] 
        [SerializeField] private BattleFlowController battleFlowController;

        private BattleResult result = BattleResult.None;
        private int aliveCreepCount;
        private int deadCreepCount;
        private int totalCreepsSpawnedThisStage;
        private bool isBossAlive;
        private int patternId;
        private int[] enemyIds;
        private float[] rates;
        private int totalMonstersKilledThisStage;

        /// <summary>Time.time lúc bắt đầu stage hiện tại — dùng tính duration_seconds cho battle/end (set trong InitStage).</summary>
        private float stageStartTime;

        /// <summary>
        /// current_stage thật mà server đang lưu — KHÁC CurrentStage (CurrentStage bị MoveToStage
        /// ghi đè khi player replay 1 stage cũ qua Stage Selection). Chỉ được set bởi
        /// ApplyServerProgression — dùng để phân biệt "đang đánh ở frontier" (gọi battle/end) với
        /// "đang replay/farm stage cũ" (không gọi battle/end, không reward, không resync).
        /// </summary>
        private int serverFrontierStage = 1;

        private GameData gameData;
        private BossActor currentBoss;

        private bool isReadyBattle = false;
        private bool losingStage = false;
        private bool playCompletedStage = false;

        private readonly Dictionary<int, BaseStat> cachedScaledCreepStats = new();
        private readonly Dictionary<int, BaseStat> cachedScaledBossStats = new();
        private GameCameraController gameCameraController;


        private readonly BattleTargetRegistry targetRegistry = new();
        public IBattleTargetRegistry TargetRegistry => targetRegistry;

        private readonly BattleEnemySelectionService enemySelectionService = new();

        private StageRuntimeData stageRuntimeData;

        private BattleState State { get; set; } = BattleState.None;
        public RewardSyncService RewardSyncService => rewardSyncService;
        public List<EnemyActor> CreepList => creeps;
        public IReadOnlyList<EnemyActor> ActiveEnemies => creeps;

        /// <summary>Stage tiếp theo được unlock để chơi/chọn — bằng serverFrontierStage (current_stage
        /// thật từ server, đã cap đúng ở stage cuối game). KHÔNG suy từ highest_stage_cleared + 1: 2 giá
        /// trị này chỉ lệch nhau đúng ở stage cuối cùng của game (server cap current_stage ở maxStage
        /// trong khi highest_stage_cleared cũng đạt maxStage), lúc đó +1 sẽ vượt quá tổng số stage thật.</summary>
        public int HighestUnlockedStage => Mathf.Max(1, serverFrontierStage);

        private UserDataCache userDataCache;
        private readonly BattleEnemySpawnService enemySpawnService = new();
        private IEnemyTargetProvider enemyTargetProvider;

        private void Start()
        {
            userDataCache = UserDataCache.Instance;
            gameCameraController = GameCameraController.Instance;
            enemyTargetProvider = new HeroTargetProvider(
                () => userDataCache.GetInBattleHeroActorAt(0),
                () => userDataCache.GetInBattleHeroActorAt(1)
            );

            GameEventManager.Subscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageFailed);
            GameEventManager.Subscribe<bool>(GameEvents.OnBossSpawnAnimationComplete, OnBossSpawnAnimationComplete);
            GameEventManager.Subscribe<int>(GameEvents.OnMoveStageRequested, HandleMoveStageRequested);
        }

        protected override void OnDestroy()
        {
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe(GameEvents.OnStageLost, OnStageFailed);
            GameEventManager.Unsubscribe<bool>(GameEvents.OnBossSpawnAnimationComplete, OnBossSpawnAnimationComplete);
            GameEventManager.Unsubscribe<int>(GameEvents.OnMoveStageRequested, HandleMoveStageRequested);
            base.OnDestroy();
        }

        private void OnBossSpawnAnimationComplete(bool result)
        {
            if (!result)
            {
                gameCameraController.FollowBoss();
                return;
            }

            gameCameraController.FollowLastHeroTarget();
        }

        public override async UniTask InitializeAsync()
        {
            BuildSpawnPositions();
            gameData = GameData.Instance;
            await StartAsync();
        }
        

        private async UniTask StartAsync()
        {
            SetState(BattleState.Initializing);
            CurrentStage = Mathf.Max(1, CurrentStage);
            InitStage(CurrentStage);
            await pvEMapController.InitChapterMapAsync(GetResolvedChapterIndexByStage(CurrentStage));

            bool poolsReady =
                await PrepareCreepPoolsForCurrentStageAsync(battleFlowController.stageFlowCancellationTokenSource.Token);
            if (!poolsReady)
            {
                Debug.LogError(
                    $"[PvE] Cannot start stage because creep pools " +
                    $"are not ready. Stage={CurrentStage}"
                );

                return;
            }

            GameEventManager.Trigger(GameEvents.OnInitSceneDataComplete);
            battleHeroSessionController?.BeginSession(
                BattleHeroSessionType.Chapter,
                this,
                GetHeroSpawnPositionForSession,
                HandleAllHeroesDeadFromSession
            );

            bool heroesSpawned = battleHeroSessionController != null &&
                                 await battleHeroSessionController.SpawnLineupAsync();
            if (!heroesSpawned)
            {
                Debug.LogError("[PvE] Cannot start because battle heroes could not be spawned.", this);
                return;
            }

            battleHeroSessionController.LineupActorsChanged -= RefreshEnemyHeroTargets;
            battleHeroSessionController.LineupActorsChanged += RefreshEnemyHeroTargets;
            
            GameEventManager.Trigger(GameEvents.OnActiveLineupChanged);
            offlineAfkRewardService?.Initialize(CurrentStage);
            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
            GameEventManager.Trigger(GameEvents.OnWaveStart);
            battleFlowController?.MarkChapterRunning();
        }

        private void HandleMoveStageRequested(int targetStage)
        {
            MoveToStage(targetStage);
        }

        public Vector3 GetEndMapPoint()
        {
            return pvEMapController.GetEndMapPosition();
        }

        public StageRuntimeData GetStageRuntimeData()
        {
            return stageRuntimeData;
        }
        
        public void CleanupBattle(
            bool despawnHeroes = false,
            bool clearResult = true)
        {
            isReadyBattle = false;

            if (clearResult)
            {
                result = BattleResult.None;
            }

            DespawnCreepAndBoss();

            cachedSpawnFormationPositions.Clear();

            aliveCreepCount = 0;
            deadCreepCount = 0;
            totalCreepsSpawnedThisStage = 0;

            isBossAlive = false;
            currentBoss = null;

            targetRegistry.Clear();

            if (despawnHeroes &&
                battleHeroSessionController != null)
            {
                battleHeroSessionController.DespawnAllHeroes();
            }

            SetState(BattleState.Ended);
            creepPoolService.DisposeAllEnemyPools();
        }

        public bool CanSwitchHero(int sourceHeroId, int targetHeroId)
        {
            return battleHeroSessionController != null &&
                   battleHeroSessionController.CanReplaceHero(sourceHeroId, targetHeroId);
        }

        public void RequestSwitchHero(int sourceHeroId, int targetHeroId)
        {
            battleHeroSessionController?.RequestReplaceHero(sourceHeroId, targetHeroId);
        }

        public BossActor GetActiveBossActor()
        {
            return currentBoss;
        }

        public HeroActor GetControlledHero()
        {
            return battleHeroSessionController?.GetControlledHero();
        }

        public HeroActor GetFollowerHero()
        {
            return battleHeroSessionController?.GetFollowerHero();
        }

        public int GetControlledHeroSlotIndex()
        {
            return battleHeroSessionController != null
                ? battleHeroSessionController.ControlledHeroSlotIndex
                : 0;
        }

        public HeroDataSO SelectControlledHeroSlot(int slotIndex)
        {
            return battleHeroSessionController?.SelectControlledHeroSlot(slotIndex);
        }

        public HeroDataSO SwitchControlledHero()
        {
            return battleHeroSessionController?.SwitchControlledHero();
        }

        public HeroDataSO OnSwitchMainSubHeroButtonClicked()
        {
            return SwitchControlledHero();
        }

        public void OnSelectedHeroCastUltimateSkill()
        {
            battleHeroSessionController?.OnSelectedHeroCastUltimateSkill();
        }

        public Vector3 GetHeroSpawnPositionForSession(int heroIndex)
        {
            Vector3 center = Vector3.forward * 12f;
            return heroIndex == 0
                ? center + Vector3.left * 1.5f
                : center + Vector3.right * 1.5f;
        }

        private void HandleAllHeroesDeadFromSession()
        {
            GameEventManager.Trigger(GameEvents.OnStageLost);
        }

        private void DespawnCreepAndBoss()
        {
            ClearAllHeroTargets();

            if (currentBoss != null)
            {
                targetRegistry.UnregisterHostile(
                    currentBoss
                );

                AddressableSpawnService.ReleaseInstance(
                    currentBoss
                );

                currentBoss = null;
            }

            for (int i = creeps.Count - 1; i >= 0; i--)
            {
                EnemyActor creep = creeps[i];

                if (creep == null)
                {
                    creeps.RemoveAt(i);
                    continue;
                }

                targetRegistry.UnregisterHostile(creep);

                creep.DespawnToPool(0f).Forget();

                creeps.RemoveAt(i);
            }

            targetRegistry.Clear();

            aliveCreepCount = 0;
        }

        private Vector3 GetHeroSpawnPosition(int heroIndex)
        {
            return GetHeroSpawnPositionForSession(heroIndex);
        }

        private void RefreshHeroSlotCache()
        {
            // Hero actors are owned by BattleHeroSessionController.
        }

        private void RefreshHeroTeamController()
        {
            // Hero team binding is owned by BattleHeroSessionController.
        }

        private void RefreshControlledHeroSkillUI()
        {
            battleHeroSessionController?.RefreshControlledHeroSkillUI();
        }

        private async UniTask HandleNextStageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            /* Hero death state is owned by BattleHeroSessionController. */
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            // CurrentStage đã được set đúng từ server (ApplyServerProgression trong OnStageCleared,
            // qua response battle/end.updated_progression) — không tự "++" cục bộ nữa.
            NotifyIdleScreenStageChanged();
            InitStage(CurrentStage);
            await pvEMapController.InitChapterMapAsync(
                GetResolvedChapterIndexByStage(CurrentStage)
            ).AttachExternalCancellation(cancellationToken);

            isReadyBattle = false;

            bool poolsReady =
                await PrepareCreepPoolsForCurrentStageAsync(cancellationToken);

            if (!poolsReady)
            {
                Debug.LogError(
                    $"[PvE] Cannot start next stage because creep pools " +
                    $"are not ready. Stage={CurrentStage}"
                );
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
            GameEventManager.Trigger(
                GameEvents.OnWaveStart
            );
        }

        private async UniTask HandleCurrentStageAsync()
        {
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            InitStage(CurrentStage);
            isReadyBattle = false;

            bool poolsReady =
                await PrepareCreepPoolsForCurrentStageAsync(battleFlowController.stageFlowCancellationTokenSource.Token);

            if (!poolsReady)
            {
                Debug.LogError(
                    $"[PvE] Cannot replay stage because creep pools " +
                    $"are not ready. Stage={CurrentStage}"
                );

                return;
            }

            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
            GameEventManager.Trigger(
                GameEvents.OnWaveStart
            );
        }

        private void MoveToStage(int targetStage)
        {
            targetStage = Mathf.Max(1, targetStage);
            CurrentStage = targetStage;
            SetState(BattleState.Ended);
            result = BattleResult.None;
            DespawnCreepAndBoss();
            PlayCurrentStage(battleFlowController.stageFlowCancellationTokenSource.Token).Forget();
            GameEventManager.Trigger(GameEvents.OnStageSessionChange);
        }

        private int GetResolvedChapterIndexByStage(int stage)
        {
            return stageDataResolver.GetChapterIndexByStage(stage);
        }

        /// <summary>
        /// Ghi đè progression từ server (player/me.progression, battle/end.updated_progression, hoặc
        /// resync battle/progression) — nguồn sự thật duy nhất cho CurrentStage, không tự "++" cục bộ
        /// nữa (xem Docs battle/end — HandleNextStageAsync không còn tăng CurrentStage thủ công).
        /// Cũng ghi đè serverFrontierStage — dùng để nhận biết replay stage cũ qua Stage Selection.
        /// </summary>
        public void ApplyServerProgression(BattleProgression progression)
        {
            if (progression == null) return;
            CurrentStage = Mathf.Max(1, progression.CurrentStage);
            serverFrontierStage = CurrentStage;
            CurrentStageService.SetCurrentStage(CurrentStage);

            // stage_creeps_cleared từ server ("đã giết hết creep của stage này, chỉ chưa xong
            // boss") mang đúng ý nghĩa losingStage đang có cục bộ (bỏ qua creep wave, mở nút
            // boss) — OR vào đây để resume sau khi đóng/mở lại app cũng vào thẳng boss như retry
            // sau khi thua. Không tự set false: Victory đã tự reset losingStage=false ở
            // OnStageClearedAsync trước khi gọi battle/end (server cũng trả false cùng lúc).
            losingStage = losingStage || progression.StageCreepsCleared;
        }

        /// <summary>Resync current_stage thật từ server sau lỗi STAGE_MISMATCH/INVALID_STAGE của battle/end.</summary>
        private async UniTask ResyncProgressionFromServerAsync()
        {
            try
            {
                BattleProgression progression = await NakamaClient.Instance.GetBattleProgressionAsync();
                ApplyServerProgression(progression);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvE] battle/progression resync failed: {e.Message}");
            }
        }

        private BattleEndRequest BuildBattleEndRequest(string result, int stage)
        {
            return new BattleEndRequest
            {
                Result = result,
                Stage = stage,
                CreepKills = deadCreepCount,
                TotalCreeps = totalCreepsSpawnedThisStage,
                HeroDeadCount = battleHeroSessionController != null ? battleHeroSessionController.DeadHeroCount : 0,
                DurationSeconds = Time.time - stageStartTime,
                HeroIds = userDataCache.InBattleHeroIdList.Select(id => id.ToString()).ToArray()
            };
        }

        // ======================
        // Stage lifecycle
        // ======================

        private void InitStage(int stage)
        {
            stageStartTime = Time.time;
            playCompletedStage = stage < HighestUnlockedStage;
            aliveCreepCount = 0;
            totalCreepsSpawnedThisStage = 0;
            deadCreepCount = losingStage || playCompletedStage ? gameData.maxCreepsPerStage : 0;
            CacheStageSpawnData(stage);

            GameEventManager.Trigger(GameEvents.OnEnemyDead, deadCreepCount);
            GameEventManager.Trigger(GameEvents.OnInitNewStage, playCompletedStage, losingStage, stageRuntimeData);

            isBossAlive = false;
            if (currentBoss != null && currentBoss.gameObject.activeInHierarchy)
            {
                AddressableSpawnService.ReleaseInstance(currentBoss);
                currentBoss = null;
            }

            CurrentStageService.SetCurrentStage(stage);
        }

        private void CacheStageSpawnData(int stage)
        {
            stageRuntimeData = stageDataResolver.Resolve(stage);

            if (stageRuntimeData == null || !stageRuntimeData.IsValid)
            {
                Debug.LogError($"[PvE] StageRuntimeData invalid. stage={stage}");
                enemyIds = null;
                rates = null;
                return;
            }

            RebuildStageStatCache();
            rewardSyncService?.SetCurrentStageData(stageRuntimeData);
            offlineAfkRewardService?.SetCurrentAfkStage(stageRuntimeData.GlobalStage);
            patternId = 0;
            enemyIds = stageRuntimeData.EnemyIds;
            rates = stageRuntimeData.EnemyRates;

            Debug.Log(
                $"[PvE] Resolve Stage={stageRuntimeData.GlobalStage}, " +
                $"Chapter={stageRuntimeData.ChapterName}, " +
                $"LocalStage={stageRuntimeData.LocalStage}, " +
                $"Element={stageRuntimeData.ChapterElement}, " +
                $"EnemyPattern={stageRuntimeData.EnemyPatternId}, " +
                $"BossId={stageRuntimeData.BossId}"
            );
        }

        private async UniTask<bool>
            PrepareCreepPoolsForCurrentStageAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (creepPoolService == null)
            {
                Debug.LogError(
                    "[PvE] BattleCreepPoolService is missing.",
                    this
                );

                return false;
            }

            if (enemyIds == null ||
                enemyIds.Length == 0)
            {
                Debug.LogError(
                    $"[PvE] Cannot prepare creep pools. " +
                    $"Stage={CurrentStage}, enemyIds are empty.",
                    this
                );

                return false;
            }

            if (rates == null ||
                rates.Length == 0)
            {
                Debug.LogError(
                    $"[PvE] Cannot prepare creep pools. " +
                    $"Stage={CurrentStage}, rates are empty.",
                    this
                );

                return false;
            }

            float totalRate = 0f;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                float rate =
                    i < rates.Length
                        ? Mathf.Max(0f, rates[i])
                        : 0f;

                totalRate += rate;
            }

            if (totalRate <= 0f)
            {
                Debug.LogError(
                    $"[PvE] Total enemy rate is zero. " +
                    $"Stage={CurrentStage}",
                    this
                );

                return false;
            }

            List<CreepPoolWarmupRequest> requests =
                new List<CreepPoolWarmupRequest>(
                    enemyIds.Length
                );

            int batchSize = Mathf.Max(
                1,
                gameData.creepBatchSize
            );

            for (int i = 0; i < enemyIds.Length; i++)
            {
                int enemyId = enemyIds[i];

                if (enemyId <= 0)
                {
                    continue;
                }

                float rate =
                    i < rates.Length
                        ? Mathf.Max(0f, rates[i])
                        : 0f;

                if (rate <= 0f)
                {
                    continue;
                }

                float normalizedRate =
                    rate / totalRate;

                int estimatedSimultaneousCount =
                    Mathf.CeilToInt(
                        batchSize * normalizedRate
                    );

                int warmupCount =
                    Mathf.Max(
                        minimumWarmupPerCreepType,
                        estimatedSimultaneousCount
                    );

                requests.Add(
                    new CreepPoolWarmupRequest(
                        enemyId,
                        warmupCount
                    )
                );
            }

            if (requests.Count == 0)
            {
                Debug.LogError(
                    $"[PvE] No valid creep warmup requests. " +
                    $"Stage={CurrentStage}",
                    this
                );

                return false;
            }

            token.ThrowIfCancellationRequested();
            return await creepPoolService.PrepareAsync(
                requests,
                $"PvE Stage {CurrentStage}"
            );
        }


        private bool TryGetBossDataByStage(int stage, out BossDataSO bossSo)
        {
            bossSo = null;

            return DatabaseManager.Instance.TryGetBossData(stageRuntimeData.BossId, out bossSo) && bossSo != null;
        }

        private void SpawnNextCreepBatch()
        {
            if (enemyIds == null || enemyIds.Length == 0 ||
                rates == null || rates.Length == 0)
            {
                Debug.LogError("[PvE] Enemy ids or rates are empty.", this);
                return;
            }

            int remaining = gameData.maxCreepsPerStage - totalCreepsSpawnedThisStage;
            int spawnNow = Mathf.Min(
                gameData.creepBatchSize,
                losingStage || playCompletedStage
                    ? gameData.creepBatchSize
                    : remaining
            );

            if (spawnNow <= 0)
                return;

            int[] counts = AllocateCounts(spawnNow, rates);
            if (counts == null)
                return;

            cachedSpawnFormationPositions.Clear();

            bool formationGenerated =
                spawnFormationService != null &&
                spawnFormationService.GeneratePositions(
                    spawnNow,
                    spawnPoss,
                    cachedSpawnFormationPositions
                );

            if (!formationGenerated)
            {
                Debug.LogError(
                    "[PvE] Cannot generate creep formation positions.",
                    this
                );
                return;
            }

            int spawnIndex = 0;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                int enemyId = enemyIds[i];
                int amount = i < counts.Length ? counts[i] : 0;

                if (amount <= 0)
                    continue;

                if (!DatabaseManager.Instance.TryGetCreepData(enemyId, out var creepData) ||
                    creepData == null)
                {
                    Debug.LogError($"[PvE] Missing CreepDataSo/Prefab for enemyId={enemyId}");
                    continue;
                }

                for (int k = 0; k < amount; k++)
                {
                    if (spawnIndex >= cachedSpawnFormationPositions.Count)
                    {
                        Debug.LogError(
                            $"[PvE] Formation position is missing. " +
                            $"Expected={spawnNow}, Actual={cachedSpawnFormationPositions.Count}",
                            this
                        );
                        return;
                    }

                    Vector3 spawnPosition = cachedSpawnFormationPositions[spawnIndex++];
                    EnemyActor creep;

                    if (cachedScaledCreepStats.TryGetValue(enemyId, out BaseStat cachedStat))
                    {
                        creep = enemySpawnService.SpawnCreep(
                            creepData,
                            spawnPosition,
                            enemyTargetProvider,
                            targetRegistry,
                            cachedStat,
                            NotifyMonsterDeath
                        );
                    }
                    else
                    {
                        StageStatScale enemyScale = stageRuntimeData != null
                            ? stageRuntimeData.EnemyScale
                            : StageStatScale.Identity;

                        creep = enemySpawnService.SpawnCreep(
                            creepData,
                            spawnPosition,
                            enemyTargetProvider,
                            targetRegistry,
                            enemyScale,
                            NotifyMonsterDeath
                        );
                    }

                    if (creep == null)
                    {
                        Debug.LogError(
                            $"[PvE] Failed to spawn creep. " +
                            $"Stage={CurrentStage}, EnemyId={creepData.Id}",
                            this
                        );
                        continue;
                    }

                    bool isLargeCreep = k % 5 == 0;
                    creep.SetScale(isLargeCreep ? 1.5f : 1f);

                    if (creep.HealthBarController != null)
                    {
                        creep.HealthBarController.SetOffsetPosition(
                            isLargeCreep ? 0.5f : 0f,
                            0f
                        );
                    }

                    creeps.Add(creep);
                    aliveCreepCount++;
                    totalCreepsSpawnedThisStage++;
                }
            }
        }

        private void RebuildStageStatCache()
        {
            cachedScaledCreepStats.Clear();
            cachedScaledBossStats.Clear();

            if (stageRuntimeData == null)
                return;

            CacheScaledCreepStats();
            CacheScaledBossStats();
        }

        private void CacheScaledCreepStats()
        {
            if (stageRuntimeData.EnemyIds == null || stageRuntimeData.EnemyIds.Length == 0)
                return;

            StageStatScale scale = stageRuntimeData.EnemyScale;
            scale.Normalize();

            for (int i = 0; i < stageRuntimeData.EnemyIds.Length; i++)
            {
                int enemyId = stageRuntimeData.EnemyIds[i];

                if (cachedScaledCreepStats.ContainsKey(enemyId))
                    continue;

                if (!DatabaseManager.Instance.TryGetCreepData(enemyId, out CreepDataSo data) || data == null)
                {
                    Debug.LogError($"[PvE] Cannot cache creep stat. Missing CreepDataSo. enemyId={enemyId}");
                    continue;
                }

                BaseStat stat = new BaseStat
                {
                    Health = data.BaseHp * scale.HpMultiplier,
                    Attack = data.BaseAtk * scale.AtkMultiplier,
                    Defense = data.BaseDef * scale.DefMultiplier,

                    AttackSpeed = data.BaseAtkSpeed,
                    AttackRange = data.BaseRange,
                    MoveSpeed = data.BaseMoveSpeed,

                    Element = data.Element
                };

                cachedScaledCreepStats.Add(enemyId, stat);
            }
        }

        private void CacheScaledBossStats()
        {
            if (stageRuntimeData.BossId <= 0)
                return;

            StageStatScale scale = stageRuntimeData.BossScale;
            scale.Normalize();

            int bossId = stageRuntimeData.BossId;

            if (!DatabaseManager.Instance.TryGetBossData(bossId, out BossDataSO data) || data == null)
            {
                Debug.LogError($"[PvE] Cannot cache boss stat. Missing BossDataSO. bossId={bossId}");
                return;
            }

            BaseStat stat = new BaseStat
            {
                Health = data.BaseHP * scale.HpMultiplier,
                Attack = data.BaseAtk * scale.AtkMultiplier,
                Defense = data.BaseDef * scale.DefMultiplier,

                AttackSpeed = data.AtkSpeed,
                AttackRange = data.AttackRange,
                MoveSpeed = data.MoveSpeed,

                Element = data.Element
            };

            cachedScaledBossStats[bossId] = stat;
        }

        private async UniTask SpawnBoss()
        {
            if (!TryGetBossDataByStage(CurrentStage, out var bossSo))
            {
                Debug.LogError($"[PvE] Cannot resolve boss data for stage={CurrentStage}");
                return;
            }

            if (string.IsNullOrEmpty(bossSo.BossAddressKey))
            {
                Debug.LogError($"[PvE] Boss prefab missing for bossId={bossSo.Id}");
                return;
            }

            if (currentBoss != null && currentBoss.gameObject.activeInHierarchy)
                return;

            Debug.Log($"[PvE] Spawn Boss - Stage={CurrentStage}, BossId={bossSo.Id}, BossName={bossSo.Name}");

            Vector3 pos = GroupFlashController.Instance.GetPosByIdx(2);

            if (cachedScaledBossStats.TryGetValue(bossSo.Id, out BaseStat cachedBossStat))
            {
                currentBoss = await enemySpawnService.SpawnBossAsync(
                    bossSo,
                    pos,
                    enemyTargetProvider,
                    targetRegistry,
                    cachedBossStat,
                    OnBossDead
                );
            }
            else
            {
                StageStatScale bossScale = stageRuntimeData != null
                    ? stageRuntimeData.BossScale
                    : StageStatScale.Identity;

                currentBoss = await enemySpawnService.SpawnBossAsync(
                    bossSo,
                    pos,
                    enemyTargetProvider,
                    targetRegistry,
                    bossScale,
                    OnBossDead
                );
            }

            if (currentBoss == null)
            {
                Debug.LogError(
                    $"[PvE] Failed to spawn boss. Stage={CurrentStage}, BossId={bossSo.Id}"
                );
                return;
            }

            ///---------------------------------------------------------

            GameStatView.Instance.InitTimer(battleTime, 2f,() =>
            {
                if (State == BattleState.Ended)
                {
                    return;
                }

                GameEventManager.Trigger(GameEvents.OnStageLost);
            }, battleFlowController.stageFlowCancellationTokenSource.Token).Forget();

            isBossAlive = true;
            SetState(BattleState.FightingBoss);
        }

        private void OnBossDead(BossActor boss)
        {
            if (boss != null)
            {
                targetRegistry.UnregisterHostile(boss);
            }

            if (State == BattleState.Ended)
            {
                return;
            }

            FarmingIdleScreenService.AddMonsterKill();
            GameEventManager.Trigger(GameEvents.OnStageCleared, CurrentStage);
        }

        private void OnStageCleared(int stage)
        {
            OnStageClearedAsync(stage, battleFlowController.stageFlowCancellationTokenSource.Token).Forget();
        }

        private async UniTask OnStageClearedAsync(int stage, CancellationToken cancellationToken)
        {
            result = BattleResult.Victory;
            SetState(BattleState.Ended);

            if (currentBoss != null)
            {
                targetRegistry.UnregisterHostile(currentBoss);
            }

            currentBoss = null;
            losingStage = false;

            // Stage khác frontier server == player đang replay/farm 1 stage cũ qua Stage Selection
            // (MoveToStage) — KHÔNG gọi battle/end (server sẽ luôn reject bằng STAGE_MISMATCH vì
            // không có khái niệm replay trong spec hiện tại). Không có Clear reward, không resync,
            // chỉ tăng CurrentStage cục bộ để tiếp tục chơi — giữ đúng hành vi cũ trước khi có RPC này.
            cancellationToken.ThrowIfCancellationRequested();
            if (stage != serverFrontierStage)
            {
                CurrentStage = stage + 1;
                CurrentStageService.SetCurrentStage(CurrentStage);
                NextStageCallback(cancellationToken).Forget();
                return;
            }

            BattleEndRequest request = BuildBattleEndRequest("Victory", stage);
            BattleEndResponse response = null;
            try
            {
                response = await NakamaClient.Instance.BattleEndAsync(request);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvE] battle/end RPC failed: {e.Message}");
            }

            if (response != null && response.Success)
            {
                ApplyServerProgression(response.UpdatedProgression);

                if (response.UpdatedResources != null)
                {
                    CurrencyManager.Instance.ApplyServerBalances(
                        response.UpdatedResources.Gold,
                        response.UpdatedResources.Diamonds,
                        response.UpdatedResources.Energy,
                        response.UpdatedResources.Items
                    );
                }

                if (response.Rewards != null)
                {
                    foreach (var kv in response.Rewards)
                        Debug.Log($"[PvE] battle/end clear reward: {kv.Key} = {kv.Value}");
                }
            }
            else if (response != null && (response.Error == "STAGE_MISMATCH" || response.Error == "INVALID_STAGE"))
            {
                Debug.LogWarning($"[PvE] battle/end rejected ({response.Error}) — resyncing progression.");
                await ResyncProgressionFromServerAsync();
            }

            // KHÔNG còn gọi rewardSyncService?.ClaimClearStageReward(stageRuntimeData) ở đây nữa —
            // reward stage-clear giờ lấy từ response battle/end. SetCurrentStageData (Online Idle)
            // vẫn chạy như cũ qua CacheStageSpawnData trong InitStage, không thuộc phạm vi RPC này.
            NextStageCallback(cancellationToken).Forget();
            cancellationToken.ThrowIfCancellationRequested();
            GameEventManager.Trigger(GameEvents.OnStageSessionChange);
        }

        private void OnStageFailed()
        {
            OnStageFailedAsync(battleFlowController.stageFlowCancellationTokenSource.Token).Forget();
        }

        private async UniTask OnStageFailedAsync(CancellationToken cancellationToken)
        {
            /* Hero death state is owned by BattleHeroSessionController. */
            SetState(BattleState.Ended);
            result = BattleResult.Defeat;
            losingStage = true;
            DespawnCreepAndBoss();

            // Defeat khi đang replay stage cũ (không phải frontier) — không cần báo server, không
            // ảnh hưởng progression dù có gọi hay không, bỏ qua để tránh gọi RPC vô nghĩa.
            if (CurrentStage == serverFrontierStage)
            {
                BattleEndRequest request = BuildBattleEndRequest("Defeat", CurrentStage);
                try
                {
                    await NakamaClient.Instance.BattleEndAsync(request);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PvE] battle/end RPC failed: {e.Message}");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            PlayCurrentStage(cancellationToken,1).Forget();
            GameEventManager.Trigger(GameEvents.OnStageSessionChange);
        }

        private void SetState(BattleState newState)
        {
            if (State == newState) return;
            State = newState;
        }
        
        private void NotifyMonsterDeath(EnemyActor enemy)
        {
            if (enemy == null)
                return;

            targetRegistry.UnregisterHostile(enemy);
            creeps.Remove(enemy);
            aliveCreepCount = Mathf.Max(0, aliveCreepCount - 1);
            deadCreepCount = losingStage || playCompletedStage
                ? GameData.Instance.maxCreepsPerStage
                : deadCreepCount + 1;
            GameEventManager.Trigger(GameEvents.OnEnemyDead, deadCreepCount);
            FarmingIdleScreenService.AddMonsterKill();
            if (State != BattleState.FightingCreeps)
                return;
            if (aliveCreepCount != 0)
                return;
            if (totalCreepsSpawnedThisStage < gameData.maxCreepsPerStage || losingStage || playCompletedStage)
            {
                SpawnNextCreepBatch();
                return;
            }

            // Creep wave của CurrentStage vừa hoàn thành lần đầu (losingStage/playCompletedStage
            // đều false ở đây — xem điều kiện trên) — CurrentStage chắc chắn == serverFrontierStage
            // (playCompletedStage chỉ false khi stage >= frontier). Báo server ngay trước khi boss
            // xuất hiện, để app đóng/crash giữa lúc đánh boss vẫn resume thẳng vào boss sau này.
            ReportCreepsClearedCheckpoint();

            SpawnBoss();
        }

        private async void ReportCreepsClearedCheckpoint()
        {
            try
            {
                var response = await NakamaClient.Instance.BattleCheckpointAsync(
                    new BattleCheckpointRequest { Stage = CurrentStage });

                if (response != null && !response.Success && response.Error == "STAGE_MISMATCH")
                {
                    Debug.LogWarning("[PvE] battle/checkpoint rejected (STAGE_MISMATCH) — resyncing progression.");
                    await ResyncProgressionFromServerAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvE] battle/checkpoint RPC failed: {e.Message}");
            }
        }

        private void RefreshEnemyHeroTargets()
        {
            for (int i = creeps.Count - 1; i >= 0; i--)
            {
                EnemyActor creep = creeps[i];

                if (creep == null || creep.IsDead)
                {
                    creeps.RemoveAt(i);
                    continue;
                }

                creep.SetTargetProvider(enemyTargetProvider);
            }

            if (currentBoss != null && !currentBoss.IsDead)
            {
                currentBoss.SetTargetProvider(enemyTargetProvider);
            }
        }

        public ICombatUnit GetNearestEnemy(Vector3 pos)
        {
            if (!isReadyBattle)
            {
                return null;
            }

            return targetRegistry.GetNearestHostile(pos);
        }

        public ICombatUnit GetFarthestEnemy(Vector3 pos)
        {
            if (!isReadyBattle)
                return null;

            ICombatUnit farthest = null;
            float farthestSqr = -1f;

            Vector3 selfPos = pos;
            selfPos.y = 0f;

            for (int i = creeps.Count - 1; i >= 0; i--)
            {
                EnemyActor creep = creeps[i];

                if (creep == null || creep.IsDead || !creep.gameObject.activeInHierarchy)
                {
                    creeps.RemoveAt(i);
                    continue;
                }

                Vector3 creepPos = creep.Position;
                creepPos.y = 0f;

                float sqr = (creepPos - selfPos).sqrMagnitude;

                if (sqr > farthestSqr)
                {
                    farthestSqr = sqr;
                    farthest = creep;
                }
            }

            if (State == BattleState.FightingBoss &&
                currentBoss != null &&
                !currentBoss.IsDead &&
                currentBoss.gameObject.activeInHierarchy)
            {
                Vector3 bossPos = currentBoss.Position;
                bossPos.y = 0f;

                float bossSqr = (bossPos - selfPos).sqrMagnitude;

                if (bossSqr > farthestSqr)
                {
                    farthest = currentBoss;
                }
            }

            return farthest;
        }

        public ICombatUnit GetRandomEnemyAlive()
        {
            if (currentBoss != null && !currentBoss.IsDead)
            {
                return currentBoss;
            }

            int index = Random.Range(0, creeps.Count);
            return creeps.Count <= 0 ? null : creeps[index];
        }

        private string FormatRewards(StageReward[] rewards)
        {
            if (rewards == null || rewards.Length == 0)
                return "None";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            for (int i = 0; i < rewards.Length; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(rewards[i].currencyType);
                builder.Append(":");
                builder.Append(rewards[i].Amount.ToString());
            }

            return builder.ToString();
        }
        
        public Vector3 GetLowestHpEnemy(Vector3 pos, float range)
        {
            return Vector3.zero;
        }

        public ICombatUnit GetRandomFromFarthestEnemies(
            Vector3 pos,
            IReadOnlyList<ICombatUnit> excludedTargets,
            int topCount = 5)
        {
            if (!isReadyBattle)
                return null;

            BossActor activeBoss =
                State == BattleState.FightingBoss
                    ? currentBoss
                    : null;

            return enemySelectionService.GetRandomFromFarthestEnemies(
                pos,
                creeps,
                activeBoss,
                excludedTargets,
                topCount,
                preferBoss: true
            );
        }

        public void SpawnBossDirectly()
        {
            // Clear target hero đang giữ trước khi dispose creep.
            ClearAllHeroTargets();

            for (int i = creeps.Count - 1; i >= 0; i--)
            {
                EnemyActor creep = creeps[i];

                if (creep == null)
                {
                    creeps.RemoveAt(i);
                    continue;
                }

                targetRegistry.UnregisterHostile(creep);

                creep.DespawnToPool(0f).Forget();

                creeps.RemoveAt(i);
            }

            aliveCreepCount = 0;

            SpawnBoss().Forget();
        }

        private void ClearAllHeroTargets()
        {
            HeroActor heroA =
                userDataCache.GetInBattleHeroActorAt(0);

            HeroActor heroB =
                userDataCache.GetInBattleHeroActorAt(1);

            heroA?.ClearTarget();
            heroB?.ClearTarget();
        }

        //dung để lấy quái gần nhất cho hero đang cần, not in use
        public List<EnemyActor> GetNearestEnemiesInRange(float range)
        {
            return null;
        }
        
        private int[] AllocateCounts(int total, float[] weights)
        {
            int n = weights.Length;

            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                if (weights[i] < 0) weights[i] = 0;
                sum += weights[i];
            }

            if (sum <= 0f) return null;

            int[] counts = new int[n];
            int assigned = 0;

            for (int i = 0; i < n; i++)
            {
                float expected = total * (weights[i] / sum);
                int c = Mathf.FloorToInt(expected);
                counts[i] = c;
                assigned += c;
            }

            int remain = total - assigned;
            for (int k = 0; k < remain; k++)
            {
                int idx = Random.Range(0, n);
                counts[idx]++;
            }

            return counts;
        }

        private void BuildSpawnPositions()
        {
            if (spawnPoss.Count == 0)
                Debug.LogError("[PvE] No spawn positions assigned");
            else
                spawnPoss = spawnPoss.OrderBy(_ => Random.value).ToList(); // shuffle
        }


        private async UniTask NextStageCallback(CancellationToken cancellationToken)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5.7f), cancellationToken: cancellationToken);
            Debug.Log("[PvE] Next Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                Vector3 spawnPos = GetHeroSpawnPosition(i);
                userDataCache.inBattleHeroes[i].ActiveHealthBar(false);
                userDataCache.inBattleHeroes[i].ActiveVisual(false);
                userDataCache.inBattleHeroes[i].ResetSpawnPosition(spawnPos);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5), cancellationToken: cancellationToken);
            Transitioner.Instance.TransitionInWithoutChangingScene();
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                userDataCache.inBattleHeroes[i].ResetData();
                await UniTask.Delay(800, cancellationToken: cancellationToken);
            }

            await HandleNextStageAsync(cancellationToken);
        }

        private async UniTask PlayCurrentStage(CancellationToken cancellationToken, float delay = 0f)
        {
            if (delay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);
            }

            Debug.Log("[PvE] Play Current Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene(cancellationToken);
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                Vector3 spawnPos = GetHeroSpawnPosition(i);
                userDataCache.inBattleHeroes[i]?.ActiveHealthBar(false);
                userDataCache.inBattleHeroes[i]?.ActiveVisual(false);
                userDataCache.inBattleHeroes[i]?.ResetSpawnPosition(spawnPos);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5), cancellationToken: cancellationToken);
            Transitioner.Instance.TransitionInWithoutChangingScene();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5), cancellationToken: cancellationToken);
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                userDataCache.inBattleHeroes[i]?.ResetData();
                await UniTask.Delay(800, cancellationToken: cancellationToken);
            }

            await HandleCurrentStageAsync();
        }

        public bool IsHeroCurrentlyActive(int heroId)
        {
            if (heroId <= 0) return false;
            return userDataCache.InBattleHeroIdList.Contains(heroId);
        }

        private void NotifyIdleScreenStageChanged()
        {
            if (!FarmingIdleScreenService.IsActive)
                return;

            if (stageDataResolver == null)
                return;

            StageRuntimeData stageData = stageDataResolver.Resolve(CurrentStage);
            if (stageData == null)
                return;

            FarmingIdleScreenService.UpdateStageData(stageData);
        }
    }
}