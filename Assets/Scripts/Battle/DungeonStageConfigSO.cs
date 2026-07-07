using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Battle.Dungeon
{
    [CreateAssetMenu(
        fileName = "DungeonStageConfig",
        menuName = "Immortal Switch/Dungeon/Dungeon Stage Config")]
    public sealed class DungeonStageConfigSO : ScriptableObject
    {
        [Header("Identity")]
        [Min(1)] public int dungeonId = 1;
        [Min(1)] public int level = 1;
        public DungeonModeType modeType;

        [Header("Battle")]
        [Min(1f)] public float duration = 60f;

        [Header("Kill All")]
        public int[] enemyIds;
        public float[] enemyRates;
        [Min(1)] public int totalEnemyCount = 20;
        [Min(1)] public int enemyPerBatch = 5;
        [Min(0f)] public float delayBetweenBatches = 0.5f;
        public StageStatScale enemyScale = StageStatScale.Identity;

        [Header("Boss Challenge")]
        [Min(0)] public int bossId;
        public StageStatScale bossScale = StageStatScale.Identity;
    }
}