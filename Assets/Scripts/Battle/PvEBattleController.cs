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

    public partial class PvEBattleController : Singleton<PvEBattleController>
    {
        [Header("Refs")] [SerializeField] FollowHeroController[] enemySpawnerCollection;
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

        private List<int> inBattleHeroIdList = new();
        private Dictionary<int, CreepDataSo> creepDataMapper;
        private Dictionary<int, BossDataSO> bossDataMapper;

        private GameData gameData;
        private BossActor currentBoss;

        private bool isReadyBattle = false;
        private bool losingStage = false;

        private readonly HeroActor[] inBattleHeroes = new HeroActor[2];
        private readonly Dictionary<int, HeroActor> inBattleHeroMapper = new();
        private GameCameraController gameCameraController;

        private HeroActor inBattleHeroA;
        private HeroActor inBattleHeroB;
        
        private readonly List<ICombatUnit> farthestCandidates = new(8);
        private readonly List<float> farthestDistances = new(8);

        public BattleState State { get; private set; } = BattleState.None;
        public List<EnemyActor> MonsterList => creeps;

        private void Start()
        {
            GameEventManager.Subscribe(GameEvents.OnStageCleared, OnStageCleared);
            GameEventManager.Subscribe(GameEvents.OnChangeHero, (Action<int, int>)OnChangeHero);
            GameEventManager.Subscribe(GameEvents.OnStageLost, () => OnStageFailed().Forget());
            GameEventManager.Subscribe<bool>(GameEvents.OnBossSpawnAnimationComplete, OnBossSpawnAnimationComplete);
            gameCameraController = GameCameraController.Instance;
            PoolManager.Instance.Prewarm(coinPrefab, 10);
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
            BuildCreepDataLookup();
            BuildSpawnPositions();
            BuildBossDataLookup();
            gameData = GameData.Instance;
            await StartAsync();
        }

        public void SetAutoSkill(bool isAutoSkill)
        {
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                inBattleHeroes[i].SetAutoSkill(isAutoSkill);
            }
        }

        private async UniTask StartAsync()
        {
            SetState(BattleState.Initializing);

            currentStage = Mathf.Max(1, currentStage);
            pvEMapController.InitMapByChapter(GetChapterIdByStage(currentStage));
            InitSwitchableHeroIds();
            await InitPlayerHeroById();
            NotifyActiveLineupChanged();
            RefreshControlledHeroSkillUI();
            InitStage(currentStage);
            SpawnNextCreepBatch();
            isReadyBattle = true;
            SetState(BattleState.FightingCreeps);
        }

        public Vector3 GetEndMapPoint()
        {
            return pvEMapController.GetEndMapPosition();
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

            var newHero = Instantiate(newHeroData.HeroPrefab,
                spawnPos, Quaternion.identity);

            if (newHero == null)
                return;

            newHero.gameObject.SetActive(true);
            newHero.Init(newHeroData, this, heroTeamController);
            oldHero.OnDead -= OnHeroDead;
            newHero.OnDead += OnHeroDead;

            inBattleHeroes[slotIndex] = newHero;
            inBattleHeroMapper[targetHeroId] = newHero;

            RefreshHeroSlotCache();
            RefreshHeroTeamController();
            RefreshEnemyHeroTargets();
            NotifyActiveLineupChanged();
            RefreshControlledHeroSkillUI();
            if (oldHero.IsChosen)
            {
                gameCameraController.SetFollowHero(newHero.transform);
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

        public void InitSwitchableHeroIds()
        {
            inBattleHeroIdList.Clear();
            inBattleHeroIdList = new List<int>() { 7, 4 };
        }

        public void OnSelectedHeroCastUltimateSkill()
        {
            gameCameraController.ZoomToHero().Forget();
        }

        private async UniTask InitPlayerHeroById(bool isSwitch = false)
        {
            for (int heroIndex = 0; heroIndex < inBattleHeroIdList.Count; heroIndex++)
            {
                var id = inBattleHeroIdList[heroIndex];
                var heroDt = MasterDataCache.Instance.GetHeroDataById(id);
                await SpawnHero(heroDt, heroIndex);
                if (heroIndex == 0)
                {
                    gameCameraController.SetFollowHero(inBattleHeroes[heroIndex].transform);
                }
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

            var hero = Instantiate(heroData.HeroPrefab,
                spawnPos, Quaternion.identity);
            if (hero == null)
            {
                Debug.LogError($"[PvE] Cannot spawn hero. heroId={heroData.Id}");
                return;
            }

            hero.gameObject.SetActive(true);
            hero.Init(heroData, this, heroTeamController);
            hero.OnDead -= OnHeroDead;
            hero.OnDead += OnHeroDead;

            inBattleHeroes[heroIndex] = hero;
            inBattleHeroMapper[heroData.Id] = hero;

            RefreshHeroSlotCache();
            RefreshHeroTeamController();
            RefreshEnemyHeroTargets();

            await UniTask.Delay(TimeSpan.FromSeconds(1f));
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

            ApplyControlledHeroSelectionToTeamController();
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

        public HeroActor GetControlledHero()
        {
            if (controlledHeroSlotIndex < 0 || controlledHeroSlotIndex >= inBattleHeroes.Length)
                controlledHeroSlotIndex = 0;

            return inBattleHeroes[controlledHeroSlotIndex];
        }

        public HeroActor GetFollowerHero()
        {
            int followerSlotIndex = controlledHeroSlotIndex == 0 ? 1 : 0;
            return inBattleHeroes[followerSlotIndex];
        }

        public int GetControlledHeroSlotIndex()
        {
            return controlledHeroSlotIndex;
        }

        public void SelectControlledHeroSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inBattleHeroes.Length)
            {
                Debug.LogWarning($"[PvE] Invalid controlled hero slot index: {slotIndex}");
                return;
            }

            if (inBattleHeroes[slotIndex] == null)
            {
                Debug.LogWarning($"[PvE] Cannot select empty hero slot: {slotIndex}");
                return;
            }

            controlledHeroSlotIndex = slotIndex;
            ApplyControlledHeroSelectionToTeamController();
            RefreshControlledHeroSkillUI();
            gameCameraController.SetFollowHero(inBattleHeroes[slotIndex].transform);
        }

        public void SwitchControlledHero()
        {
            int nextSlotIndex = controlledHeroSlotIndex == 0 ? 1 : 0;
            SelectControlledHeroSlot(nextSlotIndex);
        }

        public BossActor GetActiveBossActor()
        {
            return currentBoss;
        }

        public void OnSwitchMainSubHeroButtonClicked()
        {
            SwitchControlledHero();
        }

        private void ApplyControlledHeroSelectionToTeamController()
        {
            if (heroTeamController == null)
                return;

            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                var hero = inBattleHeroes[i];
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

        private void HandleNextStage()
        {
            heroDeadCount = 0;
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
                PoolManager.Instance.Despawn(currentBoss);
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
            List<Vector3> spawnPositions = GenerateSpawnFormationPositions(spawnNow);
            int spawnIndex = 0;

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
                    Vector3 nPos = spawnIndex < spawnPositions.Count
                        ? spawnPositions[spawnIndex]
                        : spawnPoss[Random.Range(0, spawnPoss.Count)].position;

                    spawnIndex++;

                    var creep = PoolManager.Instance.Spawn(creepData.CreepPrefab, nPos, Quaternion.identity);
                    creep.name += creep.transform.GetInstanceID();
                    creep.gameObject.SetActive(true);

                    creep.SetScale(k % 5 == 0
                        ? 1.5f
                        : 1f);
                    
                    creep.HealthBarController.SetOffsetPosition(k % 5 == 0
                        ? 0.5f
                        : 0f, 0f);

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
            var spawnedBoss = PoolManager.Instance.Spawn(bossSo.bossPrefab, pos, Quaternion.identity);
            currentBoss = spawnedBoss;
            currentBoss.Init(bossSo, inBattleHeroA, inBattleHeroB);
            currentBoss.OnDead -= OnBossDead;
            currentBoss.OnDead += OnBossDead;

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
            if (State == BattleState.Ended)
            {
                return;
            }

            GameEventManager.Trigger(GameEvents.OnStageCleared);
        }

        private void OnStageCleared()
        {
            result = BattleResult.Victory;
            SetState(BattleState.Ended);
            currentBoss = null;
            losingStage = false;
            NextStageCallback().Forget();
        }

        private async UniTask OnStageFailed()
        {
            SetState(BattleState.Ended);
            await UniTask.Delay(1000);
            heroDeadCount = 0;
            result = BattleResult.Defeat;
            PoolManager.Instance.Despawn(currentBoss);
            currentBoss = null;
            PlayCurrentStage().Forget();
            losingStage = true;
        }

        private void SetState(BattleState newState)
        {
            if (State == newState) return;
            State = newState;
        }

        //When normal monster die
        private void NotifyMonsterDeath(EnemyActor enemy)
        {
            if (enemy == null)
                return;

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
                BattleCoinView coin =
                    PoolManager.Instance.Spawn(coinPrefab, pos + Vector3.up * 0.25f, Quaternion.identity);
                var trans = FindHeroNearestFromPos(pos);
                coin.DoDrop(0.1f + i * 0.1f, trans).Forget();
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
            return creeps[index];
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
            for (int i = creeps.Count - 1; i >= 0; i--)
            {
                EnemyActor creep = creeps[i];
                PoolManager.Instance.Despawn(creep);

                creeps.RemoveAt(i);
            }

            SpawnBoss();
            aliveCreepCount = 0;
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


        private void BuildCreepDataLookup()
        {
            creepDataMapper = new Dictionary<int, CreepDataSo>();

            if (creepDataSo == null) return;

            for (int i = 0; i < creepDataSo.Length; i++)
            {
                var data = creepDataSo[i];
                if (data == null) continue;

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

        private async UniTask NextStageCallback()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(5.7f));
            Debug.Log("[PvE] Next Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                Vector3 spawnPos = GetHeroSpawnPosition(i);
                inBattleHeroes[i].ActiveHealthBar(false);
                inBattleHeroes[i].ActiveVisual(false);
                inBattleHeroes[i].ResetSpawnPosition(spawnPos);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                inBattleHeroes[i].ResetData();
                await UniTask.Delay(800);
            }

            HandleNextStage();
        }

        private async UniTask PlayCurrentStage()
        {
            Debug.Log("[PvE] Play Current Stage");
            await Transitioner.Instance.TransitionOutWithoutChangingScene();
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                Vector3 spawnPos = GetHeroSpawnPosition(i);
                inBattleHeroes[i].ActiveHealthBar(false);
                inBattleHeroes[i].ActiveVisual(false);
                inBattleHeroes[i].ResetSpawnPosition(spawnPos);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            Transitioner.Instance.TransitionInWithoutChangingScene();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5));
            for (int i = 0; i < inBattleHeroes.Length; i++)
            {
                inBattleHeroes[i].ResetData();
                await UniTask.Delay(800);
            }

            HandleCurrentStage();
        }

        public bool IsHeroCurrentlyActive(int heroId)
        {
            if (heroId <= 0) return false;
            return inBattleHeroIdList.Contains(heroId);
        }
    }
}