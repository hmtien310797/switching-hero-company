using System;
using System.Collections.Generic;
using System.Linq;
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

        [Header("Hero Team")] [SerializeField] private HeroTeamController heroTeamController;
        [SerializeField, Min(0)] private int controlledHeroSlotIndex = 0;

        [Header("Creep Spawn Formation")] [SerializeField]
        private float spawnSpacingX = 1.6f;

        [SerializeField] private float spawnSpacingZ = 1.25f;
        [SerializeField] private float spawnJitter = 0.25f;
        [SerializeField] private int spawnColumns = 5;
        [SerializeField] private float groupRadius = 2.5f;
        [SerializeField] private int spawnColumnsPerGroup = 3;

        [Header("Creep Spawn Multi Groups")] [SerializeField]
        private int minSpawnGroupsPerBatch = 3;

        [SerializeField] private int maxSpawnGroupsPerBatch = 6;
        [SerializeField] private int maxCreepsPerGroup = 5;

        [Header("Creep Addressable Pools")] [SerializeField, Min(1)]
        private int minimumWarmupPerCreepType = 20;

        [ShowInInspector] private readonly List<EnemyActor> creeps = new();

        private BattleResult result = BattleResult.None;
        private int aliveCreepCount;
        private int deadCreepCount;
        private int totalCreepsSpawnedThisStage;
        private bool isBossAlive;
        private int patternId;
        private int[] enemyIds;
        private float[] rates;
        private int heroDeadCount;
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

        private HeroActor inBattleHeroA;
        private HeroActor inBattleHeroB;

        private readonly BattleTargetRegistry targetRegistry = new();
        public IBattleTargetRegistry TargetRegistry => targetRegistry;

        private readonly List<ICombatUnit> farthestCandidates = new(8);
        private readonly List<float> farthestDistances = new(8);

        private StageRuntimeData stageRuntimeData;

        private BattleState State { get; set; } = BattleState.None;
        public RewardSyncService RewardSyncService => rewardSyncService;
        public List<EnemyActor> CreepList => creeps;
        public IReadOnlyList<EnemyActor> ActiveEnemies => creeps;
        private readonly HashSet<string> activeStageCreepPoolKeys = new();

        /// <summary>Stage tiếp theo được unlock để chơi/chọn — bằng serverFrontierStage (current_stage
        /// thật từ server, đã cap đúng ở stage cuối game). KHÔNG suy từ highest_stage_cleared + 1: 2 giá
        /// trị này chỉ lệch nhau đúng ở stage cuối cùng của game (server cap current_stage ở maxStage
        /// trong khi highest_stage_cleared cũng đạt maxStage), lúc đó +1 sẽ vượt quá tổng số stage thật.</summary>
        public int HighestUnlockedStage => Mathf.Max(1, serverFrontierStage);
        
        private UserDataCache userDataCache;
        private readonly BattleHeroSpawnService heroSpawnService = new();
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
            GameEventManager.Subscribe(GameEvents.OnChangeHero, (Action<int, int>)OnChangeHero);
            GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageFailed);
            GameEventManager.Subscribe<bool>(GameEvents.OnBossSpawnAnimationComplete, OnBossSpawnAnimationComplete);
            GameEventManager.Subscribe<int>(GameEvents.OnMoveStageRequested, HandleMoveStageRequested);
        }

        protected override void OnDestroy()
        {
            GameEventManager.Unsubscribe<int>(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Unsubscribe(GameEvents.OnChangeHero, (Action<int, int>)OnChangeHero);
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
            await pvEMapController.InitMapByChapterAsync(GetResolvedChapterIndexByStage(CurrentStage));
            
            bool poolsReady =
                await PrepareCreepPoolsForCurrentStageAsync();
            if (!poolsReady)
            {
                Debug.LogError(
                    $"[PvE] Cannot start stage because creep pools " +
                    $"are not ready. Stage={CurrentStage}"
                );

                return;
            }
            
            GameEventManager.Trigger(GameEvents.OnInitSceneDataComplete);
            await InitPlayerHeroById();
            GameEventManager.Trigger(GameEvents.OnActiveLineupChanged);
            RefreshControlledHeroSkillUI();
            offlineAfkRewardService?.Initialize(CurrentStage);
            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
            GameEventManager.Trigger(GameEvents.OnWaveStart);
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

        private void OnChangeHero(int sourceHeroId, int targetHeroId)
        {
            OnChangeHeroAsync(sourceHeroId, targetHeroId).Forget();
        }

        private async UniTask OnChangeHeroAsync(int sourceHeroId, int targetHeroId)
        {
            if (!CanSwitchHero(sourceHeroId, targetHeroId))
                return;

            int slotIndex = userDataCache.FindBattleHeroSlot(sourceHeroId);
            if (slotIndex < 0)
            {
                Debug.LogError($"[PvE] Cannot find slot for sourceHeroId={sourceHeroId}");
                return;
            }

            HeroActor oldHero = userDataCache.GetInBattleHeroActorAt(slotIndex);
            if (oldHero == null)
            {
                Debug.LogError($"[PvE] Old hero is null. sourceHeroId={sourceHeroId}, slot={slotIndex}");
                return;
            }

            HeroDataSO newHeroData = MasterDataCache.Instance.GetHeroDataById(targetHeroId);
            if (newHeroData == null)
            {
                Debug.LogError($"[PvE] Cannot find target hero data. targetHeroId={targetHeroId}");
                return;
            }

            HeroActor newHero = await heroSpawnService.SpawnAsync(
                newHeroData,
                oldHero.transform.position,
                this,
                heroTeamController,
                userDataCache.AutoSkill,
                OnHeroDead
            );

            if (newHero == null)
                return;

            if (!userDataCache.TryReplaceBattleHero(slotIndex, sourceHeroId, targetHeroId, newHero))
            {
                heroSpawnService.Despawn(newHero, OnHeroDead);
                return;
            }

            TopMainView.Instance.SetHeroSkeletonAnimationGraphic(newHeroData);

            RefreshHeroSlotCache();
            RefreshHeroTeamController();
            RefreshEnemyHeroTargets();
            GameEventManager.Trigger(GameEvents.OnActiveLineupChanged);
            RefreshControlledHeroSkillUI();

            if (oldHero.IsChosen)
                gameCameraController.SetFollowHero(newHero.transform);

            oldHero.HeroSkillController.DespawnAllInstanceOfUltimateSkillAndClassSkill();
            heroSpawnService.Despawn(oldHero, OnHeroDead);

            SyncLineupToServerAsync().Forget();
        }

        /// <summary>
        /// Lưu đội hình hiện tại (userDataCache.InBattleHeroIdList) lên server qua hero/set_lineup,
        /// để login lại đúng đội hình thay vì luôn về lineup cũ/mặc định. Fire-and-forget — UI đã
        /// apply đổi hero ngay tại chỗ, RPC này chỉ để persist, không chặn luồng chơi.
        /// </summary>
        private async UniTask SyncLineupToServerAsync()
        {
            if (NakamaClient.Instance == null || !NakamaClient.Instance.IsLoggedIn)
                return;

            var lineupUids = new string[userDataCache.InBattleHeroIdList.Count];
            for (int i = 0; i < lineupUids.Length; i++)
            {
                int heroId = userDataCache.InBattleHeroIdList[i];
                lineupUids[i] = heroId > 0 ? userDataCache.GetHeroUid(heroId) : null;
            }

            try
            {
                var response = await NakamaClient.Instance.SetLineupAsync(lineupUids);
                if (response != null && response.Updated && userDataCache.HeroList != null)
                    userDataCache.HeroList.Lineup = response.Lineup;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PvE] hero/set_lineup RPC failed: {e.Message}");
            }
        }

        private void OnHeroDead(HeroActor hero)
        {
            heroDeadCount++;
            if (heroDeadCount >= 2)
            {
                GameEventManager.Trigger(GameEvents.OnStageLost);
                heroDeadCount = 0;
            }
        }

        public void OnSelectedHeroCastUltimateSkill()
        {
            gameCameraController.ZoomToHero().Forget();
            TopMainView.Instance.PlayHeroSkeletonAnimation();
        }

        private async UniTask InitPlayerHeroById(bool isSwitch = false)
        {
            List<int> heroIds = new List<int>(userDataCache.InBattleHeroIdList);
            while (heroIds.Count < userDataCache.BattleHeroSlotCount)
                heroIds.Add(-1);

            if (!heroIds.Exists(id => id > 0))
            {
                var owned = userDataCache.HeroList?.Owned;
                if (owned != null && owned.Length > 0)
                {
                    for (int i = 0; i < heroIds.Count && i < owned.Length; i++)
                        heroIds[i] = owned[i].HeroId;
                }
                else
                {
                    heroIds[0] = 2;
                    if (heroIds.Count > 1)
                        heroIds[1] = 4;
                }

                userDataCache.SetBattleLineup(heroIds);
            }

            int spawnCount = Mathf.Min(heroIds.Count, userDataCache.BattleHeroSlotCount);
            for (int heroIndex = 0; heroIndex < spawnCount; heroIndex++)
            {
                int heroId = heroIds[heroIndex];
                if (heroId <= 0)
                    continue;

                HeroDataSO heroData = MasterDataCache.Instance.GetHeroDataById(heroId);
                HeroActor spawnedHero = await SpawnHero(heroData, heroIndex);
                if (spawnedHero == null)
                    continue;

                if (heroIndex == 0)
                {
                    gameCameraController.SetFollowHero(spawnedHero.transform);
                    TopMainView.Instance.SetHeroSkeletonAnimationGraphic(heroData);
                }
            }

            await UniTask.Delay(1000);
        }

        private async UniTask<HeroActor> SpawnHero(HeroDataSO heroData, int heroIndex)
        {
            if (heroIndex < 0 || heroIndex >= userDataCache.BattleHeroSlotCount)
            {
                Debug.LogError($"[PvE] Invalid hero slot. heroIndex={heroIndex}");
                return null;
            }

            HeroActor hero = await heroSpawnService.SpawnAsync(
                heroData,
                GetHeroSpawnPosition(heroIndex),
                this,
                heroTeamController,
                userDataCache.AutoSkill,
                OnHeroDead
            );

            if (hero == null)
                return null;

            if (!userDataCache.TrySetInBattleHeroActor(heroIndex, hero))
            {
                heroSpawnService.Despawn(hero, OnHeroDead);
                return null;
            }

            RefreshHeroSlotCache();
            RefreshHeroTeamController();
            RefreshEnemyHeroTargets();
            return hero;
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
            Vector3 center = Vector3.forward * 12f;
            if (heroIndex == 0)
            {
                return center + Vector3.left * 1.5f;
            }

            return center + Vector3.right * 1.5f;
        }

        private void RefreshHeroSlotCache()
        {
            inBattleHeroA = userDataCache.GetInBattleHeroActorAt(0);
            inBattleHeroB = userDataCache.GetInBattleHeroActorAt(1);
        }

        private void RefreshHeroTeamController()
        {
            if (heroTeamController == null)
                return;

            heroTeamController.SetHeroes(
                inBattleHeroA,
                inBattleHeroB
            );

            ApplyControlledHeroSelectionToTeamController();
        }

        public bool CanSwitchHero(int sourceHeroId, int targetHeroId)
        {
            if (sourceHeroId <= 0 || targetHeroId <= 0)
                return false;

            if (sourceHeroId == targetHeroId)
                return false;

            if (!userDataCache.ContainsBattleHero(sourceHeroId))
                return false;

            return !userDataCache.ContainsBattleHero(targetHeroId);
        }

        public void RequestSwitchHero(int sourceHeroId, int targetHeroId)
        {
            if (!CanSwitchHero(sourceHeroId, targetHeroId))
                return;

            OnChangeHero(sourceHeroId, targetHeroId);
        }

        public HeroActor GetControlledHero()
        {
            if (controlledHeroSlotIndex < 0 || controlledHeroSlotIndex >= userDataCache.inBattleHeroes.Length)
                controlledHeroSlotIndex = 0;

            return userDataCache.inBattleHeroes[controlledHeroSlotIndex];
        }

        public HeroActor GetFollowerHero()
        {
            int followerSlotIndex = controlledHeroSlotIndex == 0 ? 1 : 0;
            return userDataCache.inBattleHeroes[followerSlotIndex];
        }

        public int GetControlledHeroSlotIndex()
        {
            return controlledHeroSlotIndex;
        }

        public HeroDataSO SelectControlledHeroSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= userDataCache.inBattleHeroes.Length)
            {
                Debug.LogWarning($"[PvE] Invalid controlled hero slot index: {slotIndex}");
                return null;
            }

            if (userDataCache.inBattleHeroes[slotIndex] == null)
            {
                Debug.LogWarning($"[PvE] Cannot select empty hero slot: {slotIndex}");
                return null;
            }

            controlledHeroSlotIndex = slotIndex;
            ApplyControlledHeroSelectionToTeamController();
            RefreshControlledHeroSkillUI();
            gameCameraController.SetFollowHero(userDataCache.inBattleHeroes[slotIndex].transform);
            return userDataCache.inBattleHeroes[slotIndex].HeroData;
        }

        public HeroDataSO SwitchControlledHero()
        {
            int nextSlotIndex = controlledHeroSlotIndex == 0 ? 1 : 0;
            return SelectControlledHeroSlot(nextSlotIndex);
        }

        public BossActor GetActiveBossActor()
        {
            return currentBoss;
        }

        public HeroDataSO OnSwitchMainSubHeroButtonClicked()
        {
            return SwitchControlledHero();
        }

        private void ApplyControlledHeroSelectionToTeamController()
        {
            if (heroTeamController == null)
                return;

            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                var hero = userDataCache.inBattleHeroes[i];
                if (hero == null)
                {
                    continue;
                }

                if (i == controlledHeroSlotIndex)
                {
                    hero.SetChosen(true);
                    continue;
                }

                hero.SetChosen(false);
            }

            if (controlledHeroSlotIndex == 0)
                heroTeamController.SelectHeroA();
            else
                heroTeamController.SelectHeroB();
        }

        private void RefreshControlledHeroSkillUI()
        {
            HeroActor controlledHero = GetControlledHero();
            TopMainView.Instance?.HeroSkillBarUI?.BindHero(controlledHero);
        }

        private async UniTask HandleNextStageAsync()
        {
            heroDeadCount = 0;
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            // CurrentStage đã được set đúng từ server (ApplyServerProgression trong OnStageCleared,
            // qua response battle/end.updated_progression) — không tự "++" cục bộ nữa.
            NotifyIdleScreenStageChanged();
            InitStage(CurrentStage);

            await pvEMapController.InitMapByChapterAsync(
                GetResolvedChapterIndexByStage(CurrentStage)
            );

            isReadyBattle = false;

            bool poolsReady =
                await PrepareCreepPoolsForCurrentStageAsync();

            if (!poolsReady)
            {
                Debug.LogError(
                    $"[PvE] Cannot start next stage because creep pools " +
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

        private async UniTask HandleCurrentStageAsync()
        {
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            InitStage(CurrentStage);
            isReadyBattle = false;

            bool poolsReady =
                await PrepareCreepPoolsForCurrentStageAsync();

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
            heroDeadCount = 0;
            SetState(BattleState.Ended);
            result = BattleResult.None;
            DespawnCreepAndBoss();
            PlayCurrentStage().Forget();
            GameEventManager.Trigger(GameEvents.OnStageChange);
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
                Result          = result,
                Stage           = stage,
                CreepKills      = deadCreepCount,
                TotalCreeps     = totalCreepsSpawnedThisStage,
                HeroDeadCount   = heroDeadCount,
                DurationSeconds = Time.time - stageStartTime,
                HeroIds         = userDataCache.InBattleHeroIdList.Select(id => id.ToString()).ToArray()
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

        private async UniTask<bool> PrepareCreepPoolsForCurrentStageAsync()
        {
            AddressablePoolService poolService =
                AddressablePoolService.Instance;

            if (poolService == null)
            {
                Debug.LogError(
                    "[PvE] AddressablePoolService.Instance is null."
                );

                return false;
            }

            if (enemyIds == null ||
                rates == null ||
                enemyIds.Length == 0)
            {
                Debug.LogError(
                    $"[PvE] Cannot prepare creep pools. " +
                    $"Stage={CurrentStage}, enemyIds or rates are empty."
                );

                return false;
            }

            var requiredPools =
                new Dictionary<string, int>();

            for (int i = 0; i < enemyIds.Length; i++)
            {
                int enemyId = enemyIds[i];

                if (!MasterDataCache.Instance.TryGetCreepData(
                        enemyId,
                        out var creepData))
                {
                    Debug.LogError(
                        $"[PvE] Missing CreepDataSO while preparing pool. " +
                        $"EnemyId={enemyId}, Stage={CurrentStage}"
                    );

                    continue;
                }

                string poolKey = creepData.CreepAddressKey;

                if (string.IsNullOrWhiteSpace(poolKey))
                {
                    Debug.LogError(
                        $"[PvE] CreepAddressKey is empty. " +
                        $"EnemyId={enemyId}, Stage={CurrentStage}"
                    );

                    continue;
                }

                float rate =
                    i < rates.Length
                        ? Mathf.Max(0f, rates[i])
                        : 0f;

                int estimatedCount = Mathf.CeilToInt(
                    gameData.creepBatchSize * rate
                );

                int warmupCount = Mathf.Max(
                    minimumWarmupPerCreepType,
                    estimatedCount
                );

                /*
                 * Trường hợp nhiều enemyId cùng dùng một prefab key,
                 * chỉ tạo một pool và lấy warmup count lớn nhất.
                 */
                if (requiredPools.TryGetValue(
                        poolKey,
                        out int existingWarmupCount))
                {
                    requiredPools[poolKey] = Mathf.Max(
                        existingWarmupCount,
                        warmupCount
                    );
                }
                else
                {
                    requiredPools.Add(
                        poolKey,
                        warmupCount
                    );
                }
            }

            if (requiredPools.Count == 0)
            {
                Debug.LogError(
                    $"[PvE] No valid creep pools resolved. " +
                    $"Stage={CurrentStage}"
                );

                return false;
            }

            /*
             * Tìm các pool thuộc stage cũ nhưng stage mới không còn dùng.
             */
            var obsoletePoolKeys = new List<string>();

            foreach (string oldKey in activeStageCreepPoolKeys)
            {
                if (!requiredPools.ContainsKey(oldKey))
                {
                    obsoletePoolKeys.Add(oldKey);
                }
            }

            /*
             * Dispose pool cũ.
             *
             * Chỉ xóa key khỏi activeStageCreepPoolKeys nếu dispose thành công.
             * Nếu vẫn còn creep active, service sẽ giữ pool và log cảnh báo.
             */
            for (int i = 0; i < obsoletePoolKeys.Count; i++)
            {
                string obsoleteKey = obsoletePoolKeys[i];

                bool disposed =
                    poolService.DisposePool(obsoleteKey);

                if (disposed)
                {
                    activeStageCreepPoolKeys.Remove(
                        obsoleteKey
                    );
                }
            }

            /*
             * Giữ lại pool đã có.
             * Chỉ tạo và warmup pool mới.
             */
            foreach (KeyValuePair<string, int> pair in requiredPools)
            {
                string poolKey = pair.Key;
                int warmupCount = pair.Value;

                if (poolService.HasPool(poolKey))
                {
                    activeStageCreepPoolKeys.Add(poolKey);
                    continue;
                }

                bool created =
                    await poolService.CreatePoolAsync(
                        poolKey,
                        Mathf.CeilToInt(warmupCount * 1.5f)
                    );

                if (!created)
                {
                    Debug.LogError(
                        $"[PvE] Failed to prepare creep pool. " +
                        $"Stage={CurrentStage}, Key={poolKey}"
                    );

                    return false;
                }

                activeStageCreepPoolKeys.Add(poolKey);
            }

            Debug.Log(
                $"[PvE] Creep pools prepared. " +
                $"Stage={CurrentStage}, PoolCount={requiredPools.Count}"
            );

            return true;
        }


        private bool TryGetBossDataByStage(int stage, out BossDataSO bossSo)
        {
            bossSo = null;

            return MasterDataCache.Instance.TryGetBossData(stageRuntimeData.BossId, out bossSo) && bossSo != null;
        }

        private void SpawnNextCreepBatch()
        {
            if (enemyIds == null || rates == null) return;

            int remaining = gameData.maxCreepsPerStage - totalCreepsSpawnedThisStage;
            //if (remaining <= 0 && !losingStage || !playCompletedStage) return;

            int spawnNow = Mathf.Min(gameData.creepBatchSize,
                losingStage || playCompletedStage ? gameData.creepBatchSize : remaining);

            int[] counts = AllocateCounts(spawnNow, rates);
            if (counts == null) return;
            List<Vector3> spawnPositions = GenerateSpawnFormationPositions(spawnNow);
            int spawnIndex = 0;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                int enemyId = enemyIds[i];
                int amount = counts[i];

                if (!MasterDataCache.Instance.TryGetCreepData(enemyId, out var creepData))
                {
                    Debug.LogError($"[PvE] Missing CreepDataSo/Prefab for enemyId={enemyId}");
                    continue;
                }

                for (int k = 0; k < amount; k++)
                {
                    Vector3 nPos = spawnIndex < spawnPositions.Count
                        ? spawnPositions[spawnIndex]
                        : spawnPoss[Random.Range(0, spawnPoss.Count)].position;

                    spawnIndex++;

                    EnemyActor creep;

                    if (cachedScaledCreepStats.TryGetValue(enemyId, out BaseStat cachedStat))
                    {
                        creep = enemySpawnService.SpawnCreep(
                            creepData,
                            nPos,
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
                            nPos,
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
                            $"Stage={CurrentStage}, EnemyId={creepData.Id}"
                        );
                        continue;
                    }

                    creep.SetScale(k % 5 == 0
                        ? 1.5f
                        : 1f);

                    creep.HealthBarController.SetOffsetPosition(k % 5 == 0
                        ? 0.5f
                        : 0f, 0f);

                    creeps.Add(creep);
                    aliveCreepCount++;
                    totalCreepsSpawnedThisStage++;
                }
            }
        }

        private List<Vector3> GenerateSpawnFormationPositions(int amount)
        {
            List<Vector3> result = new List<Vector3>(amount);

            if (amount <= 0)
                return result;

            if (spawnPoss == null || spawnPoss.Count == 0)
                return result;

            int maxPossibleGroups = Mathf.Min(spawnPoss.Count, amount);

            int minGroup = Mathf.Clamp(minSpawnGroupsPerBatch, 1, maxPossibleGroups);
            int maxGroup = Mathf.Clamp(maxSpawnGroupsPerBatch, minGroup, maxPossibleGroups);

            int groupCount = Random.Range(minGroup, maxGroup + 1);

            List<Transform> selectedAnchors = PickRandomSpawnAnchors(groupCount);
            List<int> groupCounts = AllocateEvenGroupCounts(amount, selectedAnchors.Count);

            for (int groupIndex = 0; groupIndex < selectedAnchors.Count; groupIndex++)
            {
                int countInGroup = groupCounts[groupIndex];

                AddGroupFormationPositions(
                    result,
                    selectedAnchors[groupIndex].position,
                    countInGroup
                );
            }

            Shuffle(result);
            return result;
        }

        private List<int> AllocateEvenGroupCounts(int totalAmount, int groupCount)
        {
            List<int> counts = new List<int>(groupCount);

            if (totalAmount <= 0 || groupCount <= 0)
                return counts;

            int baseCount = totalAmount / groupCount;
            int remainder = totalAmount % groupCount;

            for (int i = 0; i < groupCount; i++)
            {
                int count = baseCount;

                if (i < remainder)
                    count++;

                counts.Add(count);
            }

            Shuffle(counts);

            return counts;
        }

        private List<Transform> PickRandomSpawnAnchors(int count)
        {
            List<Transform> candidates = new List<Transform>(spawnPoss);
            List<Transform> result = new List<Transform>(count);

            count = Mathf.Min(count, candidates.Count);

            for (int i = 0; i < count; i++)
            {
                int randomIndex = Random.Range(0, candidates.Count);
                result.Add(candidates[randomIndex]);
                candidates.RemoveAt(randomIndex);
            }

            return result;
        }

        private void AddGroupFormationPositions(
            List<Vector3> result,
            Vector3 anchorPosition,
            int amount
        )
        {
            if (amount <= 0)
                return;

            int columns = Mathf.Max(1, spawnColumnsPerGroup);
            int rows = Mathf.CeilToInt(amount / (float)columns);

            float totalWidth = (columns - 1) * spawnSpacingX;
            float totalDepth = (rows - 1) * spawnSpacingZ;

            for (int i = 0; i < amount; i++)
            {
                int row = i / columns;
                int col = i % columns;

                float x = col * spawnSpacingX - totalWidth * 0.5f;
                float z = row * spawnSpacingZ - totalDepth * 0.5f;

                // hàng lẻ lệch nửa ô để cụm nhìn tự nhiên hơn
                if (row % 2 == 1)
                    x += spawnSpacingX * 0.5f;

                Vector3 jitter = new Vector3(
                    Random.Range(-spawnJitter, spawnJitter),
                    0f,
                    Random.Range(-spawnJitter, spawnJitter)
                );

                Vector3 finalPosition = anchorPosition + new Vector3(x, 0f, z) + jitter;
                result.Add(finalPosition);
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
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

                if (!MasterDataCache.Instance.TryGetCreepData(enemyId, out CreepDataSo data) || data == null)
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

            if (!MasterDataCache.Instance.TryGetBossData(bossId, out BossDataSO data) || data == null)
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

            GameStatView.Instance.InitTimer(battleTime, () =>
            {
                if (State == BattleState.Ended)
                {
                    return;
                }

                GameEventManager.Trigger(GameEvents.OnStageLost);
            });

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

        private async void OnStageCleared(int stage)
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
            if (stage != serverFrontierStage)
            {
                CurrentStage = stage + 1;
                CurrentStageService.SetCurrentStage(CurrentStage);
                NextStageCallback().Forget();
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
            NextStageCallback().Forget();
            GameEventManager.Trigger(GameEvents.OnStageChange);
        }

        private async void OnStageFailed()
        {
            heroDeadCount = 0;
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

            PlayCurrentStage(1).Forget();
            GameEventManager.Trigger(GameEvents.OnStageChange);
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

            SpawnBoss();
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

        private void DropCoinAsync(Vector3 pos)
        {
            var numRand = Random.Range(2, 5);
            for (int i = 0; i < numRand; i++)
            {
                BattleCoinView coin =
                    PoolManager.Instance.Spawn(coinPrefab, pos + Vector3.up * 0.25f, Quaternion.identity);
                var trans = FindHeroNearestFromPos(pos);
                coin.DoDrop(0.1f + i * 0.1f, trans);
            }
        }

        private Transform FindHeroNearestFromPos(Vector3 pos)
        {
            HeroActor firstHero = inBattleHeroA;
            HeroActor secondHero = inBattleHeroB;

            if (firstHero == null && secondHero == null)
                return null;

            if (firstHero == null)
                return secondHero.transform;

            if (secondHero == null)
                return firstHero.transform;

            float distA = (firstHero.transform.position - pos).sqrMagnitude;
            float distB = (secondHero.transform.position - pos).sqrMagnitude;

            return distB < distA
                ? secondHero.transform
                : firstHero.transform;
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

        //not in use
        public Vector3 GetLowestHpEnemy(Vector3 pos, float range)
        {
            // Vector3 tPos = pos;
            // if (!isReadyBattle) return pos;
            //
            // float minHp = float.MaxValue;
            // for (int i = 0; i < creeps.Count; i++)
            // {
            //     var c = creeps[i];
            //     if(c.IsDead || c.gameObject.activeInHierarchy == false) continue;
            //
            //     float d = (c.transform.position- pos).magnitude;
            //     if (d < range && c.CurrentHp < minHp)
            //     {
            //         minHp = c.CurrentHp;
            //         tPos = c.transform.position;
            //     }
            // }
            //
            // if (minHp == float.MaxValue && State == BattleState.FightingBoss && currentBoss.gameObject.activeInHierarchy && !currentBoss.IsDead && currentBoss.IsReady)
            // {
            //     tPos = currentBoss.transform.position;
            // }
            //
            // return tPos;
            return Vector3.zero;
        }

        public ICombatUnit GetRandomFromFarthestEnemies(
            Vector3 pos,
            IReadOnlyList<ICombatUnit> excludedTargets,
            int topCount = 5)
        {
            if (!isReadyBattle)
                return null;

            if (currentBoss != null)
            {
                return currentBoss;
            }

            if (topCount <= 0)
                topCount = 1;

            farthestCandidates.Clear();
            farthestDistances.Clear();

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

                if (IsExcluded(creep, excludedTargets))
                    continue;

                Vector3 creepPos = creep.Position;
                creepPos.y = 0f;

                float sqr = (creepPos - selfPos).sqrMagnitude;

                InsertCandidateByDistanceDesc(
                    farthestCandidates,
                    farthestDistances,
                    creep,
                    sqr,
                    topCount
                );
            }

            if (State == BattleState.FightingBoss &&
                currentBoss != null &&
                !currentBoss.IsDead &&
                currentBoss.gameObject.activeInHierarchy &&
                !IsExcluded(currentBoss, excludedTargets))
            {
                Vector3 bossPos = currentBoss.Position;
                bossPos.y = 0f;

                float bossSqr = (bossPos - selfPos).sqrMagnitude;

                InsertCandidateByDistanceDesc(
                    farthestCandidates,
                    farthestDistances,
                    currentBoss,
                    bossSqr,
                    topCount
                );
            }

            if (farthestCandidates.Count == 0)
                return null;

            int randomIndex = Random.Range(0, farthestCandidates.Count);
            return farthestCandidates[randomIndex];
        }

        private void InsertCandidateByDistanceDesc(
            List<ICombatUnit> candidates,
            List<float> distances,
            ICombatUnit unit,
            float sqrDistance,
            int maxCount)
        {
            int insertIndex = candidates.Count;

            for (int i = 0; i < distances.Count; i++)
            {
                if (sqrDistance > distances[i])
                {
                    insertIndex = i;
                    break;
                }
            }

            if (insertIndex >= maxCount)
                return;

            candidates.Insert(insertIndex, unit);
            distances.Insert(insertIndex, sqrDistance);

            if (candidates.Count > maxCount)
            {
                int lastIndex = candidates.Count - 1;
                candidates.RemoveAt(lastIndex);
                distances.RemoveAt(lastIndex);
            }
        }

        private bool IsExcluded(ICombatUnit unit, IReadOnlyList<ICombatUnit> excludedTargets)
        {
            if (unit == null || excludedTargets == null)
                return false;

            for (int i = 0; i < excludedTargets.Count; i++)
            {
                if (excludedTargets[i] == unit)
                    return true;
            }

            return false;
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
            // List<MonsterScrepController> targets = null;
            // if (State == BattleState.FightingBoss)
            // {
            //     if (currentBoss != null &&
            //         currentBoss.gameObject.activeInHierarchy &&
            //         !currentBoss.IsDead &&
            //         (currentBoss.transform.position - pos).sqrMagnitude < range * range && 
            //         currentBoss.IsReady)
            //     {
            //         return new List<MonsterScrepController> { currentBoss };
            //     }
            //
            //     return targets;
            // }
            //
            // if (creeps.Count == 0) return targets;
            //
            // float sqrRange = range * range;
            //
            // for (int i = 0; i < creeps.Count; i++)
            // {
            //     var c = creeps[i];
            //     if (c == null || !c.gameObject.activeInHierarchy) continue;
            //
            //     float sqrDist = (c.transform.position - pos).sqrMagnitude;
            //     if (sqrDist <= sqrRange)
            //     {
            //         targets ??= new List<MonsterScrepController>();
            //         targets.Add(c);
            //     }
            // }
            //
            // return targets;
            return null;
        }

        private int GetPatternIdByStageLoop(int stage, int stageStep)
        {
            var list = creepSpawnPatternCollection.ListSpawnPattern;
            if (list == null || list.Length == 0) return 1;

            int group = (stage - 1) / Mathf.Max(1, stageStep);
            int idx = group % list.Length; // loop back to 0
            return list[idx].Id;
        }

        private float[] GetRandomRates(int id, int length)
        {
            if (spawnRatePattern == null || spawnRatePattern.SpawnRatePatterns == null ||
                spawnRatePattern.SpawnRatePatterns.Length == 0)
                return null;

            var matchedById = spawnRatePattern.SpawnRatePatterns
                .Where(p => p.Id == id && p.SpawnRate != null && p.SpawnRate.Length == length)
                .ToArray();

            if (matchedById.Length > 0)
                return matchedById[Random.Range(0, matchedById.Length)].SpawnRate;

            var matchedByLen = spawnRatePattern.SpawnRatePatterns
                .Where(p => p.SpawnRate != null && p.SpawnRate.Length == length)
                .ToArray();

            if (matchedByLen.Length > 0)
                return matchedByLen[Random.Range(0, matchedByLen.Length)].SpawnRate;

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


        private async UniTask NextStageCallback()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5.7f));
            Debug.Log("[PvE] Next Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                Vector3 spawnPos = GetHeroSpawnPosition(i);
                userDataCache.inBattleHeroes[i].ActiveHealthBar(false);
                userDataCache.inBattleHeroes[i].ActiveVisual(false);
                userDataCache.inBattleHeroes[i].ResetSpawnPosition(spawnPos);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                userDataCache.inBattleHeroes[i].ResetData();
                await UniTask.Delay(800);
            }

            await HandleNextStageAsync();
        }

        private async UniTask PlayCurrentStage(float delay = 0f)
        {
            if (delay > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delay));
            }

            Debug.Log("[PvE] Play Current Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                Vector3 spawnPos = GetHeroSpawnPosition(i);
                userDataCache.inBattleHeroes[i]?.ActiveHealthBar(false);
                userDataCache.inBattleHeroes[i]?.ActiveVisual(false);
                userDataCache.inBattleHeroes[i]?.ResetSpawnPosition(spawnPos);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            for (int i = 0; i < userDataCache.inBattleHeroes.Length; i++)
            {
                userDataCache.inBattleHeroes[i]?.ResetData();
                await UniTask.Delay(800);
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