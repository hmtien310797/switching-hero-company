using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Configs.Generated;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Helper;
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
        public event Action<TutorialData> OnChangeStep;

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
        /// check complete truoc khi start 1 guide
        /// </summary>
        public void TryGuide(int guideId)
        {
            // if (!IsComplete(guideId))
            // {
            //     StartAt(guideId);
            // }
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

        public async UniTask FireOnClick()
        {
            if (_rows.Count < _currentStep)
            {
                Debug.LogError($"[Tutorial] current step: {_currentStep} must smaller rows: {_rows.Count}");
                return;
            }

            var step = _rows[_currentStep];

            if (OnClick != null)
            {
                await OnClick.Invoke(step.tutorialId, step.stepId);
            }

            if (step.nextStepId == 0)
            {
                Service.Complete(_guideId);
                ClearTutorial();
                OnCompleteTutorial?.Invoke();
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
                    PresentRewards(step, ContinueTutorial).Forget();
                }
                else
                {
                    ContinueTutorial();
                }
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

            var tutorialData = new TutorialData
            {
                ActionType = row.actionType,
                LocalizeKey = row.localizeKey,
                Target = target,
            };

            if (isOpening)
            {
                OnChangeStep?.Invoke(tutorialData);
            }
            else
            {
                UIManager.Instance.OpenPopupAsync<TutorialView>(tutorialData).Forget();
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
                Debug.LogError($"[Tutorial] {row.stepId} - {row.rewardItems} is empty");
                return false;
            }

            return true;
        }

        private async UniTask PresentRewards(DynamicHeroesGlobalSpecificationsTutConfigRow row, Action onContinueTutorial)
        {
            var rewards = await DatabaseManager.Instance.GetRewards(row.rewardItems);

            if (rewards == null)
            {
                Debug.LogError("[Tutorial] Rewards parse error");
                onContinueTutorial();
                return;
            }

            // todo: show popup rewards
            foreach (var reward in rewards)
            {
                try
                {
                    var currency = CurrencyMapper.Parse(reward.ItemKey);

                    CurrencyLedgerService.Instance.AddOrMergeIncome(
                        currency,
                        reward.Quantity,
                        CurrencyTransactionReason.TutorialReward
                    );
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            onContinueTutorial();
        }
    }
}