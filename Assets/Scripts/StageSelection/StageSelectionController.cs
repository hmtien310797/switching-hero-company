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
        
        [SerializeField] private StageTowerRecyclableView towerView;

        private int selectedStage;
        private int viewCenterStage;
        private int highestUnlockedStage;
        private int currentBattleStage;
        private int viewingChapterStage;

        private StageRuntimeData selectedStageData;

        public event Action<StageRuntimeData> OnSelectedStageChanged;
        public event Action<StageRuntimeData> OnViewingChapterChanged;

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
            viewingChapterStage = selectedStage;

            StageRuntimeData runtimeData = ResolveStage(selectedStage);
            if (runtimeData == null)
                return;

            selectedStageData = runtimeData;

            RefreshTower(true);

            OnSelectedStageChanged?.Invoke(runtimeData);
            OnViewingChapterChanged?.Invoke(runtimeData);
        }

        public void SelectStage(int stage)
        {
            SelectStage(stage, force: false);
        }

        private void SelectStage(int stage, bool force)
        {
            stage = Mathf.Max(1, stage);

            if (!force && selectedStage == stage)
            {
                OnSelectedStageChanged?.Invoke(selectedStageData);
                RefreshTower(false);
                return;
            }

            StageRuntimeData runtimeData = ResolveStage(stage);
            if (runtimeData == null)
                return;

            selectedStage = stage;
            selectedStageData = runtimeData;
            viewingChapterStage = stage;

            RefreshTower(false);

            OnSelectedStageChanged?.Invoke(runtimeData);
        }
        

        public void MoveToHighestStage()
        {
            selectedStage = highestUnlockedStage;
            viewingChapterStage = selectedStage;

            StageRuntimeData runtimeData = ResolveStage(selectedStage);
            if (runtimeData == null)
                return;

            selectedStageData = runtimeData;

            RefreshTower(true);

            OnSelectedStageChanged?.Invoke(runtimeData);
            OnViewingChapterChanged?.Invoke(runtimeData);
        }

        public void SelectPreviousChapter()
        {
            StageRuntimeData viewingData = ResolveStage(viewingChapterStage);
            if (viewingData == null)
                return;

            int previousChapterLastStage = viewingData.ChapterStartStage - 1;

            if (previousChapterLastStage < 1)
                return;

            StageRuntimeData previousChapterData = ResolveStage(previousChapterLastStage);
            if (previousChapterData == null)
                return;

            viewingChapterStage = previousChapterData.ChapterStartStage;

            RefreshTower(true);

            // Chỉ update header chapter/range, không đổi selected detail.
            OnViewingChapterChanged?.Invoke(previousChapterData);
        }

        public void SelectNextChapter()
        {
            StageRuntimeData viewingData = ResolveStage(viewingChapterStage);
            if (viewingData == null)
                return;

            int nextChapterFirstStage = viewingData.ChapterEndStage + 1;

            StageRuntimeData nextChapterData = ResolveStage(nextChapterFirstStage);
            if (nextChapterData == null)
                return;

            viewingChapterStage = nextChapterData.ChapterStartStage;

            RefreshTower(true);
            
            OnViewingChapterChanged?.Invoke(nextChapterData);
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
            RefreshTower(true);
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

        private void RefreshTower(bool scrollToStageAfterBind)
        {
            StageRuntimeData viewingData = ResolveStage(viewingChapterStage);
            if (viewingData == null)
                return;

            if (towerView != null)
            {
                towerView.Bind(
                    selectedStage,
                    currentBattleStage,
                    highestUnlockedStage,
                    viewingData.ChapterStartStage,
                    viewingData.ChapterEndStage,
                    ResolveStage,
                    SelectStage,
                    scrollToStageAfterBind
                );
            }
        }
    }
}