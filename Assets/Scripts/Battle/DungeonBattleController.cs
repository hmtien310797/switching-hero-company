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
using Immortal_Switch.Scripts.StatSystem;
using UnityEngine;

namespace Battle.Dungeon
{
    public sealed partial class DungeonBattleController : MonoBehaviour, IHeroBattleContext
    {
        [Header("Dungeon Data")]
        [SerializeField] private DungeonDatabaseSO database;
        [SerializeField] private int dungeonId = 1;
        [SerializeField, Min(1)] private int stage = 1;

        [Header("Addressable Dungeon Map")]
        [SerializeField] private PvEMapController dungeonMapController;

        [Header("Hero")]
        [SerializeField] private HeroTeamController heroTeamController;

        [Header("Fallback Spawn Points")]
        [Tooltip("Only used when the spawned DungeonMapView has no corresponding point.")]
        [SerializeField] private Transform[] heroSpawnPoints;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform bossSpawnPoint;
        [SerializeField] private Transform specialTargetSpawnPoint;

        [Header("Special Mode Target")]
        [SerializeField] private DungeonDamageDummy damageDummyPrefab;
        [SerializeField] private DungeonDefenseObjective defenseObjectivePrefab;
        [SerializeField] private BaseStat damageDummyBaseStat;
        [SerializeField] private BaseStat defenseObjectiveBaseStat;

        private readonly BattleHeroSpawnService heroSpawnService = new();
        private readonly BattleEnemySpawnService enemySpawnService = new();
        private readonly BattleTargetRegistry targetRegistry = new();
        private readonly List<EnemyActor> activeEnemies = new();

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
            if (remainingTime <= 0f)
                currentMode?.OnTimeExpired();
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

        public BossActor GetActiveBossActor()
        {
            return activeBoss;
        }

        public void OnSelectedHeroCastUltimateSkill()
        {
        }

        private async UniTask SpawnLineupHeroesAsync()
        {
            for (int slotIndex = 0; slotIndex < userDataCache.BattleHeroSlotCount; slotIndex++)
            {
                int heroId = userDataCache.GetBattleHeroIdAt(slotIndex);
                if (heroId <= 0)
                    continue;

                HeroActor hero = await heroSpawnService.SpawnAsync(
                    heroId,
                    GetHeroSpawnPosition(slotIndex),
                    this,
                    heroTeamController,
                    userDataCache.AutoSkill,
                    OnHeroDead
                );

                if (hero == null)
                    continue;

                if (!userDataCache.TrySetInBattleHeroActor(slotIndex, hero))
                    heroSpawnService.Despawn(hero, OnHeroDead);
            }

            heroTeamController?.SetHeroes(
                userDataCache.GetInBattleHeroActorAt(0),
                userDataCache.GetInBattleHeroActorAt(1)
            );
        }

        private EnemyActor SpawnEnemyForMode(int enemyId)
        {
            if (!MasterDataCache.Instance.TryGetCreepData(enemyId, out CreepDataSo creepData) ||
                creepData == null)
            {
                Debug.LogError($"[Dungeon] Missing creep data. enemyId={enemyId}");
                return null;
            }

            EnemyActor enemy = enemySpawnService.SpawnCreep(
                creepData,
                GetRandomEnemySpawnPosition(),
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
            if (!MasterDataCache.Instance.TryGetBossData(bossId, out BossDataSO bossData) ||
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

        private void OnHeroDead(HeroActor hero)
        {
            HeroActor heroA = userDataCache.GetInBattleHeroActorAt(0);
            HeroActor heroB = userDataCache.GetInBattleHeroActorAt(1);

            bool heroADead = heroA == null || heroA.IsDead;
            bool heroBDead = heroB == null || heroB.IsDead;

            if (heroADead && heroBDead)
                CompleteDefeat();
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

            if (userDataCache != null)
            {
                for (int slotIndex = 0; slotIndex < userDataCache.BattleHeroSlotCount; slotIndex++)
                {
                    HeroActor hero = userDataCache.GetInBattleHeroActorAt(slotIndex);
                    if (hero != null)
                        heroSpawnService.Despawn(hero, OnHeroDead);

                    userDataCache.TrySetInBattleHeroActor(slotIndex, null);
                }
            }

            targetRegistry.Clear();

            activeDungeonMapView = null;
            dungeonMapController?.ReleaseCurrentMap();

            runtimeData = null;
            state = DungeonBattleState.None;
        }

        private Vector3 GetHeroSpawnPosition(int slotIndex)
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

        private Vector3 GetRandomEnemySpawnPosition()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
                return Vector3.zero;

            int startIndex = UnityEngine.Random.Range(0, enemySpawnPoints.Length);
            for (int offset = 0; offset < enemySpawnPoints.Length; offset++)
            {
                int index = (startIndex + offset) % enemySpawnPoints.Length;
                Transform point = enemySpawnPoints[index];
                if (point != null)
                    return point.position;
            }

            return Vector3.zero;
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
