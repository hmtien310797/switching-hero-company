using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public abstract class BaseSummonMainView<TPaymentType, TResult> : BaseSummonPanelView
    {
        [Header("Buttons")]
        [SerializeField] protected BaseSummonButtonUI summonButtonA;
        [SerializeField] protected BaseSummonButtonUI summonButtonB;

        [Header("Texts")]
        [SerializeField] protected TMP_Text summonLevelText;

        [Header("Progress")]
        [SerializeField] protected Image summonLevelProgressFill;

        [Header("Buttons")]
        [SerializeField] protected Button probabilityInfoButton;
        [SerializeField] protected Button summonAchievementButton;

        [Header("Option Id")]
        [SerializeField] protected string optionAId;
        [SerializeField] protected string optionBId;

        private bool isBound;

        protected virtual void OnEnable()
        {
            SubscribeEvents();
            BindButtonsIfNeeded();
            RefreshView();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeEvents();
            HideAllPopups();
        }

        protected override void OnHidePanel()
        {
            HideAllPopups();
        }

        private void BindButtonsIfNeeded()
        {
            if (isBound)
                return;

            if (summonButtonA != null)
                summonButtonA.Init(optionAId, TrySummon);

            if (summonButtonB != null)
                summonButtonB.Init(optionBId, TrySummon);

            if (probabilityInfoButton != null)
            {
                probabilityInfoButton.onClick.RemoveAllListeners();
                probabilityInfoButton.onClick.AddListener(OpenProbabilityPopup);
            }

            if (summonAchievementButton != null)
            {
                summonAchievementButton.onClick.RemoveAllListeners();
                summonAchievementButton.onClick.AddListener(OpenAchievementPopup);
            }

            isBound = true;
        }

        public override void RefreshView()
        {
            if (!CanRefresh())
                return;

            RefreshSummonLevel();
            RefreshRewardPreview();
            summonButtonA?.Refresh();
            summonButtonB?.Refresh();
        }

        private void RefreshSummonLevel()
        {
            int currentLevel = GetCurrentSummonLevel();
            int currentProgress = GetCurrentLevelProgressRoll();
            int currentRequired = GetCurrentLevelRequiredRoll();

            if (summonLevelText != null)
                summonLevelText.text = $"Lv.{currentLevel}";

            if (summonLevelProgressFill != null)
            {
                if (currentRequired <= 0)
                    summonLevelProgressFill.fillAmount = 1f;
                else
                    summonLevelProgressFill.fillAmount = Mathf.Clamp01((float)currentProgress / currentRequired);
            }
        }

        protected virtual void TrySummon(string optionId)
        {
            if (!CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("Not enough resource");
                return;
            }

            if (IsGemPayment(paymentType))
            {
                if (ShouldSkipGemConfirm())
                {
                    ExecuteSummon(optionId, paymentType);
                    return;
                }

                ShowGemConfirm(optionId, paidAmount);
                return;
            }

            ExecuteSummon(optionId, paymentType);
        }

        protected virtual void ShowGemConfirm(string optionId, int gemCost)
        {
            if (!HasConfirmPopup())
            {
                ExecuteSummon(optionId, GetGemPaymentType());
                return;
            }

            ShowConfirmPopup(gemCost, () => ExecuteSummon(optionId, GetGemPaymentType()));
        }

        protected virtual void ExecuteSummon(string optionId, TPaymentType paymentType)
        {
            SetSequenceBusy(true);

            var result = DoExecuteSummon(optionId, paymentType);

            SetSequenceBusy(false);

            if (result == null)
                return;

            if (IsSequenceShowing())
                ReplaceSequenceResult(result);
            else
                ShowSequenceFirstResult(result, TrySummonFromPopup, optionId);

            RefreshView();
        }

        protected virtual void TrySummonFromPopup(string optionId)
        {
            TrySummon(optionId);
        }

        protected abstract void SubscribeEvents();
        protected abstract void UnsubscribeEvents();

        protected abstract bool CanRefresh();

        protected abstract int GetCurrentSummonLevel();
        protected abstract int GetCurrentLevelProgressRoll();
        protected abstract int GetCurrentLevelRequiredRoll();

        protected abstract void RefreshRewardPreview();

        protected abstract bool CanSummon(string optionId, out TPaymentType paymentType, out int paidAmount);
        protected abstract TResult DoExecuteSummon(string optionId, TPaymentType paymentType);

        protected abstract bool IsGemPayment(TPaymentType paymentType);
        protected abstract TPaymentType GetGemPaymentType();
        protected abstract bool ShouldSkipGemConfirm();

        protected abstract bool HasConfirmPopup();
        protected abstract void ShowConfirmPopup(int gemCost, System.Action onConfirm);

        protected abstract void HideAllPopups();

        protected abstract void OpenProbabilityPopup();
        protected abstract void OpenAchievementPopup();

        protected abstract void SetSequenceBusy(bool value);
        protected abstract bool IsSequenceShowing();
        protected abstract void ReplaceSequenceResult(TResult result);
        protected abstract void ShowSequenceFirstResult(TResult result, System.Action<string> summonAction, string optionId);
    }
}