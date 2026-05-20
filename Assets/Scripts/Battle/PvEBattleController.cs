using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Enemy;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Pattern;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using Random = UnityEngine.Random;
using Immortal_Switch.Scripts.StatSystem;
using Scripts.Common;

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

    public partial class PvEBattleController : Singleton<PvEBattleController>
    {
        [Header("Refs")] [SerializeField] PlayerCamController playerCamController;
        [SerializeField] Transform skillObjTrans;
        [SerializeField] FollowHeroController[] enemySpawnerCollection;
        [SerializeField] PvEMapController pvEMapController;
        [SerializeField] BattleCoinView coinPrefab;

        [Header("Spawn Positions")] [SerializeField]
        private List<Transform> spawnPoss;

        [Header("Data (Creeps)")] 
        [SerializeField]
        private CreepDataSo[] creepDataSo;

        [SerializeField] private CreepSpawnPatternCollectionSO creepSpawnPatternCollection;
        [SerializeField] private SpawnRatePatternSO spawnRatePattern;

        [Header("Data (Boss)")] [SerializeField]
        private BossDataSO[] bossData;

        [Header("Stage")] [SerializeField] private int currentStage = 1;
        [SerializeField] private int stagesPerPattern = 10;
        [SerializeField] private int battleTime = 20;
        [SerializeField] private ChapterStageSO[] chapterStages;
        [Header("Hero Team")]
        [SerializeField] private HeroTeamController heroTeamController;

        private BattleResult result = BattleResult.None;
        private readonly List<EnemyActor> creeps = new();
        private int aliveCreepCount;
        private int deadCreepCount;
        private int totalCreepsSpawnedThisStage;
        private bool isBossAlive;
        private int patternId;
        private int[] enemyIds;
        private float[] rates;

        private List<int> inBattleHeroIdList = new();
        private Dictionary<int, CreepDataSo> creepDataMapper;
        private Dictionary<int, BossDataSO> bossDataMapper;

        private GameData gameData;
        private BossActor currentBoss;

        private bool isReadyBattle = false;
        private bool losingStage = false;
        
        private readonly HeroActor[] inBattleHeroes = new HeroActor[2];

        private readonly Dictionary<int, HeroActor> inBattleHeroMapper = new();

        private HeroActor inBattleHeroA;
        private HeroActor inBattleHeroB;

        public BattleState State { get; private set; } = BattleState.None;
        public List<EnemyActor> MonsterList => creeps;
        
        private void Start()
        {
            GameEventManager.Subscribe(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe(GameEvents.OnChangeHero, (Action<int, int>)OnChangeHero);
            GameEventManager.Subscribe(GameEvents.OnNextStageButtonClicked, () => NextStageCallback().Forget());
            GameEventManager.Subscribe(GameEvents.OnStageLost, () => OnStageFailed().Forget());
        }

        public override async UniTask InitializeAsync()
        {
            BuildCreepDataLookup();
            BuildSpawnPositions();
            BuildBossDataLookup();
            gameData = GameData.Instance;
            await StartAsync();        
        }

        private async UniTask StartAsync()
        {
            SetState(BattleState.Initializing);

            currentStage = Mathf.Max(1, currentStage);
            pvEMapController.InitMapByChapter(GetChapterIdByStage(currentStage));
            InitSwitchableHeroIds();
            await InitPlayerHeroById();
            NotifyActiveLineupChanged();
            InitStage(currentStage);

            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
        }

        public Vector3 GetMapEndPoint()
        {
            return pvEMapController.EndMapPoint;
        }

        private void OnChangeHero(int sourceHeroId, int targetHeroId)
        {
            if (!CanSwitchHero(sourceHeroId, targetHeroId))
                return;

            int slotIndex = inBattleHeroIdList.IndexOf(sourceHeroId);

            if (slotIndex < 0 || slotIndex >= inBattleHeroes.Length)
            {
                Debug.LogError($"[PvE] Cannot find slot for sourceHeroId={sourceHeroId}");
                return;
            }

            HeroActor oldHero = inBattleHeroes[slotIndex];

            if (oldHero == null)
            {
                Debug.LogError($"[PvE] Old hero is null. sourceHeroId={sourceHeroId}, slot={slotIndex}");
                return;
            }

            Vector3 spawnPos = oldHero.transform.position;

            HeroDataSO newHeroData = MasterDataCache.Instance.GetHeroDataById(targetHeroId);

            if (newHeroData == null)
            {
                Debug.LogError($"[PvE] Cannot find target hero data. targetHeroId={targetHeroId}");
                return;
            }

            if (newHeroData.HeroPrefab == null)
            {
                Debug.LogError($"[PvE] Target hero prefab is null. targetHeroId={targetHeroId}");
                return;
            }

            inBattleHeroIdList[slotIndex] = targetHeroId;

            inBattleHeroMapper.Remove(sourceHeroId);

            oldHero.gameObject.SetActive(false);
            PoolController.Instance.ReturnToPool(oldHero.gameObject);

            var (newHero, _) = PoolController.Instance.Get(
                newHeroData.HeroPrefab,
                spawnPos
            );

            if (newHero == null)
                return;

            newHero.gameObject.SetActive(true);
            newHero.Init(newHeroData, this);

            inBattleHeroes[slotIndex] = newHero;
            inBattleHeroMapper[targetHeroId] = newHero;

            RefreshHeroSlotCache();
            RefreshHeroTeamController();
            RefreshEnemyHeroTargets();
            NotifyActiveLineupChanged();
        }
        

        private void RefreshBattleUISlot(PlayerHeroController heroController, int heroId)
        {
            if (heroController == null) return;

            var heroData = MasterDataCache.Instance.GetHeroDataById(heroId);
            if (heroData == null) return;

            UIHeroBattleController.Instance?.ReplaceHeroSlot(
                heroController,
                heroId,
                heroController.SkillIdDict
            );
        }

        public void InitSwitchableHeroIds()
        {
            // asign to 1 and 2
            inBattleHeroIdList.Clear();
            inBattleHeroIdList = new List<int>() { 2, 4 };
        }

        private async UniTask InitPlayerHeroById(bool isSwitch = false)
        {
            for (int heroIndex = 0; heroIndex < inBattleHeroIdList.Count; heroIndex++)
            {
                var id = inBattleHeroIdList[heroIndex];
                var heroDt = MasterDataCache.Instance.GetHeroDataById(id);
                await SpawnHero(heroDt, heroIndex);
            }

            await UniTask.Delay(1000);
        }

        private async UniTask SpawnHero(HeroDataSO heroData, int heroIndex)
        {
            if (heroData == null)
            {
                Debug.LogError($"[PvE] HeroData is null. heroIndex={heroIndex}");
                return;
            }

            if (heroData.HeroPrefab == null)
            {
                Debug.LogError($"[PvE] HeroPrefab is null. heroId={heroData.Id}");
                return;
            }

            Vector3 spawnPos = GetHeroSpawnPosition(heroIndex);

            var (hero, _) = PoolController.Instance.Get(
                heroData.HeroPrefab,
                spawnPos
            );

            if (hero == null)
            {
                Debug.LogError($"[PvE] Cannot spawn hero. heroId={heroData.Id}");
                return;
            }

            hero.gameObject.SetActive(true);
            hero.Init(heroData, this);

            inBattleHeroes[heroIndex] = hero;
            inBattleHeroMapper[heroData.Id] = hero;

            RefreshHeroSlotCache();
            RefreshHeroTeamController();
            RefreshEnemyHeroTargets();

            await UniTask.Delay(100);
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
            inBattleHeroA = inBattleHeroes[0];
            inBattleHeroB = inBattleHeroes[1];
        }
        
        private void RefreshHeroTeamController()
        {
            if (heroTeamController == null)
                return;

            heroTeamController.SetHeroes(
                inBattleHeroA,
                inBattleHeroB
            );
        }

        private void BuildBossDataLookup()
        {
            bossDataMapper = new Dictionary<int, BossDataSO>();

            if (bossData == null) return;

            for (int i = 0; i < bossData.Length; i++)
            {
                var data = bossData[i];
                if (data == null) continue;

                bossDataMapper[data.Id] = data;
            }
        }

        public List<int> GetCurrentSwitchHeroIds()
        {
            return new List<int>(inBattleHeroIdList);
        }

        public bool CanSwitchHero(int sourceHeroId, int targetHeroId)
        {
            if (sourceHeroId <= 0 || targetHeroId <= 0)
                return false;

            if (sourceHeroId == targetHeroId)
                return false;

            if (!inBattleHeroIdList.Contains(sourceHeroId))
                return false;

            return !inBattleHeroIdList.Contains(targetHeroId);
        }

        public void RequestSwitchHero(int sourceHeroId, int targetHeroId)
        {
            if (!CanSwitchHero(sourceHeroId, targetHeroId))
                return;

            OnChangeHero(sourceHeroId, targetHeroId);
        }

        private async UniTaskVoid HandleNextStage()
        {
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            currentStage++;
            pvEMapController.InitMapByChapter(GetChapterIdByStage(currentStage));
            InitStage(currentStage);
            isReadyBattle = false;
            await UniTask.Delay(1000);
            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
        }

        private void HandleCurrentStage()
        {
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            
            InitStage(currentStage);
            isReadyBattle = false;
            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
        }

        // ======================
        // Stage lifecycle
        // ======================

        private void InitStage(int stage)
        {
            aliveCreepCount = 0;
            totalCreepsSpawnedThisStage = 0;
            deadCreepCount = losingStage ? gameData.maxCreepsPerStage : 0;
            GameEventManager.Trigger(GameEvents.OnEnemyDead, deadCreepCount);
            GameEventManager.Trigger(GameEvents.OnWaveStart);

            isBossAlive = false;
            if (currentBoss != null && currentBoss.gameObject.activeInHierarchy)
            {
                PoolController.Instance.ReturnToPool(currentBoss.gameObject);
                currentBoss = null;
            }

            CacheStageSpawnData(stage);
        }

        private void CacheStageSpawnData(int stage)
        {
            patternId = GetPatternIdByStageLoop(stage, stagesPerPattern);

            enemyIds = creepSpawnPatternCollection.GetSpawnPatternBaseOnId(patternId);
            if (enemyIds == null || enemyIds.Length < 2 || enemyIds.Length > 4)
            {
                Debug.LogError($"[PvE] Invalid enemyIds. stage={stage} patternId={patternId}");
                enemyIds = null;
                rates = null;
                return;
            }

            rates = GetRandomRates(patternId, enemyIds.Length);
            if (rates == null)
            {
                Debug.LogError($"[PvE] No rates found. patternId={patternId}, len={enemyIds.Length}");
            }
        }

        private bool TryGetChapterDataByStage(int stage, out ChapterStageSO chapterData, out int chapterIndex,
            out int localStage)
        {
            chapterData = null;
            chapterIndex = -1;
            localStage = -1;

            if (chapterStages == null || chapterStages.Length == 0 || stage <= 0)
                return false;

            int accumulatedStage = 0;

            for (int i = 0; i < chapterStages.Length; i++)
            {
                var data = chapterStages[i];
                if (data == null || data.TotalStage <= 0) continue;

                int startStage = accumulatedStage + 1;
                int endStage = accumulatedStage + data.TotalStage;

                if (stage >= startStage && stage <= endStage)
                {
                    chapterData = data;
                    chapterIndex = i;
                    localStage = stage - accumulatedStage;
                    return true;
                }

                accumulatedStage = endStage;
            }

            return false;
        }

        private int GetChapterIdByStage(int stage)
        {
            if (chapterStages == null || chapterStages.Length == 0 || stage <= 0)
                return 0;

            int accumulatedStage = 0;

            for (int i = 0; i < chapterStages.Length; i++)
            {
                var data = chapterStages[i];
                if (data == null || data.TotalStage <= 0) continue;

                int startStage = accumulatedStage;
                int endStage = accumulatedStage + data.TotalStage;

                if (stage > startStage && stage <= endStage)
                {
                    return i;
                }

                accumulatedStage = endStage;
            }

            return 0;
        }

        private bool TryGetBossDataByStage(int stage, out BossDataSO bossSo)
        {
            bossSo = null;

            if (!TryGetChapterDataByStage(stage, out var chapterData, out _, out _))
                return false;

            if (bossDataMapper == null || bossDataMapper.Count == 0)
                return false;

            return bossDataMapper.TryGetValue(chapterData.BossId, out bossSo) && bossSo != null;
        }

        private void SpawnNextCreepBatch()
        {
            if (enemyIds == null || rates == null) return;

            int remaining = gameData.maxCreepsPerStage - totalCreepsSpawnedThisStage;
            if (remaining <= 0 && !losingStage) return;

            int spawnNow = Mathf.Min(gameData.creepBatchSize, losingStage ? gameData.creepBatchSize : remaining);

            int[] counts = AllocateCounts(spawnNow, rates);
            if (counts == null) return;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                int enemyId = enemyIds[i];
                int amount = counts[i];

                if (!TryGetCreepPrefab(enemyId, out var creepData))
                {
                    Debug.LogError($"[PvE] Missing CreepDataSo/Prefab for enemyId={enemyId}");
                    continue;
                }

                for (int k = 0; k < amount; k++)
                {
                    var basePos = spawnPoss[Random.Range(0, spawnPoss.Count)].position;
                    var nPos = basePos + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));

                    var (creep, _) = PoolController.Instance.Get(creepData.CreepPrefab, nPos);

                    creep.gameObject.SetActive(true);

                    creep.transform.localScale = k % 5 == 0
                        ? Vector3.one * 1.5f
                        : Vector3.one;

                    creep.Init(
                        creepData,
                        inBattleHeroA,
                        inBattleHeroB
                    );

                    creep.OnDead -= NotifyMonsterDeath;
                    creep.OnDead += NotifyMonsterDeath;

                    creeps.Add(creep);
                    aliveCreepCount++;
                    totalCreepsSpawnedThisStage++;
                }
            }
        }

        private void SpawnBoss()
        {
            // if (!TryGetBossDataByStage(currentStage, out var bossSo))
            // {
            //     Debug.LogError($"[PvE] Cannot resolve boss data for stage={currentStage}");
            //     return;
            // }
            //
            // if (bossSo.bossPrefab == null)
            // {
            //     Debug.LogError($"[PvE] Boss prefab missing for bossId={bossSo.Id}");
            //     return;
            // }
            //
            // if (currentBoss != null && currentBoss.gameObject.activeInHierarchy)
            //     return;
            //
            // Debug.Log($"[PvE] Spawn Boss - Stage={currentStage}, BossId={bossSo.Id}, BossName={bossSo.Name}");
            //
            // var pos = GroupFlashController.Instance.GetPosByIdx(2);
            // var (spawnedBoss, _) = PoolController.Instance.Get(bossSo.bossPrefab, pos);
            // currentBoss = spawnedBoss;
            //
            // BaseStat bossStat = new BaseStat
            // {
            //     Element = bossSo.Element,
            //     Health = bossSo.BaseHP * Mathf.Pow(1.15f, currentStage - 1f),
            //     Attack = bossSo.BaseAtk * Mathf.Pow(1.10f, currentStage - 1f),
            //     Defense = bossSo.BaseDef * Mathf.Pow(1.08f, currentStage - 1f),
            //     AttackRange = bossSo.AttackRange
            // };
            //
            // currentBoss.InitMonster(bossSo.Id, inBattleHeroCollection[0], this, bossStat, true, inBattleHeroCollection.ToList());            
        }

        public void NotifyBossReady()
        {
            // for (int i = 0; i < inBattleHeroCollection.Length; i++)
            // {
            //     inBattleHeroCollection[i].SetTarget(currentBoss);
            // }
            //
            // GameStatView.Instance.InitTimer(battleTime, () =>
            // {
            //     if (State == BattleState.Ended)
            //     {
            //         return;
            //     }
            //
            //     GameEventManager.Trigger(GameEvents.OnStageLost);
            // });
            //
            // isBossAlive = true;
            // SetState(BattleState.FightingBoss);
        }

        private void OnStageCleared()
        {
            //currentBoss.IsReady = false;
            result = BattleResult.Victory;
            SetState(BattleState.Ended);
            losingStage = false;
        }

        public async UniTask OnStageFailed()
        {
            await UniTask.Delay(2000);
            result = BattleResult.Defeat;
            PoolController.Instance.ReturnToPool(currentBoss.gameObject);
            currentBoss = null;
            SetState(BattleState.Ended);
            PlayCurrentStage().Forget();
            losingStage = true;
        }

        private void SetState(BattleState newState)
        {
            if (State == newState) return;
            State = newState;
        }

        //khi monster chết 
        public void NotifyMonsterDeath(EnemyActor enemy)
        {
            if (enemy == null)
                return;

            enemy.OnDead -= NotifyMonsterDeath;
            DropCoinAsync(enemy.transform.position);
            creeps.Remove(enemy);
            aliveCreepCount = Mathf.Max(0, aliveCreepCount - 1);
            deadCreepCount = losingStage
                ? GameData.Instance.maxCreepsPerStage
                : deadCreepCount + 1;
            GameEventManager.Trigger(GameEvents.OnEnemyDead, deadCreepCount);

            if (State != BattleState.FightingCreeps)
                return;

            if (aliveCreepCount != 0)
                return;
            
            if (totalCreepsSpawnedThisStage < gameData.maxCreepsPerStage || losingStage)
            {
                isReadyBattle = false;
                SpawnNextCreepBatch();
                isReadyBattle = true;
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

                creep.SetHeroTargets(
                    inBattleHeroA,
                    inBattleHeroB
                );
            }

            if (currentBoss != null && !currentBoss.IsDead)
            {
                currentBoss.SetHeroTargets(
                    inBattleHeroA,
                    inBattleHeroB
                );
            }
        }

        private void DropCoinAsync(Vector3 pos)
        {
            var numRand = Random.Range(2, 5);
            for (int i = 0; i < numRand; i++)
            {
                var coin = PoolController.Instance.Get(coinPrefab, pos + Vector3.up * 0.25f);
                var trans = FindHeroNearestFromPos(pos);
                coin.Item1.DoDrop(0.1f + i * 0.1f, trans).Forget();
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
                return null;

            ICombatUnit nearest = null;
            float nearestSqr = float.MaxValue;

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

                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = creep;
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

                if (bossSqr < nearestSqr)
                {
                    nearest = currentBoss;
                }
            }

            return nearest;
        }
        
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

        public void SpawnBossDirectly()
        {
            SpawnBoss();
            aliveCreepCount = 0;
            for (int i = 0; i < creeps.Count; i++)
            {
                PoolController.Instance.ReturnToPool(creeps[i].gameObject);
                creeps.RemoveAt(i);
            }
        }

        //dung để lấy quái gần nhất cho hero đang cần
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
            /*var radiusX = 5;
            var radiusZ = 8;
            var offsetZ = 15;
            for (int i = 0; i < spawnPoss.Count; i++)
            {
                var xz = new Vector3(radiusX * Mathf.Cos(60 * i), 0, radiusZ * Mathf.Sin(60 * i) + offsetZ);
                spawnPoss[i].position = xz;
            }*/

            if (spawnPoss.Count == 0)
                Debug.LogError("[PvE] No spawn positions assigned");
            else
                spawnPoss = spawnPoss.OrderBy(_ => Random.value).ToList(); // shuffle
        }


        private void BuildCreepDataLookup()
        {
            creepDataMapper = new Dictionary<int, CreepDataSo>();

            if (creepDataSo == null) return;

            for (int i = 0; i < creepDataSo.Length; i++)
            {
                var data = creepDataSo[i];
                if (data == null) continue;

                // assumes CreepDataSo has int Id
                creepDataMapper[data.Id] = data;
            }
        }

        private bool TryGetCreepPrefab(int enemyId, out CreepDataSo creepData)
        {
            creepData = null;

            if (creepDataMapper == null || creepDataMapper.Count == 0) return false;
            if (!creepDataMapper.TryGetValue(enemyId, out var data) || data == null) return false;

            creepData = data;

            return creepData != null;
        }

        public async UniTask NextStageCallback()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(7f));
            Debug.Log("[PvE] Next Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            
            // Dùng để reset hero data
            // for (int i = 0; i < inBattleHeroCollection.Length; i++)
            // {
            //     inBattleHeroCollection[i].ResetHeroData();
            // }

            HandleNextStage().Forget();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }

        private async UniTask PlayCurrentStage()
        {
            Debug.Log("[PvE] Play Current Stage");
            await UniTask.Delay(TimeSpan.FromSeconds(1.2f));
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            
            //dùng để set active hero == false
            // for (int i = 0; i < inBattleHeroCollection.Length; i++)
            // {
            //     inBattleHeroCollection[i].gameObject.SetActive(false);
            // }

            HandleCurrentStage();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            
            //dùng để reset hero data
            // for (int i = 0; i < inBattleHeroCollection.Length; i++)
            // {
            //     inBattleHeroCollection[i].ResetHeroData();
            // }

        }

        public bool IsHeroCurrentlyActive(int heroId)
        {
            if (heroId <= 0) return false;
            return inBattleHeroIdList.Contains(heroId);
        }
    }
}