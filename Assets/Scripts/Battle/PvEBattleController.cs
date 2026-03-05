using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts;
using UnityEngine;
using Scripts.Common;


#if UNITY_EDITOR
using NaughtyAttributes;
#endif

namespace Scripts.Battle
{
    public class PvEBattleController : MonoBehaviour
    {
        public enum BattleState
        {
            None,
            Initializing,
            FightingCreeps,
            FightingBoss,
            StageCleared
        }

        [Header("Refs")]
        [SerializeField] private PlayerHeroController playerHeroController;
        [SerializeField] private MonsterBossController boss;

        [Header("Spawn Positions")]
        [SerializeField] private List<Transform> leftSpawnPoss;
        [SerializeField] private List<Transform> rightSpawnPoss;

        [Header("Data (Creeps)")]
        [SerializeField] private CreepDataSo[] creepDataSo;
        [SerializeField] private CreepSpawnPatternCollectionSO creepSpawnPatternCollection; 
        [SerializeField] private SpawnRatePatternSO spawnRatePattern; 

        [Header("Stage")]
        [SerializeField] private int currentStage = 1;
        [SerializeField] private int stagesPerPattern = 10;

        [Header("Config")]
        [SerializeField] private int maxCreepsPerStage = 80;
        [SerializeField] private int creepBatchSize = 40;
        
        private BattleState state = BattleState.None;
        
        private readonly List<MonsterScrepController> creeps = new();
        private int aliveCreepCount;
        private int totalCreepsSpawnedThisStage;
        private bool isBossAlive;
        private int patternId;
        private int[] enemyIds;
        private float[] rates;
        private List<Transform> spawnPoss;
        private Dictionary<int, CreepDataSo> creepDataDict;

        public BattleState State => state;

        private void Awake()
        {
            BuildCreepDataLookup();
            spawnPoss = BuildSpawnPositions();
        }

        private void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            SetState(BattleState.Initializing);

            currentStage = Mathf.Max(1, currentStage);
            await UIManager.Instance.InitMainScene();

            InitStage(currentStage);

            playerHeroController.InitHero(this);

            // Start fighting creeps right away
            SpawnNextCreepBatch();
            SetState(BattleState.FightingCreeps);
        }

        private void HandleNextStage()
        {
            SetState(BattleState.Initializing);
            currentStage++;
            InitStage(currentStage);
            SpawnNextCreepBatch();
            SetState(BattleState.FightingCreeps);
        }

        // ======================
        // Stage lifecycle
        // ======================

        private void InitStage(int stage)
        {
            aliveCreepCount = 0;
            totalCreepsSpawnedThisStage = 0;

            isBossAlive = false;
            if (boss != null) boss.gameObject.SetActive(false);

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

        // ======================
        // Core flow
        // ======================

        private void SpawnNextCreepBatch()
        {
            if (enemyIds == null || rates == null) return;

            int remaining = maxCreepsPerStage - totalCreepsSpawnedThisStage;
            if (remaining <= 0) return;

            int spawnNow = Mathf.Min(creepBatchSize, remaining);

            // Stable per-batch distribution
            int[] counts = AllocateCounts(spawnNow, rates);
            if (counts == null) return;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                int enemyId = enemyIds[i];
                int amount = counts[i];

                if (!TryGetCreepPrefab(enemyId, out var prefab))
                {
                    Debug.LogError($"[PvE] Missing CreepDataSo/Prefab for enemyId={enemyId}");
                    continue;
                }

                for (int k = 0; k < amount; k++)
                {
                    var basePos = spawnPoss[Random.Range(0, spawnPoss.Count)].position;
                    var nPos = basePos + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

                    var creep = PoolController.Instance.Get(prefab, nPos);
                    //var creep = Instantiate(prefab, nPos, Quaternion.identity);
                    int hid = creep.GetInstanceID();

                    // If you have an overload InitMonster(hid, enemyId, hero, battle) -> use it:
                    // creep.InitMonster(hid, enemyId, playerHeroController, this);
                    creep.InitMonster(hid, playerHeroController, this);

                    creeps.Add(creep);
                    aliveCreepCount++;
                    totalCreepsSpawnedThisStage++;
                }
            }
        }

        private void SpawnBoss()
        {
            if (boss == null)
            {
                Debug.LogError("[PvE] Boss ref missing");
                return;
            }

            if (boss.gameObject.activeInHierarchy) return;

            Debug.Log("[PvE] Spawn Boss");

            var pos = GroupFlashController.Instance.GetPosByIdx(2) + Vector3.forward * 30;
            boss.transform.position = pos;
            boss.gameObject.SetActive(true);

            int hid = boss.GetInstanceID();
            boss.InitMonster(hid, playerHeroController, this);
            playerHeroController.SetTarget(boss);
            TopMainView.Instance?.GetBattleTimerIntance()?.InitTimer(120, () => TopMainView.Instance?.GetBattleResultIntance()?.ShowBattleResult(false));
            isBossAlive = true;
            SetState(BattleState.FightingBoss);
        }

        private void OnStageCleared()
        {
            // TODO: you implement: rewards, stage++, InitStage(next), etc.
            Debug.Log("[PvE] Stage Cleared");
        }

        private void SetState(BattleState newState)
        {
            if (state == newState) return;
            state = newState;
            // Debug.Log($"[PvE] State => {state}");
        }

        // ======================
        // Death notifications
        // ======================

        // Creep calls this when it dies
        public void NotifyMonsterDeath(MonsterScrepController creep)
        {
            if (creep == null) return;
            if (!creep.gameObject.activeInHierarchy) return;

            //creep.gameObject.SetActive(false);
            PoolController.Instance.ReturnToPool(creep.gameObject);
            creeps.Remove(creep);
            aliveCreepCount = Mathf.Max(0, aliveCreepCount - 1);

            if (state != BattleState.FightingCreeps) return;
            if (aliveCreepCount != 0) return;

            // All creeps are dead
            if (totalCreepsSpawnedThisStage < maxCreepsPerStage)
            {
                SpawnNextCreepBatch();
                return;
            }

            // Reached stage cap => Condition A = spawn boss
            SpawnBoss();
        }

        // Boss calls this when it dies
        public void NotifyBossDeath()
        {
            if (state != BattleState.FightingBoss) return;
            if (!isBossAlive) return;

            isBossAlive = false;
            if (boss != null) boss.gameObject.SetActive(false);

            SetState(BattleState.StageCleared);
            OnStageCleared();
        }

        // ======================
        // Target queries (skills)
        // ======================

        public MonsterScrepController GetNearestMonster(Vector3 pos)
        {
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

            return target;
        }

        public List<MonsterScrepController> GetNearestMonstesInRange(Vector3 pos, float range)
        {
            List<MonsterScrepController> targets = null;
            if (state == BattleState.FightingBoss)
            {
                return new List<MonsterScrepController> { boss };
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

        // ======================
        // Helpers: pattern & rates
        // ======================

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

        // weights -> int counts; remainder distributed randomly (your rule)
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

        private List<Transform> BuildSpawnPositions()
        {
            var list = new List<Transform>((leftSpawnPoss?.Count ?? 0) + (rightSpawnPoss?.Count ?? 0));
            if (leftSpawnPoss != null) list.AddRange(leftSpawnPoss);
            if (rightSpawnPoss != null) list.AddRange(rightSpawnPoss);

            if (list.Count == 0)
                Debug.LogError("[PvE] No spawn positions assigned");

            return list;
        }

        // ======================
        // Helpers: creep data lookup
        // ======================

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

        private bool TryGetCreepPrefab(int enemyId, out MonsterScrepController prefab)
        {
            prefab = null;

            if (creepDataDict == null || creepDataDict.Count == 0) return false;
            if (!creepDataDict.TryGetValue(enemyId, out var data) || data == null) return false;

            // IMPORTANT:
            // Change this line if your CreepDataSo uses a different field/property name for prefab.
            prefab = data.CreepPrefab;

            return prefab != null;
        }

        [Button]
        private void MakeEnemyDead()
        {
            var monster = creeps.Find(c => c.gameObject.activeInHierarchy);
            NotifyMonsterDeath(monster);
        }
        
        public async UniTask NextStageCallback()
        {
            Debug.Log("[PvE] Next Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            HandleNextStage();
            playerHeroController.ResetHeroData();
            await UniTask.Delay(1000);
            Transitioner.Instance.TransitionInWithoutChangingScene();
        }

    }
}