using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.UI;

namespace Immortal_Switch.Scripts.StageSelection
{
    public class StageSelectionView : AnimatedUIView
    {
        [Header("Controller")]
        [SerializeField] private StageSelectionController controller;
        
        [Header("Header")]
        [SerializeField] private TMP_Text chapterNameText;
        [SerializeField] private TMP_Text chapterRangeText;
        [SerializeField] private TMP_Text selectedStageText;

        [Header("Rewards")]
        [SerializeField] private StageRewardListView baseRewardListView;
        [SerializeField] private StageRewardListView clearRewardListView;

        [Header("Enemies")]
        [SerializeField] private StageEnemyListView enemyListView;

        [Header("Buttons")]
        [SerializeField] private Button moveStageButton;
        [SerializeField] private Button highestStageButton;
        [SerializeField] private Button previousChapterButton;
        [SerializeField] private Button nextChapterButton;

        private StageRuntimeData currentData;
        
        private void Awake()
        {
            if (moveStageButton != null)
                moveStageButton.onClick.AddListener(HandleMoveStageClicked);

            if (highestStageButton != null)
                highestStageButton.onClick.AddListener(HandleHighestStageClicked);

            if (previousChapterButton != null)
                previousChapterButton.onClick.AddListener(HandlePreviousChapterClicked);

            if (nextChapterButton != null)
                nextChapterButton.onClick.AddListener(HandleNextChapterClicked);
        }

        public void Bind(StageRuntimeData data)
        {
            if (data == null)
            {
                Clear();
                return;
            }

            if (chapterNameText != null)
                chapterNameText.text = $"{data.ChapterId}. {data.ChapterName}";

            if (chapterRangeText != null)
                chapterRangeText.text = $"{data.ChapterStartStage}~{data.ChapterEndStage}";

            if (selectedStageText != null)
                selectedStageText.text = $"Stage {data.GlobalStage}";

            if (baseRewardListView != null)
                baseRewardListView.Bind(data.BaseRewards);

            if (clearRewardListView != null)
                clearRewardListView.Bind(data.ClearRewards);

            if (enemyListView != null)
                enemyListView.Bind(data.EnemyIds, data.BossId);
        }
        
        public override void OnShow(object args)
        {
            base.OnShow(args);

            if (controller == null)
            {
                Debug.LogError("[StageSelectionView] Missing controller.");
                return;
            }

            StageSelectionOpenArgs openArgs = args as StageSelectionOpenArgs;

            if (openArgs == null)
            {
                Debug.LogError("[StageSelectionView] Invalid open args.");
                return;
            }

            controller.Initialize(
                currentStage: openArgs.CurrentStage,
                highestStage: openArgs.HighestUnlockedStage
            );

            Bind(controller.SelectedStageData);
            RefreshMoveStageState(controller.CanMoveToSelectedStage());
        }
        

        public void RefreshMoveStageState(bool canMove)
        {
            if (moveStageButton != null)
                moveStageButton.interactable = canMove;
        }

        private void Clear()
        {
            if (chapterNameText != null)
                chapterNameText.text = "-";

            if (chapterRangeText != null)
                chapterRangeText.text = "-";

            if (selectedStageText != null)
                selectedStageText.text = "-";

            if (baseRewardListView != null)
                baseRewardListView.Bind(null);

            if (clearRewardListView != null)
                clearRewardListView.Bind(null);

            if (enemyListView != null)
                enemyListView.Bind(null, 0);

            RefreshMoveStageState(false);
        }
        
        private void HandleMoveStageClicked()
        {
            controller?.ConfirmMoveStage();
            RefreshMoveStageState(controller != null && controller.CanMoveToSelectedStage());
        }

        private void HandleHighestStageClicked()
        {
            controller?.MoveToHighestStage();
        }

        private void HandlePreviousChapterClicked()
        {
            controller?.SelectPreviousChapter();
        }

        private void HandleNextChapterClicked()
        {
            controller?.SelectNextChapter();
        }

        private void OnDestroy()
        {
            if (moveStageButton != null)
                moveStageButton.onClick.RemoveListener(HandleMoveStageClicked);

            if (highestStageButton != null)
                highestStageButton.onClick.RemoveListener(HandleHighestStageClicked);

            if (previousChapterButton != null)
                previousChapterButton.onClick.RemoveListener(HandlePreviousChapterClicked);

            if (nextChapterButton != null)
                nextChapterButton.onClick.RemoveListener(HandleNextChapterClicked);
        }
    }
    
    [Serializable]
    public class StageSelectionOpenArgs
    {
        public int CurrentStage;
        public int HighestUnlockedStage;
    }
}