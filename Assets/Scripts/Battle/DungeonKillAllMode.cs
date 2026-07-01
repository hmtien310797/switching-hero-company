using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Enemy;
using UnityEngine;

namespace Battle.Dungeon
{
    public sealed class DungeonKillAllMode : IDungeonMode
    {
        public DungeonModeType ModeType => DungeonModeType.KillAllEnemies;

        private DungeonModeContext context;
        private int spawnedCount;
        private int killedCount;
        private int aliveCount;
        private float nextBatchDelay;
        private bool active;

        public UniTask InitializeAsync(DungeonModeContext context)
        {
            this.context = context;
            spawnedCount = 0;
            killedCount = 0;
            aliveCount = 0;
            nextBatchDelay = 0f;
            active = false;
            return UniTask.CompletedTask;
        }

        public void Begin()
        {
            active = true;
            SpawnBatch();
        }

        public void Tick(float deltaTime)
        {
            if (!active || context == null)
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

            if (killedCount >= context.RuntimeData.TotalEnemyCount)
            {
                active = false;
                context.Victory?.Invoke();
                return;
            }

            if (aliveCount == 0)
                nextBatchDelay = Mathf.Max(0f, context.RuntimeData.DelayBetweenBatchesSec);
        }

        public void OnTimeExpired()
        {
            if (!active)
                return;

            active = false;
            context.Defeat?.Invoke();
        }

        public void Dispose()
        {
            active = false;
            context = null;
        }

        private void SpawnBatch()
        {
            if (context == null)
                return;

            int remaining = context.RuntimeData.TotalEnemyCount - spawnedCount;
            int amount = Mathf.Min(context.RuntimeData.EnemyPerBatch, remaining);
            int enemyId = context.RuntimeData.EnemyId;
            
            for (int i = 0; i < amount; i++)
            {
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
