using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Enemy;
using UnityEngine;

namespace Battle.Dungeon
{
    public sealed class DungeonModeContext
    {
        public DungeonStageRuntimeData RuntimeData { get; }

        public Func<int, EnemyActor> SpawnEnemy { get; }

        /// <summary>
        /// Chuẩn bị toàn bộ vị trí formation trước khi spawn một batch enemy.
        /// </summary>
        public Action<int> PrepareEnemyBatchFormation { get; }

        public Func<int, UniTask<BossActor>> SpawnBossAsync { get; }

        public Func<DungeonDamageDummy> SpawnDamageDummy { get; }

        public Func<DungeonDefenseObjective> SpawnDefenseObjective { get; }

        public Action Victory { get; }

        public Action Defeat { get; }

        public Action<long> ScoreChanged { get; }

        public DungeonModeContext(
            DungeonStageRuntimeData runtimeData,
            Func<int, EnemyActor> spawnEnemy,
            Action<int> prepareEnemyBatchFormation,
            Func<int, UniTask<BossActor>> spawnBossAsync,
            Func<DungeonDamageDummy> spawnDamageDummy,
            Func<DungeonDefenseObjective> spawnDefenseObjective,
            Action victory,
            Action defeat,
            Action<long> scoreChanged)
        {
            RuntimeData = runtimeData;
            SpawnEnemy = spawnEnemy;

            PrepareEnemyBatchFormation =
                prepareEnemyBatchFormation;

            SpawnBossAsync = spawnBossAsync;
            SpawnDamageDummy = spawnDamageDummy;
            SpawnDefenseObjective = spawnDefenseObjective;

            Victory = victory;
            Defeat = defeat;
            ScoreChanged = scoreChanged;
        }

        public void SetScore(long score)
        {
            ScoreChanged?.Invoke(score);
        }
    }
}