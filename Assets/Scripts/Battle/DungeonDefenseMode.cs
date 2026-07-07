using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Enemy;
using UnityEngine;

namespace Battle.Dungeon
{
    public sealed class DungeonDefenseMode : IDungeonMode
    {
        public DungeonModeType ModeType => DungeonModeType.DefendObjective;

        private DungeonModeContext context;
        private DungeonDefenseObjective objective;
        private int spawnedCount;
        private int killedCount;
        private int aliveCount;
        private float nextBatchDelay;
        private bool active;
        private DefenseDungeonDto defenseDungeonDto;

        public UniTask InitializeAsync(DungeonModeContext context)
        {
            this.context = context;
            objective = null;
            spawnedCount = 0;
            killedCount = 0;
            aliveCount = 0;
            nextBatchDelay = 0f;
            active = false;
            defenseDungeonDto = new DefenseDungeonDto(context.RuntimeData.TempDungeonName, context.RuntimeData.Stage);
            GameEventManager.Trigger(GameEvents.OnDefenseDungeonInit, defenseDungeonDto);
            return UniTask.CompletedTask;
        }

        public void Begin()
        {
            objective = context?.SpawnDefenseObjective?.Invoke();
            if (objective == null)
            {
                context?.Defeat?.Invoke();
                return;
            }

            objective.Stats.HealthModule.OnHPChanged -= OnHpDefenseObjectChange;
            objective.Stats.HealthModule.OnHPChanged += OnHpDefenseObjectChange;
            
            objective.OnDestroyed -= HandleObjectiveDestroyed;
            objective.OnDestroyed += HandleObjectiveDestroyed;
            
            active = true;
            SpawnBatch();
        }

        private void OnHpDefenseObjectChange(float current, float total)
        {
            GameEventManager.Trigger(GameEvents.OnDefenseDungeonDataChanged , current/total);
        }

        public void Tick(float deltaTime)
        {
            if (!active || context == null || objective == null || objective.IsDead)
                return;

            if (spawnedCount >= context.RuntimeData.TotalEnemyCount || aliveCount > 0)
                return;

            nextBatchDelay -= deltaTime;
            if (nextBatchDelay <= 0f)
                SpawnBatch();
        }

        public void NotifyEnemyDead(EnemyActor enemy)
        {
            if (!active || context?.RuntimeData == null)
                return;

            aliveCount = Mathf.Max(0, aliveCount - 1);
            killedCount++;

            context.ScoreChanged?.Invoke(killedCount);

            // Toàn bộ số quái của stage đã được spawn
            // và không còn con nào sống.
            if (spawnedCount >= context.RuntimeData.TotalEnemyCount &&
                aliveCount == 0)
            {
                CompleteVictoryByEnemyClear();
                return;
            }

            // Batch hiện tại đã chết hết nhưng vẫn còn quái chưa spawn.
            if (aliveCount == 0)
            {
                nextBatchDelay = Mathf.Max(
                    0f,
                    context.RuntimeData.DelayBetweenBatchesSec
                );
            }
        }
        
        private void CompleteVictoryByEnemyClear()
        {
            if (!active)
                return;

            active = false;

            if (objective != null)
            {
                objective.OnDestroyed -= HandleObjectiveDestroyed;
            }

            context?.Victory?.Invoke();
        }

        public void OnTimeExpired()
        {
            if (!active)
                return;

            active = false;

            if (objective != null && !objective.IsDead)
                context.Victory?.Invoke();
            else
                context.Defeat?.Invoke();
        }

        public void Dispose()
        {
            active = false;

            if (objective != null)
            {
                objective.OnDestroyed -= HandleObjectiveDestroyed;
                objective.Stats.HealthModule.OnHPChanged += OnHpDefenseObjectChange;
            }

            objective = null;
            context = null;
        }

        private void HandleObjectiveDestroyed(DungeonDefenseObjective destroyedObjective)
        {
            if (!active)
                return;

            active = false;
            context?.Defeat?.Invoke();
        }

        private void SpawnBatch()
        {
            if (!active || context?.RuntimeData == null)
                return;

            int remaining =
                context.RuntimeData.TotalEnemyCount - spawnedCount;

            int amount = Mathf.Min(
                context.RuntimeData.EnemyPerBatch,
                remaining
            );

            if (amount <= 0)
                return;

            int enemyId = context.RuntimeData.EnemyId;
            if (enemyId <= 0)
            {
                Debug.LogError(
                    $"[DungeonDefense] Invalid EnemyId: {enemyId}"
                );
                return;
            }

            // SpawnEnemyForMode lấy vị trí tuần tự từ danh sách formation.
            // Vì vậy phải chuẩn bị đủ position trước khi spawn batch.
            context.PrepareEnemyBatchFormation?.Invoke(amount);

            for (int i = 0; i < amount; i++)
            {
                EnemyActor enemy =
                    context.SpawnEnemy?.Invoke(enemyId);

                if (enemy == null)
                    continue;

                spawnedCount++;
                aliveCount++;
            }

            nextBatchDelay = Mathf.Max(
                0f,
                context.RuntimeData.DelayBetweenBatchesSec
            );
        }
    }

    public struct DefenseDungeonDto
    {
        public string Name;
        public int Stage;
        
        public DefenseDungeonDto(string name, int stage)
        {
            Name = name;
            Stage = stage;
        }
    }
}
