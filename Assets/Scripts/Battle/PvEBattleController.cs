using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Level.Pattern;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using Random = UnityEngine.Random;

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
        [SerializeField] FollowHeroController mainFollow;
        [SerializeField] FollowHeroController subFollow;
        [SerializeField] PvEMapController pvEMapController;
        [SerializeField] BattleCoinView coinPrefab;

        [Header("Spawn Positions")] [SerializeField]
        private List<Transform> spawnPoss;

        [Header("Data (Creeps)")] [SerializeField]
        private CreepDataSo[] creepDataSo;

        [SerializeField] private CreepSpawnPatternCollectionSO creepSpawnPatternCollection;
        [SerializeField] private SpawnRatePatternSO spawnRatePattern;

        [Header("Data (Boss)")] [SerializeField]
        private BossDataSO[] bossData;

        [Header("Stage")] [SerializeField] private int currentStage = 1;
        [SerializeField] private int stagesPerPattern = 10;
        [SerializeField] private int battleTime = 20;
        [SerializeField] private ChapterStageSO[] chapterStages;

        private BattleResult result = BattleResult.None;
        private readonly List<MonsterScrepController> creeps = new();
        private int aliveCreepCount;
        private int deadCreepCount;
        private int totalCreepsSpawnedThisStage;
        private bool isBossAlive;
        private int patternId;
        private int[] enemyIds;
        private float[] rates;

        private Dictionary<int, CreepDataSo> creepDataDict;
        private Dictionary<int, BossDataSO> bossDataDict;
        private Dictionary<int, PlayerHeroController> switchablePlayers = new();
        private GameData gameData;
        private MonsterBossController currentBoss;
        private List<int> switchHeroIds = new();
        private PlayerHeroController firstPlayerHeroController;
        private PlayerHeroController secondPlayerHeroController;
        private PlayerHeroController mainPlayerHeroController;
        private bool isReadyBattle = false;
        private bool losingStage = false;

        public BattleState State { get; private set; } = BattleState.None;
        public List<MonsterScrepController> MonsterList => creeps;

        public bool IsReadyBattle
        {
            get => isReadyBattle;
            set => isReadyBattle = value;
        }
        
        private void Start()
        {
            GameEventManager.Subscribe(GameEvents.OnStageCleared, OnStageCleared);
            //GameEventManager.Subscribe(GameEvents.OnStageLost, OnStageFailed);
            GameEventManager.Subscribe(GameEvents.OnChangeHero, (Action<int, int>)OnChangeHero);
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

        private void OnChangeHero(int sId, int tId)
        {
            if (!CanSwitchHero(sId, tId))
                return;

            bool replaceFirst = firstPlayerHeroController != null && firstPlayerHeroController.GetHeroId() == sId;
            bool replaceSecond = secondPlayerHeroController != null && secondPlayerHeroController.GetHeroId() == sId;

            if (!replaceFirst && !replaceSecond)
                return;

            var oldHero = replaceFirst ? firstPlayerHeroController : secondPlayerHeroController;
            if (oldHero == null)
                return;

            bool wasMain = mainPlayerHeroController != null &&
                           mainPlayerHeroController.GetHeroId() == sId;
            Vector3 spawnPos = oldHero.transform.position;
            var heroDt = MasterDataCache.Instance.GetHeroDataById(tId);
            if (heroDt == null)
                return;

            if (replaceFirst)
                switchHeroIds[0] = tId;
            else
                switchHeroIds[1] = tId;

            if (switchablePlayers.ContainsKey(sId))
                switchablePlayers.Remove(sId);

            oldHero.gameObject.SetActive(false);

            var (newHero, isNewInstance) = PoolController.Instance.Get(heroDt.PlayerHeroController, spawnPos);
            if (newHero == null)
                return;

            switchablePlayers[tId] = newHero;

            if (replaceFirst)
            {
                firstPlayerHeroController = newHero;
                if (wasMain)
                {
                    mainPlayerHeroController = firstPlayerHeroController;
                }

                firstPlayerHeroController.InitHero(
                    heroDt,
                    this,
                    playerCamController,
                    skillObjTrans,
                    secondPlayerHeroController != null ? secondPlayerHeroController.transform : null,
                    mainFollow,
                    heroDt.HeroClass,
                    false,
                    wasMain
                );

                RefreshBattleUISlot(firstPlayerHeroController, true, tId);
            }
            else
            {
                secondPlayerHeroController = newHero;
                if (wasMain)
                {
                    mainPlayerHeroController = secondPlayerHeroController;
                }

                secondPlayerHeroController.InitHero(
                    heroDt,
                    this,
                    playerCamController,
                    skillObjTrans,
                    firstPlayerHeroController != null ? firstPlayerHeroController.transform : null,
                    subFollow,
                    heroDt.HeroClass,
                    false,
                    wasMain
                );

                RefreshBattleUISlot(secondPlayerHeroController, false, tId);
            }

            if (firstPlayerHeroController != null && secondPlayerHeroController != null)
            {
                firstPlayerHeroController.SetPartner(secondPlayerHeroController.transform);
                secondPlayerHeroController.SetPartner(firstPlayerHeroController.transform);
            }
            NotifyActiveLineupChanged();
        }
        

        private void RefreshBattleUISlot(PlayerHeroController heroController, bool isFirstSlot, int heroId)
        {
            if (heroController == null) return;

            var heroData = MasterDataCache.Instance.GetHeroDataById(heroId);
            if (heroData == null) return;

            UIHeroBattleController.Instance?.ReplaceHeroSlot(
                heroController,
                isFirstSlot,
                heroId,
                heroController.SkillIdDict
            );
        }

        public void InitSwitchableHeroIds()
        {
            // asign to 1 and 2
            switchHeroIds.Clear();
            switchHeroIds = new List<int>() { 1, 3 };
        }

        private async UniTask InitPlayerHeroById(bool isSwitch = false)
        {
            for (int i = 0; i < switchHeroIds.Count; i++)
            {
                var id = switchHeroIds[i];
                var heroDt = MasterDataCache.Instance.GetHeroDataById(id);
                await SpawnHero(heroDt, i == 0);
            }

            await UniTask.Delay(1000);
        }

        private async UniTask SpawnHero(HeroDataSO heroDt, bool isMain = true)
        {
            var (hero, b) = PoolController.Instance.Get<PlayerHeroController>(heroDt.PlayerHeroController,
                isMain ? Vector3.forward * 12 + Vector3.left * 1.5f : Vector3.forward * 12 + Vector3.right * 1.5f);
            switchablePlayers[heroDt.Id] = hero;
            if (isMain)
            {
                firstPlayerHeroController = hero;
                firstPlayerHeroController.InitHero(heroDt, this, playerCamController, skillObjTrans,
                    secondPlayerHeroController?.transform ?? null, mainFollow, heroDt.HeroClass, false, true);
                mainPlayerHeroController = firstPlayerHeroController;
                mainFollow.SetFollowTarget(firstPlayerHeroController.transform);
            }
            else
            {
                secondPlayerHeroController = hero;
                secondPlayerHeroController.InitHero(heroDt, this, playerCamController, skillObjTrans,
                    firstPlayerHeroController.transform, subFollow, heroDt.HeroClass, false, false);
                firstPlayerHeroController.SetPartner(secondPlayerHeroController.transform);
                subFollow.SetFollowTarget(secondPlayerHeroController.transform);
            }
            
            await UniTask.Delay(1000);
            NotifyActiveLineupChanged();
        }

        private void BuildBossDataLookup()
        {
            bossDataDict = new Dictionary<int, BossDataSO>();

            if (bossData == null) return;

            for (int i = 0; i < bossData.Length; i++)
            {
                var data = bossData[i];
                if (data == null) continue;

                bossDataDict[data.Id] = data;
            }
        }

        public void SwitchHero(int hId)
        {
            if (mainPlayerHeroController.GetHeroId() == hId) return;

            var hero = firstPlayerHeroController.GetHeroId() == hId
                ? firstPlayerHeroController
                : secondPlayerHeroController;
            mainPlayerHeroController = hero;
        }

        public List<int> GetCurrentSwitchHeroIds()
        {
            return new List<int>(switchHeroIds);
        }

        public bool CanSwitchHero(int sourceHeroId, int targetHeroId)
        {
            if (sourceHeroId <= 0 || targetHeroId <= 0)
                return false;

            if (sourceHeroId == targetHeroId)
                return false;

            if (!switchHeroIds.Contains(sourceHeroId))
                return false;

            if (switchHeroIds.Contains(targetHeroId))
                return false;

            return true;
        }

        public void RequestSwitchHero(int sourceHeroId, int targetHeroId)
        {
            if (!CanSwitchHero(sourceHeroId, targetHeroId))
                return;

            OnChangeHero(sourceHeroId, targetHeroId);
        }

        private void HandleNextStage()
        {
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            currentStage++;
            pvEMapController.InitMapByChapter(GetChapterIdByStage(currentStage));
            InitStage(currentStage);
            isReadyBattle = false;
            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
        }

        private void HandleCurrentStage()
        {
            result = BattleResult.None;
            SetState(BattleState.Initializing);
            ;
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

            if (bossDataDict == null || bossDataDict.Count == 0)
                return false;

            return bossDataDict.TryGetValue(chapterData.BossId, out bossSo) && bossSo != null;
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
                    if (k % 5 == 0)
                    {
                        creep.transform.localScale = Vector3.one * 1.5f;
                    }
                    else
                    {
                        creep.transform.localScale = Vector3.one;
                    }

                    int hid = creep.GetInstanceID();
                    BaseStat monsterStat = new BaseStat
                    {
                        AttackRange = creepData.BaseRange,
                        Element = creepData.Element,
                        Health = creepData.BaseHp * Mathf.Pow(1.12f, currentStage - 1f),
                        Attack = creepData.BaseAtk * Mathf.Pow(1.08f, currentStage - 1f),
                        Defense = creepData.BaseDef * Mathf.Pow(1.06f, currentStage - 1f),
                        MoveSpeed = creepData.BaseMoveSpeed,
                    };
                    var target = UnityEngine.Random.Range(0, 2);
                    creep.InitMonster(hid, target == 0 ? firstPlayerHeroController : secondPlayerHeroController, this,
                        monsterStat, false,
                        new List<PlayerHeroController>() { firstPlayerHeroController, secondPlayerHeroController });

                    creeps.Add(creep);
                    aliveCreepCount++;
                    totalCreepsSpawnedThisStage++;
                }
            }
        }

        private void SpawnBoss()
        {
            if (!TryGetBossDataByStage(currentStage, out var bossSo))
            {
                Debug.LogError($"[PvE] Cannot resolve boss data for stage={currentStage}");
                return;
            }

            if (bossSo.bossPrefab == null)
            {
                Debug.LogError($"[PvE] Boss prefab missing for bossId={bossSo.Id}");
                return;
            }

            if (currentBoss != null && currentBoss.gameObject.activeInHierarchy)
                return;

            Debug.Log($"[PvE] Spawn Boss - Stage={currentStage}, BossId={bossSo.Id}, BossName={bossSo.Name}");

            var pos = GroupFlashController.Instance.GetPosByIdx(2);
            var (spawnedBoss, _) = PoolController.Instance.Get(bossSo.bossPrefab, pos);
            currentBoss = spawnedBoss;

            BaseStat bossStat = new BaseStat
            {
                Element = bossSo.Element,
                Health = bossSo.BaseHP * Mathf.Pow(1.15f, currentStage - 1f),
                Attack = bossSo.BaseAtk * Mathf.Pow(1.10f, currentStage - 1f),
                Defense = bossSo.BaseDef * Mathf.Pow(1.08f, currentStage - 1f),
                AttackRange = bossSo.AttackRange
            };

            currentBoss.InitMonster(bossSo.Id, mainPlayerHeroController, this, bossStat, true,
                new List<PlayerHeroController>() { firstPlayerHeroController, secondPlayerHeroController });

            firstPlayerHeroController.SetTarget(currentBoss);
            secondPlayerHeroController.SetTarget(currentBoss);

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

        private void OnStageCleared()
        {
            result = BattleResult.Victory;
            SetState(BattleState.Ended);
            losingStage = false;
        }

        public void OnStageFailed()
        {
            result = BattleResult.Defeat;
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

        public void NotifyMonsterDeath(MonsterScrepController creep)
        {
            if (creep == null) return;
            if (!creep.gameObject.activeInHierarchy) return;

            DropCoinAsync(creep.transform.position).Forget();
            PoolController.Instance.ReturnToPool(creep.gameObject);
            creeps.Remove(creep);
            aliveCreepCount = Mathf.Max(0, aliveCreepCount - 1);
            deadCreepCount = losingStage ? GameData.Instance.maxCreepsPerStage :
                creep.IsBoss() ? deadCreepCount : deadCreepCount + 1;
            GameEventManager.Trigger(GameEvents.OnEnemyDead, deadCreepCount);
            if (State != BattleState.FightingCreeps) return;
            if (aliveCreepCount != 0) return;

            if (totalCreepsSpawnedThisStage < gameData.maxCreepsPerStage || losingStage)
            {
                isReadyBattle = false;
                SpawnNextCreepBatch();
                isReadyBattle = true;
                return;
            }

            SpawnBoss();
        }

        private async UniTaskVoid DropCoinAsync(Vector3 pos)
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
            float dist = float.MaxValue;
            if (firstPlayerHeroController)
                dist = (firstPlayerHeroController.transform.position - pos).magnitude;

            if(secondPlayerHeroController)
            {
                var dist2 = (secondPlayerHeroController.transform.position - pos).magnitude;
                if(dist2 < dist) return secondPlayerHeroController.transform;
            }

            if(dist < float.MaxValue)
                return firstPlayerHeroController.transform;

            return null;
        }
        
        public MonsterScrepController GetNearestMonster(Vector3 pos)
        {
            if (!isReadyBattle) return null;

            float nearest = float.MaxValue;
            MonsterScrepController target = null;

            for (int i = 0; i < creeps.Count; i++)
            {
                var c = creeps[i];
                float d = Vector3.Distance(c.transform.position, pos);
                if (d < nearest)
                {
                    nearest = d;
                    target = c;
                }
            }

            if (target == null && State == BattleState.FightingBoss && currentBoss.gameObject.activeInHierarchy &&
                !currentBoss.IsDead)
            {
                target = currentBoss;
            }

            return target;
        }

        public MonsterScrepController GetFarestMonster(Vector3 pos)
        {
            if (!isReadyBattle) return null;
            float farest = 0;
            MonsterScrepController target = null;

            for (int i = 0; i < creeps.Count; i++)
            {
                var c = creeps[i];
                float d = Vector3.Distance(c.transform.position, pos);
                if (d > farest)
                {
                    farest = d;
                    target = c;
                }
            }

            if (target == null && State == BattleState.FightingBoss && currentBoss.gameObject.activeInHierarchy &&
                !currentBoss.IsDead)
            {
                target = currentBoss;
            }

            return target;
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

        public List<MonsterScrepController> GetNearestMonstesInRange(Vector3 pos, float range)
        {
            List<MonsterScrepController> targets = null;
            if (State == BattleState.FightingBoss)
            {
                if (currentBoss != null &&
                    currentBoss.gameObject.activeInHierarchy &&
                    !currentBoss.IsDead &&
                    (currentBoss.transform.position - pos).sqrMagnitude < range * range)
                {
                    return new List<MonsterScrepController> { currentBoss };
                }

                return targets;
            }

            if (creeps.Count == 0) return targets;

            float sqrRange = range * range;

            for (int i = 0; i < creeps.Count; i++)
            {
                var c = creeps[i];
                if (c == null || !c.gameObject.activeInHierarchy) continue;

                float sqrDist = (c.transform.position - pos).sqrMagnitude;
                if (sqrDist <= sqrRange)
                {
                    targets ??= new List<MonsterScrepController>();
                    targets.Add(c);
                }
            }

            return targets;
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
            creepDataDict = new Dictionary<int, CreepDataSo>();

            if (creepDataSo == null) return;

            for (int i = 0; i < creepDataSo.Length; i++)
            {
                var data = creepDataSo[i];
                if (data == null) continue;

                // assumes CreepDataSo has int Id
                creepDataDict[data.Id] = data;
            }
        }

        private bool TryGetCreepPrefab(int enemyId, out CreepDataSo creepData)
        {
            creepData = null;

            if (creepDataDict == null || creepDataDict.Count == 0) return false;
            if (!creepDataDict.TryGetValue(enemyId, out var data) || data == null) return false;

            creepData = data;

            return creepData != null;
        }

        private void MakeEnemyDead()
        {
            var monster = creeps.Find(c => c.gameObject.activeInHierarchy);
            NotifyMonsterDeath(monster);
        }

        public async UniTask NextStageCallback()
        {
            Debug.Log("[PvE] Next Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            firstPlayerHeroController.ResetHeroData();
            secondPlayerHeroController.ResetHeroData();

            HandleNextStage();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
        }

        private async UniTask PlayCurrentStage()
        {
            Debug.Log("[PvE] Play Current Stage");
            await UniTask.Delay(TimeSpan.FromSeconds(1.2f));
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            firstPlayerHeroController.gameObject.SetActive(false);
            secondPlayerHeroController.gameObject.SetActive(false);

            HandleCurrentStage();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            firstPlayerHeroController.ResetHeroData();
            secondPlayerHeroController.ResetHeroData();
        }

        public bool IsHeroCurrentlyActive(int heroId)
        {
            if (heroId <= 0) return false;
            return switchHeroIds.Contains(heroId);
        }

        public int GetFirstActiveHeroId()
        {
            if (switchHeroIds == null || switchHeroIds.Count <= 0)
                return -1;

            return switchHeroIds[0];
        }

        public int GetSecondActiveHeroId()
        {
            if (switchHeroIds == null || switchHeroIds.Count <= 1)
                return -1;

            return switchHeroIds[1];
        }
    }
}