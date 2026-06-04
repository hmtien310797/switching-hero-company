using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    public static class StageScalingFormula
    {
        public static StageStatScale GetEnemyScale(int globalStage)
        {
            globalStage = Mathf.Max(1, globalStage);

            StageStatScale scale = new StageStatScale
            {
                HpMultiplier = 1f + (globalStage - 1) * 0.08f,
                AtkMultiplier = 1f + (globalStage - 1) * 0.05f,
                DefMultiplier = 1f + (globalStage - 1) * 0.04f
            };

            scale.Normalize();
            return scale;
        }

        public static StageStatScale GetBossScale(int globalStage)
        {
            globalStage = Mathf.Max(1, globalStage);

            StageStatScale scale = new StageStatScale
            {
                HpMultiplier = 2.5f + (globalStage - 1) * 0.12f,
                AtkMultiplier = 1.5f + (globalStage - 1) * 0.07f,
                DefMultiplier = 1.2f + (globalStage - 1) * 0.05f
            };

            scale.Normalize();
            return scale;
        }
    }
}