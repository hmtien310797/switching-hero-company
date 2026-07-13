using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Common;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Battle.Dungeon
{
    public sealed partial class DungeonBattleController : MonoBehaviour, IHeroBattleContext
    {
        [SerializeField] private int dungeonId = 1;
        [SerializeField, Min(1)] private int stage = 1;

        [Header("Addressable Dungeon Map")] [SerializeField]
        private PvEMapController dungeonMapController;

        [Header("Hero Session")] [SerializeField] private BattleHeroSessionController battleHeroSessionController;

        [Header("Fallback Spawn Points")]
        [Tooltip("Only used when the spawned DungeonMapView has no corresponding point.")]
        [SerializeField]
        private Transform[] heroSpawnPoints;

        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform bossSpawnPoint;

        [Header("Enemy Spawn Formation")]
        [SerializeField] private BattleSpawnFormationService spawnFormationService;
        [SerializeField] private Transform specialTargetSpawnPoint;

        [Header("Special Mode Target")] [SerializeField]
        private DungeonDamageDummy damageDummyPrefab;

        [SerializeField] private DungeonDefenseObjective defenseObjectivePrefab;
        [SerializeField] private BaseStat damageDummyBaseStat;
        [SerializeField] private BaseStat defenseObjectiveBaseStat;

        [Header("Creep Addressable Pools")] [SerializeField]
        private BattleCreepPoolService creepPoolService;

        [SerializeField, Min(1)] private int minimumDungeonCreepWarmup = 5;

        private readonly BattleEnemySpawnService enemySpawnService = new();
        private readonly BattleTargetRegistry targetRegistry = new();
        private readonly BattleEnemySelectionService enemySelectionService = new();
        private readonly List<EnemyActor> activeEnemies = new();
        private readonly List<Vector3> dungeonBatchSpawnPositions = new();
        private int dungeonBatchSpawnPositionIndex;

        private readonly List<CreepPoolWarmupRequest> dungeonCreepPoolRequests = new(1);
        private UserDataCache userDataCache;
        private IEnemyTargetProvider heroTargetProvider;
        private IEnemyTargetProvider defenseTargetProvider;
        private IEnemyTargetProvider activeEnemyTargetProvider;
        private IDungeonMode currentMode;
        private DungeonStageRuntimeData runtimeData;
        private BossActor activeBoss;
        private DungeonDamageDummy activeDamageDummy;
        private DungeonDefenseObjective activeDefenseObjective;
        private PvEMapView activeDungeonMapView;
        private DungeonBattleState state;
        private DungeonBattleResult result;
        private float remainingTime;
        private long currentScore;
        private DungeonRuntimeReward[] finalRewards = Array.Empty<DungeonRuntimeReward>();

        public IBattleTargetRegistry TargetRegistry => targetRegistry;
        public IReadOnlyList<EnemyActor> ActiveEnemies => activeEnemies;
        public DungeonStageRuntimeData RuntimeData => runtimeData;
        public DungeonBattleState State => state;
        public DungeonBattleResult Result => result;
        public float RemainingTime => remainingTime;
        public long CurrentScore => currentScore;
        public IReadOnlyList<DungeonRuntimeReward> FinalRewards => finalRewards;

        public event Action<DungeonBattleResult>
            BattleEnded;

        private DungeonBattleResult battleResult =
            DungeonBattleResult.None;

        private bool battleEnded;

        private void Awake()
        {
            userDataCache = UserDataCache.Instance;

            heroTargetProvider = new HeroTargetProvider(
                () => userDataCache.GetInBattleHeroActorAt(0),
                () => userDataCache.GetInBattleHeroActorAt(1)
            );

            defenseTargetProvider = new DefenseTargetProvider(
                () => activeDefenseObjective,
                () => userDataCache.GetInBattleHeroActorAt(0),
                () => userDataCache.GetInBattleHeroActorAt(1)
            );
        }

        private void Update()
        {
            if (state != DungeonBattleState.Fighting)
                return;

            float deltaTime = Time.deltaTime;
            currentMode?.Tick(deltaTime);

            remainingTime = Mathf.Max(0f, remainingTime - deltaTime);
        }

        private void OnDestroy()
        {
            CleanupBattle();
        }

        public ICombatUnit GetNearestEnemy(Vector3 position)
        {
            return state == DungeonBattleState.Fighting
                ? targetRegistry.GetNearestHostile(position)
                : null;
        }

        public ICombatUnit GetRandomEnemyAlive()
        {
            if (GetActiveBossActor())
            {
                return activeBoss;
            }

            int index = Random.Range(0, activeEnemies.Count);
            return activeEnemies.Count <= 0 ? null : activeEnemies[index];
        }

        public ICombatUnit GetRandomFromFarthestEnemies(
            Vector3 position,
            IReadOnlyList<ICombatUnit> excludedTargets,
            int topCount = 5)
        {
            if (state != DungeonBattleState.Fighting)
                return null;

            return enemySelectionService.GetRandomFromFarthestEnemies(
                position,
                activeEnemies,
                GetActiveBossActor(),
                excludedTargets,
                topCount,
                preferBoss: true
            );
        }

        public BossActor GetActiveBossActor()
        {
            if (activeBoss != null && !activeBoss.IsDead)
            {
                return activeBoss;
            }
            return null;
        }

        public void OnSelectedHeroCastUltimateSkill()
        {
            battleHeroSessionController?.OnSelectedHeroCastUltimateSkill();
        }

        private void PrepareEnemyBatchFormation(int amount)
        {
            dungeonBatchSpawnPositions.Clear();
            dungeonBatchSpawnPositionIndex = 0;

            if (amount <= 0)
                return;

            if (spawnFormationService == null)
            {
                Debug.LogError(
                    "[Dungeon] BattleSpawnFormationService is missing.",
                    this
                );
                return;
            }

            bool generated = spawnFormationService.GeneratePositions(
                amount,
                enemySpawnPoints,
                dungeonBatchSpawnPositions
            );

            if (!generated)
            {
                Debug.LogError(
                    $"[Dungeon] Cannot generate enemy formation. Amount={amount}",
                    this
                );
            }
        }

        private EnemyActor SpawnEnemyForMode(int enemyId)
        {
            if (!DatabaseManager.Instance.TryGetCreepData(enemyId, out CreepDataSo creepData) ||
                creepData == null)
            {
                Debug.LogError($"[Dungeon] Missing creep data. enemyId={enemyId}");
                return null;
            }

            if (dungeonBatchSpawnPositionIndex >= dungeonBatchSpawnPositions.Count)
            {
                Debug.LogError(
                    $"[Dungeon] Formation position is missing. " +
                    $"Index={dungeonBatchSpawnPositionIndex}, " +
                    $"Count={dungeonBatchSpawnPositions.Count}",
                    this
                );
                return null;
            }

            Vector3 spawnPosition =
                dungeonBatchSpawnPositions[dungeonBatchSpawnPositionIndex++];

            EnemyActor enemy = enemySpawnService.SpawnCreep(
                creepData,
                spawnPosition,
                activeEnemyTargetProvider,
                targetRegistry,
                runtimeData.EnemyScale,
                OnEnemyDead
            );

            if (enemy != null)
                activeEnemies.Add(enemy);

            return enemy;
        }

        private async UniTask<BossActor> SpawnBossForModeAsync(int bossId)
        {
            if (!DatabaseManager.Instance.TryGetBossData(bossId, out BossDataSO bossData) ||
                bossData == null)
            {
                Debug.LogError($"[Dungeon] Missing boss data. bossId={bossId}");
                return null;
            }

            Vector3 position = bossSpawnPoint.position;

            activeBoss = await enemySpawnService.SpawnBossAsync(
                bossData,
                position,
                activeEnemyTargetProvider,
                targetRegistry,
                runtimeData.BossScale,
                OnBossDead
            );

            return activeBoss;
        }

        private DungeonDamageDummy SpawnDamageDummy()
        {
            if (damageDummyPrefab == null)
            {
                Debug.LogError("[Dungeon] Damage Dummy prefab is null.");
                return null;
            }

            activeDamageDummy = Instantiate(
                damageDummyPrefab,
                GetSpecialTargetPosition(),
                Quaternion.identity
            );

            activeDamageDummy.Initialize(BuildScaledBaseStat(
                damageDummyBaseStat,
                runtimeData.EnemyScale
            ));

            targetRegistry.RegisterHostile(activeDamageDummy);
            return activeDamageDummy;
        }

        private DungeonDefenseObjective SpawnDefenseObjective()
        {
            if (defenseObjectivePrefab == null)
            {
                Debug.LogError("[Dungeon] Defense Objective prefab is null.");
                return null;
            }

            activeDefenseObjective = Instantiate(
                defenseObjectivePrefab,
                GetSpecialTargetPosition(),
                Quaternion.identity
            );

            activeDefenseObjective.Initialize(BuildScaledBaseStat(
                defenseObjectiveBaseStat,
                runtimeData.EnemyScale
            ));

            return activeDefenseObjective;
        }

        private async UniTask<bool>
            PrepareCreepPoolsForDungeonAsync()
        {
            if (creepPoolService == null)
            {
                Debug.LogError(
                    "[Dungeon] BattleCreepPoolService is missing.",
                    this
                );

                return false;
            }

            dungeonCreepPoolRequests.Clear();

            if (runtimeData == null)
            {
                Debug.LogError(
                    "[Dungeon] RuntimeData is null while preparing pools.",
                    this
                );

                return false;
            }

            switch (runtimeData.Mode)
            {
                case DungeonModeType.KillAllEnemies:
                case DungeonModeType.DefendObjective:
                {
                    if (runtimeData.EnemyId <= 0)
                    {
                        Debug.LogError(
                            $"[Dungeon] Invalid EnemyId. " +
                            $"DungeonId={runtimeData.DungeonId}, " +
                            $"Stage={runtimeData.Stage}, " +
                            $"EnemyId={runtimeData.EnemyId}",
                            this
                        );

                        return false;
                    }

                    int totalEnemyCount =
                        Mathf.Max(
                            0,
                            runtimeData.TotalEnemyCount
                        );

                    int enemyPerBatch =
                        Mathf.Max(
                            1,
                            runtimeData.EnemyPerBatch
                        );

                    int simultaneousEnemyCount =
                        Mathf.Min(
                            totalEnemyCount,
                            enemyPerBatch
                        );

                    int warmupCount =
                        Mathf.Max(
                            minimumDungeonCreepWarmup,
                            simultaneousEnemyCount
                        );

                    dungeonCreepPoolRequests.Add(
                        new CreepPoolWarmupRequest(
                            runtimeData.EnemyId,
                            warmupCount
                        )
                    );

                    break;
                }

                case DungeonModeType.DamageChallenge:
                case DungeonModeType.BossChallenge:
                    // Không spawn creep thường.
                    // Vẫn gọi PrepareAsync với list rỗng để dispose
                    // các pool creep Chapter còn tồn tại.
                    break;

                default:
                    Debug.LogError(
                        $"[Dungeon] Unsupported mode while preparing pools: " +
                        $"{runtimeData.Mode}",
                        this
                    );

                    return false;
            }

            return await creepPoolService.PrepareAsync(
                dungeonCreepPoolRequests,
                $"Dungeon {runtimeData.DungeonKey} Stage {runtimeData.Stage}"
            );
        }

        private void OnEnemyDead(EnemyActor enemy)
        {
            if (enemy == null)
                return;

            enemy.OnDead -= OnEnemyDead;
            targetRegistry.UnregisterHostile(enemy);
            activeEnemies.Remove(enemy);

            if (currentMode is DungeonKillAllMode killAllMode)
                killAllMode.NotifyEnemyDead(enemy);
            else if (currentMode is DungeonDefenseMode defenseMode)
                defenseMode.NotifyEnemyDead(enemy);
        }

        private void OnBossDead(BossActor boss)
        {
            if (boss == null)
                return;

            boss.OnDead -= OnBossDead;
            targetRegistry.UnregisterHostile(boss);

            if (currentMode is DungeonBossChallengeMode bossMode)
                bossMode.NotifyBossDead(boss);

            activeBoss = null;
        }

        private void CompleteVictory()
        {
            CompleteBattle(DungeonBattleResult.Victory);
        }

        private void CompleteDefeat()
        {
            CompleteBattle(DungeonBattleResult.Defeat);
        }

        private void SetScore(long score)
        {
            currentScore = Math.Max(0L, score);
        }

        public void CleanupBattle()
        {
            currentMode?.Dispose();
            currentMode = null;

            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                EnemyActor enemy = activeEnemies[i];
                if (enemy == null)
                    continue;

                enemy.OnDead -= OnEnemyDead;
                targetRegistry.UnregisterHostile(enemy);
                enemy.DespawnToPool(0f).Forget();
            }

            activeEnemies.Clear();

            if (activeBoss != null)
            {
                activeBoss.OnDead -= OnBossDead;
                targetRegistry.UnregisterHostile(activeBoss);
                AddressableSpawnService.ReleaseInstance(activeBoss);
                activeBoss = null;
            }

            if (activeDamageDummy != null)
            {
                targetRegistry.UnregisterHostile(activeDamageDummy);
                Destroy(activeDamageDummy.gameObject);
                activeDamageDummy = null;
            }

            if (activeDefenseObjective != null)
            {
                Destroy(activeDefenseObjective.gameObject);
                activeDefenseObjective = null;
            }

            battleHeroSessionController?.DespawnAllHeroes();

            targetRegistry.Clear();
            dungeonBatchSpawnPositions.Clear();
            dungeonBatchSpawnPositionIndex = 0;

            activeDungeonMapView = null;

            runtimeData = null;
            state = DungeonBattleState.None;
        }

        public Vector3 GetHeroSpawnPositionForSession(int slotIndex)
        {
            if (heroSpawnPoints != null &&
                slotIndex >= 0 &&
                slotIndex < heroSpawnPoints.Length &&
                heroSpawnPoints[slotIndex] != null)
            {
                return heroSpawnPoints[slotIndex].position;
            }

            return slotIndex == 0
                ? Vector3.left * 1.5f
                : Vector3.right * 1.5f;
        }

        public void HandleAllHeroesDeadFromSession()
        {
            CompleteDefeat();
        }

        private Vector3 GetSpecialTargetPosition()
        {
            return specialTargetSpawnPoint != null
                ? specialTargetSpawnPoint.position
                : Vector3.zero;
        }

        private static BaseStat BuildScaledBaseStat(BaseStat source, StageStatScale scale)
        {
            if (source == null)
                return null;

            return new BaseStat
            {
                Health = source.Health * Mathf.Max(0.0001f, scale.HpMultiplier),
                Attack = source.Attack * Mathf.Max(0.0001f, scale.AtkMultiplier),
                Defense = source.Defense * Mathf.Max(0.0001f, scale.DefMultiplier),
                AttackRange = source.AttackRange,
                AttackSpeed = source.AttackSpeed,
                CritChance = source.CritChance,
                CritDamage = source.CritDamage,
                Accuracy = source.Accuracy,
                MoveSpeed = source.MoveSpeed,
                IdleStateTime = source.IdleStateTime,
                IdleIntervalTime = source.IdleIntervalTime,
                Element = source.Element
            };
        }
    }
}