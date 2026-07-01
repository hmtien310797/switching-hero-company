using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Boss;
using Immortal_Switch.Scripts.Enemy;

namespace Battle.Dungeon
{
    public sealed class DungeonModeContext
    {
        public DungeonStageRuntimeData RuntimeData { get; }
        public Func<int, EnemyActor> SpawnEnemy { get; }
        public Func<int, UniTask<BossActor>> SpawnBossAsync { get; }
        public Func<DungeonDamageDummy> SpawnDamageDummy { get; }
        public Func<DungeonDefenseObjective> SpawnDefenseObjective { get; }
        public Action Victory { get; }
        public Action Defeat { get; }
        public Action<long> ScoreChanged { get; }

        public DungeonModeContext(
            DungeonStageRuntimeData runtimeData,
            Func<int, EnemyActor> spawnEnemy,
            Func<int, UniTask<BossActor>> spawnBossAsync,
            Func<DungeonDamageDummy> spawnDamageDummy,
            Func<DungeonDefenseObjective> spawnDefenseObjective,
            Action victory,
            Action defeat,
            Action<long> scoreChanged)
        {
            RuntimeData = runtimeData;
            SpawnEnemy = spawnEnemy;
            SpawnBossAsync = spawnBossAsync;
            SpawnDamageDummy = spawnDamageDummy;
            SpawnDefenseObjective = spawnDefenseObjective;
            Victory = victory;
            Defeat = defeat;
            ScoreChanged = scoreChanged;
        }
    }
}
