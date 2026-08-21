using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Modules.Analytics;
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
        private List<int> _pendingTutorialGuideIds = new();

        private int _currentPendingGuideId;
        private int _guideId;
        private int _currentStep;

        protected override void OnSingletonAwake()
        {
            if (UserDataCache.Instance != null)
            {
                UserDataCache.Instance.OnExpChanged += RefreshUnlock;
            }

            Storage = new TutorialStorage();
            Service = new TutorialService(Storage);

            Storage.Load();
            base.OnSingletonAwake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (UserDataCache.Instance != null)
            {
                UserDataCache.Instance.OnExpChanged -= RefreshUnlock;
            }
        }

        private void RefreshUnlock()
        {
            var featureUnlocks = DatabaseManager.Instance.GetFeatureUnlocks();
            var pendingGuideIds = new List<int>();

            foreach (var row in featureUnlocks)
            {
                if (row.tutorialStepId > 0 &&
                    !IsComplete(row.tutorialStepId) &&
                    !pendingGuideIds.Contains(row.tutorialStepId))
                {
                    pendingGuideIds.Add(row.tutorialStepId);
                }
            }

            pendingGuideIds.Sort((a, b) => a - b);
            _pendingTutorialGuideIds.Clear();

            for (int i = 1; i < pendingGuideIds.Count; i++)
            {
                _pendingTutorialGuideIds.Add(pendingGuideIds[i]);
            }

            if (pendingGuideIds.Count > 0)
            {
                TryGuide(pendingGuideIds[0]).Forget();
            }
        }

        public void ClearTutorial()
        {
            if (_currentPendingGuideId != 0)
            {
                _pendingTutorialGuideIds.Remove(_currentPendingGuideId);
            }

            Debug.Log($"ClearTutorial: {_guideId}");
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
            if (NakamaClient.Instance == null)
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

            if (guideId == TutorialGuideIds.NEW_USER_GUIDE)
            {
                AppsflyerService.TrackingCompleteRegistration(UserDataCache.Instance.AccountType);
            }

            AppsflyerService.TrackingTutorialBegin(guideId);

            _guideId = guideId;

            var tutorials = DatabaseManager.Instance.TutorialDb.GetTutorials(guideId);

            _rows.AddRange(tutorials);
            NextStep();
        }

        public void NextStep()
        {
            if (_rows.Count < 1 ||
                _rows.Count < _currentStep)
            {
                ClearTutorial();
                CheckPendingGuideId();
                return;
            }

            var step = _rows[_currentStep];
            ShowTutorial(step);
        }

        public void OnSkip()
        {
            if (_rows.Count < 1 ||
                _rows.Count < _currentStep)
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
            if (_rows.Count < 1 ||
                _rows.Count < _currentStep)
            {
                Debug.LogError($"[Tutorial] current step: {_currentStep} must smaller rows: {_rows.Count}");
                return;
            }

            var step = _rows[_currentStep];
            SyncStepCompletionAsync(step).Forget();

            if (OnClick != null)
            {
                await InvokeClickHandlersAsync(step.tutorialId, step.stepId);
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

        /// <summary>
        /// Gọi và chờ toàn bộ subscriber xử lý click hoàn tất.
        /// Multicast delegate chỉ trả task của subscriber cuối nếu gọi Invoke trực tiếp,
        /// khiến tutorial đổi step trong khi animation của subscriber trước vẫn đang chạy.
        /// </summary>
        private async UniTask InvokeClickHandlersAsync(string tutorialId, int stepId)
        {
            var handlers = OnClick?.GetInvocationList();

            if (handlers == null ||
                handlers.Length == 0)
            {
                return;
            }

            var tasks = new List<UniTask>(handlers.Length);

            foreach (var handler in handlers)
            {
                if (handler is Func<string, int, UniTask> clickHandler)
                {
                    tasks.Add(clickHandler.Invoke(tutorialId, stepId));
                }
            }

            if (tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }
        }

        private void CompleteCurrentGuide()
        {
            if (_guideId == TutorialGuideIds.END_GUIDE)
            {
                AppsflyerService.TrackingTutorialCompletion(_guideId);
            }

            Service.Complete(_guideId);
            ClearTutorial();
            CheckPendingGuideId();
        }

        private void CheckPendingGuideId()
        {
            if (_pendingTutorialGuideIds.Count > 0)
            {
                _currentPendingGuideId = _pendingTutorialGuideIds[0];

                TryGuide(_currentPendingGuideId).Forget();
            }
            else
            {
                OnCompleteTutorial?.Invoke();
            }
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