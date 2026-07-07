using System;
using UnityEngine;

namespace Battle.Dungeon
{
    [Serializable]
    public sealed class DungeonDefinitionData
    {
        [SerializeField] private int dungeonId;
        [SerializeField] private string dungeonKey;
        [SerializeField] private string uiNameVi;
        [SerializeField] private string uiNameEn;
        [SerializeField] private DungeonModeType mode;
        [SerializeField] private int stageCount;
        [SerializeField] private string entryCostKey;
        [SerializeField] private int entryCostAmount = 1;
        [SerializeField] private string stageTableKey;
        [SerializeField] private int defaultTimeLimitSec = 60;

        [Header("Fixed dungeon content")]
        [Tooltip("Enemy used by this dungeon for every stage. Use 0 when the mode does not spawn normal enemies.")]
        [SerializeField] private int enemyId;

        [Tooltip("Boss used by this dungeon for every stage. Use 0 when the mode is not BossChallenge.")]
        [SerializeField] private int bossId;

        [Header("Addressable Map")]
        [Tooltip("Addressable prefab name under Assets/Immortal Switch/Addressable/Map/Prefab.")]
        [SerializeField] private string mapName;

        public int DungeonId => dungeonId;
        public string DungeonKey => dungeonKey;
        public string UiNameVi => uiNameVi;
        public string UiNameEn => uiNameEn;
        public DungeonModeType Mode => mode;
        public int StageCount => stageCount;
        public string EntryCostKey => entryCostKey;
        public int EntryCostAmount => entryCostAmount;
        public string StageTableKey => stageTableKey;
        public int DefaultTimeLimitSec => defaultTimeLimitSec;
        public int EnemyId => enemyId;
        public int BossId => bossId;
        public string MapName => mapName;

        public bool ContainsStage(int stage)
        {
            return stage >= 1 && (stageCount <= 0 || stage <= stageCount);
        }
    }
}
