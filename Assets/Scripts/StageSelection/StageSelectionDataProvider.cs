using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageSelectionDataProvider : MonoBehaviour
    {
        [SerializeField] private StageDataResolverSO stageDataResolver;

        public StageSelectionPreviewData GetPreviewData(int globalStage)
        {
            if (stageDataResolver == null)
            {
                Debug.LogError("[StageSelectionDataProvider] Missing StageDataResolverSO.");
                return null;
            }

            StageRuntimeData runtimeData = stageDataResolver.Resolve(globalStage);

            if (runtimeData == null)
            {
                Debug.LogError($"[StageSelectionDataProvider] Cannot resolve stage: {globalStage}");
                return null;
            }

            return new StageSelectionPreviewData
            {
                RuntimeData = runtimeData
            };
        }
    }
}