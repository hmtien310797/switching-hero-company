using UnityEngine;

namespace Battle.Dungeon
{
    public sealed class DungeonDataUsageExample : MonoBehaviour
    {
        [SerializeField] private DungeonDatabaseSO database;
        [SerializeField] private int dungeonId = 1;
        [SerializeField] private int stage = 1;

        [ContextMenu("Build Dungeon Runtime Data")]
        private void BuildRuntimeData()
        {
            if (!DungeonStageRuntimeBuilder.TryBuild(
                    database,
                    dungeonId,
                    stage,
                    out DungeonStageRuntimeData runtimeData))
            {
                return;
            }

            Debug.Log(
                $"[Dungeon] id={runtimeData.DungeonId}, " +
                $"stage={runtimeData.Stage}, mode={runtimeData.Mode}, " +
                $"time={runtimeData.TimeLimitSec}, " +
                $"recommendedPower={runtimeData.RecommendedPower}, " +
                $"enemyCount={runtimeData.TotalEnemyCount}, " +
                $"enemyPerBatch={runtimeData.EnemyPerBatch}"
            );

            if (runtimeData.DamageChallenge != null)
            {
                Debug.Log(
                    $"[Dungeon DPS] threshold1={runtimeData.DamageChallenge.RequiredDamage} ");
            }
        }
    }
}
