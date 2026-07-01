using Cysharp.Threading.Tasks;
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

        public UniTask InitializeAsync(DungeonModeContext context)
        {
            this.context = context;
            objective = null;
            spawnedCount = 0;
            killedCount = 0;
            aliveCount = 0;
            nextBatchDelay = 0f;
            active = false;
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

            objective.OnDestroyed -= HandleObjectiveDestroyed;
            objective.OnDestroyed += HandleObjectiveDestroyed;
            active = true;
            SpawnBatch();
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
            if (!active)
                return;

            aliveCount = Mathf.Max(0, aliveCount - 1);
            killedCount++;
            context.ScoreChanged?.Invoke(killedCount);

            if (aliveCount == 0)
                nextBatchDelay = Mathf.Max(0f, context.RuntimeData.DelayBetweenBatchesSec);
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
                objective.OnDestroyed -= HandleObjectiveDestroyed;

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
            if (context?.RuntimeData == null)
                return;

            int remaining = context.RuntimeData.TotalEnemyCount - spawnedCount;
            int amount = Mathf.Min(context.RuntimeData.EnemyPerBatch, remaining);
            int enemyId = context.RuntimeData.EnemyId;
            
            for (int i = 0; i < amount; i++)
            {
                if (enemyId <= 0)
                    continue;

                EnemyActor enemy = context.SpawnEnemy?.Invoke(enemyId);
                if (enemy == null)
                    continue;

                spawnedCount++;
                aliveCount++;
            }

            nextBatchDelay = Mathf.Max(0f, context.RuntimeData.DelayBetweenBatchesSec);
        }
    }
}
