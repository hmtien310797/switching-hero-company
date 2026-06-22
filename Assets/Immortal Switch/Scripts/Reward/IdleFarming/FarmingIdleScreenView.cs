using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Level.Stage;
using Immortal_Switch.Scripts.Reward;
using Immortal_Switch.Scripts.StageSelection;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class FarmingIdleScreenView : AnimatedUIView
    {
        [Header("Header")]
        [SerializeField] private TMP_Text stageText;
        [SerializeField] private TMP_Text chapterNameText;

        [Header("Runtime")]
        [SerializeField] private TMP_Text clockText;
        [SerializeField] private TMP_Text monstersHuntedText;
        [SerializeField] private TMP_Text battleTimeText;

        [Header("Rewards")]
        [SerializeField] private RewardItemView[] rewardItems;

        [Header("Unlock")]
        [SerializeField] private SwipeToUnlockControl swipeToUnlock;

        private StageRuntimeData stageData;
        private FarmingIdleScreenSession session;
        private bool isRunning;

        public event Action OnIdleScreenClosed;

        protected void OnEnable()
        {
            if (swipeToUnlock != null)
                swipeToUnlock.OnUnlocked += HandleUnlocked;
        }

        protected void OnDisable()
        {
            FarmingIdleScreenService.OnStageDataChanged -= HandleStageDataChanged;

            if (swipeToUnlock != null)
                swipeToUnlock.OnUnlocked -= HandleUnlocked;

            isRunning = false;
        }

        public override void OnShow(object args)
        {
            base.OnShow(args);

            FarmingIdleScreenOpenParam param = args as FarmingIdleScreenOpenParam;
            if (param == null)
            {
                Debug.LogError("[FarmingIdleScreenView] Invalid open param.");
                return;
            }

            stageData = param.StageData;
            session = param.Session;

            FarmingIdleScreenService.OnStageDataChanged -= HandleStageDataChanged;
            FarmingIdleScreenService.OnStageDataChanged += HandleStageDataChanged;

            BindStaticInfo();

            if (swipeToUnlock != null)
                swipeToUnlock.ResetState();

            isRunning = true;
            UpdateLoop().Forget();
        }
        
        private void HandleStageDataChanged(StageRuntimeData newStageData)
        {
            stageData = newStageData;
            BindStaticInfo();
        }

        private void BindStaticInfo()
        {
            if (stageData == null)
                return;

            if (stageText != null)
                stageText.text = $"{stageData.ChapterIndex + 1}-{stageData.LocalStage}";

            if (chapterNameText != null)
                chapterNameText.text = stageData.ChapterName;
        }

        private async UniTaskVoid UpdateLoop()
        {
            while (isRunning)
            {
                RefreshRuntimeInfo();
                await UniTask.Delay(TimeSpan.FromSeconds(0.25f));
            }
        }

        private void RefreshRuntimeInfo()
        {
            if (session == null)
                return;

            int elapsedSeconds = session.GetElapsedSeconds();

            if (clockText != null)
                clockText.text = DateTime.Now.ToString("hh:mm tt");

            if (battleTimeText != null)
                battleTimeText.text = FormatTime(elapsedSeconds);

            if (monstersHuntedText != null)
                monstersHuntedText.text = session.MonstersHunted.ToString();

            RefreshRewards();
        }

        private void RefreshRewards()
        {
            if (rewardItems == null)
                return;

            for (int i = 0; i < rewardItems.Length; i++)
            {
                RewardItemView item = rewardItems[i];

                if (item == null)
                    continue;

                if (stageData == null ||
                    stageData.BaseRewards == null ||
                    i >= stageData.BaseRewards.Length)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }

                StageReward reward = stageData.BaseRewards[i];

                if (reward.currencyType == CurrencyType.none)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }

                BigNumber earned = session.GetEarnedAmount(reward.currencyType);

                item.gameObject.SetActive(true);
                item.Bind(reward);
            }
        }

        private string FormatTime(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours:00}h {time.Minutes:00}m";

            return $"{time.Minutes:00}m {time.Seconds:00}s";
        }

        private void HandleUnlocked()
        {
            isRunning = false;

            OnIdleScreenClosed?.Invoke();

            PlayHideAsync().Forget();
        }
    }
    
    public class FarmingIdleScreenOpenParam
    {
        public StageRuntimeData StageData;
        public FarmingIdleScreenSession Session;
    }
}