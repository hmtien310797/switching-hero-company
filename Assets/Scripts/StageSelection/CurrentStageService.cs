using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    public static class CurrentStageService
    {
        private static int currentStage = 1;

        public static int CurrentStage => Mathf.Max(1, currentStage);

        public static void SetCurrentStage(int stage)
        {
            currentStage = Mathf.Max(1, stage);
        }
    }
}