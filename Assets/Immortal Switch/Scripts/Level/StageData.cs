namespace Immortal_Switch.Scripts.Level
{
    public class StageData
    {
        public int Stage { get; set; }
        public int Chapter { get; set; }
        public int[] EnemyId { get; set; } // get this collection from CreepSpawnPatternCollectionSO
        public int BossId { get; set; }
        public int TotalEnemyQuantity { get; set; } //get this from CreepQuantityPatternCollectionSO
        public int[] SpawnRatePatternId { get; set; } // get this collection from SpawnRatePatternCollectionSO
    }
}