using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.Tutorial.Interfaces;
using Immortal_Switch.Scripts.Tutorial.Views;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.Tutorial
{
    public class TutorialManager : Singleton<TutorialManager>
    {
        /// <summary>
        /// event fire khi tutorial xuat hien va can 1 target.
        /// 1: tutorial id
        /// 2: step id
        /// result: target can tra ve
        /// </summary>
        public event Func<string, int, RectTransform> OnResolveTarget;

        /// <summary>
        /// event fire khi thay doi tutorial step
        /// </summary>
        public event Action<TutorialArgs> OnChangeStep;

        /// <summary>
        /// event fire khi tutorial click
        /// </summary>
        public event Func<string, int, UniTask> OnClick;

        /// <summary>
        /// event fire khi tutorial complete
        /// </summary>
        public event Action OnCompleteTutorial;

        private ITutorialService Service { get; set; }
        private ITutorialStorage Storage { get; set; }

        // --- Private Fields ---
        private List<DynamicHeroesGlobalSpecificationsTutConfigRow> _rows = new();
        private int _guideId;
        private int _currentStep;

        protected override void OnSingletonAwake()
        {
            Storage = new TutorialStorage();
            Service = new TutorialService(Storage);

            Storage.Load();
            base.OnSingletonAwake();
        }

        public void ClearTutorial()
        {
            _rows.Clear();
            _currentStep = 0;
            _guideId = 0;
        }

        public override UniTask InitializeAsync()
        {
            return UniTask.CompletedTask;
        }

        public bool IsComplete(int tutorialGuideId)
        {
            return Storage.Data.CompletedIds.Exists(v => v == tutorialGuideId);
        }

        /// <summary>
        /// check complete truoc khi start 1 guide. Đối soát với server trước: nếu bước
        /// cuối của guide đã có trong completed_step_ids server (vd hoàn thành ở thiết bị
        /// khác rồi cài lại app — ES3 local là fresh) nhưng local chưa đánh dấu complete,
        /// tự đánh dấu complete thay vì bắt người chơi làm lại toàn bộ tutorial.
        /// </summary>
        public async UniTask TryGuide(int guideId)
        {
            if (!IsComplete(guideId))
            {
                await ReconcileGuideFromServerAsync(guideId);
            }

            if (!IsComplete(guideId))
            {
                StartAt(guideId);
            }
        }

        private async UniTask ReconcileGuideFromServerAsync(int guideId)
        {
            if (NakamaClient.Instance == null ||
                !NakamaClient.Instance.IsLoggedIn)
                return;

            try
            {
                var state = await NakamaClient.Instance.GetTutorialStateAsync();

                if (state?.CompletedStepIds == null)
                    return;

                var tutorials = DatabaseManager.Instance.TutorialDb.GetTutorials(guideId);
                var lastStep = tutorials.LastOrDefault();

                if (lastStep != null &&
                    state.CompletedStepIds.Contains(lastStep.stepId))
                {
                    Service.Complete(guideId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Tutorial] ReconcileGuideFromServerAsync failed: {e.Message}");
            }
        }

        /// <summary>
        /// Báo server 1 step đã hoàn thành (fire-and-forget). Nếu step có reward, server trả
        /// balances tuyệt đối — clear phần ledger tạm (đã cộng optimistic trong PresentRewards)
        /// rồi áp balances thật, tránh double-count (cùng convention với RewardSyncService).
        /// </summary>
        private async UniTask SyncStepCompletionAsync(DynamicHeroesGlobalSpecificationsTutConfigRow step)
        {
            if (NakamaClient.Instance == null ||
                !NakamaClient.Instance.IsLoggedIn)
                return;

            try
            {
                var response = await NakamaClient.Instance.CompleteTutorialStepAsync(step.stepId);

                if (response == null ||
                    !response.Success)
                    return;

                if (response.Balances != null &&
                    response.Balances.Count > 0)
                {
                    CurrencyLedgerService.Instance?.ClearPendingByReason(CurrencyTransactionReason.TutorialReward);
                    CurrencyManager.Instance?.ApplyServerBalances(response.Balances);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Tutorial] SyncStepCompletionAsync failed step={step.stepId}: {e.Message}");
            }
        }

        /// <summary>
        /// start ma ko can cehck complete
        /// </summary>
        public void StartAt(int guideId)
        {
            ClearTutorial();

            _guideId = guideId;
            var tutorials = DatabaseManager.Instance.TutorialDb.GetTutorials(guideId);

            _rows.AddRange(tutorials);
            NextStep();
        }

        public void NextStep()
        {
            if (_rows.Count < _currentStep)
            {
                ClearTutorial();
                return;
            }

            var step = _rows[_currentStep];
            ShowTutorial(step);
        }

        public void OnSkip()
        {
            if (_rows.Count < _currentStep)
            {
                Debug.LogError($"[Tutorial] current step: {_currentStep} must smaller rows: {_rows.Count}");
                return;
            }

            var step = _rows[_currentStep];
            SyncStepCompletionAsync(step).Forget();

            if (step.nextStepId == 0)
            {
                CompleteCurrentGuide();
            }
            else
            {
                void ContinueTutorial()
                {
                    _currentStep++;
                    OnSkip();
                }

                var hasReward = HasRewards(step);

                if (hasReward)
                {
                    PresentRewards(step, ContinueTutorial);
                }
                else
                {
                    ContinueTutorial();
                }
            }
        }

        public async UniTask FireOnClick()
        {
            if (_rows.Count < _currentStep)
            {
                Debug.LogError($"[Tutorial] current step: {_currentStep} must smaller rows: {_rows.Count}");
                return;
            }

            var step = _rows[_currentStep];
            SyncStepCompletionAsync(step).Forget();

            if (OnClick != null)
            {
                await OnClick.Invoke(step.tutorialId, step.stepId);
            }

            if (step.nextStepId == 0)
            {
                CompleteCurrentGuide();
            }
            else
            {
                void ContinueTutorial()
                {
                    _currentStep++;
                    NextStep();
                }

                var hasReward = HasRewards(step);

                if (hasReward)
                {
                    PresentRewards(step, ContinueTutorial);
                }
                else
                {
                    ContinueTutorial();
                }
            }
        }

        private void CompleteCurrentGuide()
        {
            Service.Complete(_guideId);
            ClearTutorial();
            OnCompleteTutorial?.Invoke();
        }

        private void ShowTutorial(DynamicHeroesGlobalSpecificationsTutConfigRow row)
        {
            RectTransform target = null;

            if (row.actionType != TutorialConstants.DIALOGUE)
            {
                target = ResolveTutorialTarget(row.tutorialId, row.stepId);
            }

            var isOpening = UIManager.Instance.IsOpen<TutorialView>();

            var args = new TutorialArgs
            {
                ActionType = row.actionType,
                LocalizeKey = row.localizeKey,
                Target = target,
                NarratorId = row.narratorId,
            };

            if (isOpening)
            {
                OnChangeStep?.Invoke(args);
            }
            else
            {
                UIManager.Instance.OpenPopupAsync<TutorialView>(args, false).Forget();
            }
        }

        private RectTransform ResolveTutorialTarget(
            string tutorialId,
            int stepId
        )
        {
            return OnResolveTarget?.GetInvocationList()
                .Select(handler => handler.DynamicInvoke(tutorialId, stepId))
                .OfType<RectTransform>()
                .FirstOrDefault();
        }

        private bool HasRewards(DynamicHeroesGlobalSpecificationsTutConfigRow row)
        {
            if (string.IsNullOrWhiteSpace(row.rewardItems))
            {
                Debug.LogWarning($"[Tutorial] {row.stepId} - {row.rewardItems} is empty");
                return false;
            }

            return true;
        }

        private void PresentRewards(DynamicHeroesGlobalSpecificationsTutConfigRow row, Action onContinueTutorial)
        {
            var rewards = DatabaseManager.Instance.GetRewards(row.rewardItems);

            if (rewards == null)
            {
                Debug.LogError("[Tutorial] Rewards parse error");
                OnClosePopupReward();
                return;
            }
            
            PopupRewardService.Show(rewards, OnClosePopupReward);

            return;

            void OnClosePopupReward()
            {
                onContinueTutorial();
            }
        }
    }
}