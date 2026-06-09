using System;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEngine;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageSelectionController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private StageDataResolverSO stageDataResolver;

        [Header("Tower")]
        [SerializeField] private StageTowerListView towerListView;
        
        [SerializeField] private StageTowerRecyclableView towerView;

        private int selectedStage;
        private int viewCenterStage;
        private int highestUnlockedStage;
        private int currentBattleStage;

        private StageRuntimeData selectedStageData;

        public event Action<StageRuntimeData> OnSelectedStageChanged;

        public int SelectedStage => selectedStage;
        public int ViewCenterStage => viewCenterStage;
        public int HighestUnlockedStage => highestUnlockedStage;
        public int CurrentBattleStage => currentBattleStage;
        public StageRuntimeData SelectedStageData => selectedStageData;

        public void Initialize(int currentStage, int highestStage)
        {
            currentBattleStage = Mathf.Max(1, currentStage);
            highestUnlockedStage = Mathf.Max(1, highestStage);

            selectedStage = currentBattleStage;
            viewCenterStage = selectedStage;

            SelectStage(selectedStage, force: true);
            RefreshTower();
        }

        public void SelectStage(int stage)
        {
            SelectStage(stage, force: false);
        }

        private void SelectStage(int stage, bool force)
        {
            stage = Mathf.Max(1, stage);

            if (!force && selectedStage == stage)
                return;

            StageRuntimeData runtimeData = ResolveStage(stage);
            if (runtimeData == null)
                return;

            selectedStage = stage;
            selectedStageData = runtimeData;

            RefreshTower();

            OnSelectedStageChanged?.Invoke(runtimeData);
        }
        

        public void MoveToHighestStage()
        {
            SelectStage(highestUnlockedStage);
            viewCenterStage = highestUnlockedStage;
            RefreshTower();
        }

        public void SelectPreviousChapter()
        {
            StageRuntimeData data = ResolveStage(viewCenterStage);
            if (data == null)
                return;

            int targetStage = data.ChapterStartStage - 1;
            if (targetStage < 1)
                return;

            StageRuntimeData targetData = ResolveStage(targetStage);
            if (targetData == null)
                return;

            viewCenterStage = Mathf.Clamp(
                Mathf.Min(targetData.ChapterEndStage, highestUnlockedStage),
                targetData.ChapterStartStage,
                targetData.ChapterEndStage
            );

            RefreshTower();
        }

        public void SelectNextChapter()
        {
            StageRuntimeData data = ResolveStage(viewCenterStage);
            if (data == null)
                return;

            int targetStage = data.ChapterEndStage + 1;

            StageRuntimeData targetData = ResolveStage(targetStage);
            if (targetData == null)
                return;

            viewCenterStage = targetData.ChapterStartStage;

            RefreshTower();
        }

        public bool CanMoveToSelectedStage()
        {
            return selectedStage > 0 &&
                   selectedStage <= highestUnlockedStage &&
                   selectedStage != currentBattleStage;
        }

        public void ConfirmMoveStage()
        {
            if (!CanMoveToSelectedStage())
                return;

            currentBattleStage = selectedStage;

            // TODO:
            // Chỗ này chỉ là logic controller.
            // Sau này bắn event hoặc gọi service đổi stage thật.
            Debug.Log($"[StageSelection] Confirm move to stage {selectedStage}");
            GameEventManager.Trigger(GameEvents.OnMoveStageRequested, selectedStage);
            RefreshTower();
        }

        private StageRuntimeData ResolveStage(int stage)
        {
            if (stageDataResolver == null)
            {
                Debug.LogError("[StageSelectionController] Missing StageDataResolver.");
                return null;
            }

            return stageDataResolver.Resolve(stage);
        }

        private void RefreshTower()
        {
            StageRuntimeData selectedData = ResolveStage(selectedStage);
            if (selectedData == null)
                return;

            if (towerView != null)
            {
                towerView.Bind(
                    selectedStage,
                    currentBattleStage,
                    highestUnlockedStage,
                    selectedData.ChapterStartStage,
                    selectedData.ChapterEndStage,
                    ResolveStage,
                    SelectStage
                );
            }
        }
    }
}