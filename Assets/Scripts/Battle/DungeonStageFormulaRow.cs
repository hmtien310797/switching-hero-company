using System;
using UnityEngine;

namespace Battle.Dungeon
{
    [Serializable]
    public sealed class DungeonStageFormulaRow
    {
        [SerializeField] private string tableKey;
        [SerializeField, Min(1)] private int stage = 1;
        [SerializeField] private int timeLimitOverrideSec;

        [SerializeField] private DungeonFormulaData recommendedPower;
        [SerializeField] private DungeonFormulaData enemyHp;
        [SerializeField] private DungeonFormulaData enemyAtk;
        [SerializeField] private DungeonFormulaData enemyDef;

        [SerializeField] private string reward1ItemId;
        [SerializeField] private DungeonFormulaData reward1;
        [SerializeField] private string reward2ItemId;
        [SerializeField] private DungeonFormulaData reward2;
        [SerializeField] private string reward3ItemId;
        [SerializeField] private DungeonFormulaData reward3;

        [SerializeField] private DungeonFormulaData enemyCount;
        [SerializeField, Min(1)] private int enemyPerBatch = 1;
        [SerializeField, Min(0f)] private float delayBetweenBatchesSec;

        public string TableKey => tableKey;
        public int Stage => stage;
        public int TimeLimitOverrideSec => timeLimitOverrideSec;
        public DungeonFormulaData RecommendedPower => recommendedPower;
        public DungeonFormulaData EnemyHp => enemyHp;
        public DungeonFormulaData EnemyAtk => enemyAtk;
        public DungeonFormulaData EnemyDef => enemyDef;
        public string Reward1ItemId => reward1ItemId;
        public DungeonFormulaData Reward1 => reward1;
        public string Reward2ItemId => reward2ItemId;
        public DungeonFormulaData Reward2 => reward2;
        public string Reward3ItemId => reward3ItemId;
        public DungeonFormulaData Reward3 => reward3;
        public DungeonFormulaData EnemyCount => enemyCount;
        public int EnemyPerBatch => enemyPerBatch;
        public float DelayBetweenBatchesSec => delayBetweenBatchesSec;
    }
}
