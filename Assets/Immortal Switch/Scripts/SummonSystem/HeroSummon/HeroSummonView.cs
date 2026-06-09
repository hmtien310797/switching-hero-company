using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.HeroUIView;
using Immortal_Switch.Scripts.SummonSystem.Shared.Base;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public class HeroSummonView : BaseSummonPanelView
    {
        public override SummonCategory Category => SummonCategory.Hero;

        [Header("Buttons")]
        [SerializeField] private SummonButtonUI summonButtonA;
        [SerializeField] private SummonButtonUI summonButtonB;

        [Header("Texts")]
        [SerializeField] private TMP_Text summonLevelText;

        [Header("Progress")]
        [SerializeField] private Image summonLevelProgressFill;

        [Header("Reward Preview")]
        [SerializeField] private SummonLevelRewardPreviewUI levelRewardPreviewUI;

        [Header("Popup")]
        [SerializeField] private SummonConfirmPopup confirmPopup;
        [SerializeField] private HeroSummonSequencePopup sequencePopup;
        [SerializeField] private HeroSummonProbabilityPopup probabilityPopup;

        [Header("Achievement")]
        [SerializeField] private SummonAchievementRewardView summonAchievementRewardView;
        [SerializeField] private Button summonAchievementButton;

        [Header("Probability")]
        [SerializeField] private Button probabilityInfoButton;

        [Header("Option Id")]
        [SerializeField] private string optionAId = "summon_10";
        [SerializeField] private string optionBId = "summon_50";

        private bool isBound;

        private void OnEnable()
        {
            SubscribeEvents();
            BindButtonsIfNeeded();
            RefreshView();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            HideAllPopups();
        }

        public override bool HasNotification()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return false;

            var claimables = HeroSummonManager.Instance.Service.GetClaimableRewardLevels();
            return claimables != null && claimables.Count > 0;
        }

        public override void RefreshView()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            RefreshSummonLevel();
            levelRewardPreviewUI?.Refresh();
            summonButtonA?.Refresh();
            summonButtonB?.Refresh();
        }

        protected override void OnShowPanel()
        {
            RefreshView();
        }

        protected override void OnHidePanel()
        {
            HideAllPopups();
        }

        private void SubscribeEvents()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyLedgerService.Instance.OnAnyLedgerChanged += RefreshView;
        }

        private void UnsubscribeEvents()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyLedgerService.Instance.OnAnyLedgerChanged  -= RefreshView;
        }

        private void BindButtonsIfNeeded()
        {
            if (isBound)
                return;

            summonButtonA?.Init(optionAId, TrySummon);
            summonButtonB?.Init(optionBId, TrySummon);

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

        private void RefreshSummonLevel()
        {
            int currentLevel = HeroSummonManager.Instance.GetCurrentSummonLevel();

            if (summonLevelText != null)
                summonLevelText.text = $"Lv.{currentLevel}";

            int currentProgress = HeroSummonManager.Instance.Service.GetCurrentLevelProgressRoll();
            int currentRequired = HeroSummonManager.Instance.Service.GetCurrentLevelRequiredRoll();

            if (summonLevelProgressFill != null)
            {
                if (currentRequired <= 0)
                    summonLevelProgressFill.fillAmount = 1f;
                else
                    summonLevelProgressFill.fillAmount = Mathf.Clamp01((float)currentProgress / currentRequired);
            }
        }

        private void OpenProbabilityPopup()
        {
            if (probabilityPopup == null || HeroSummonManager.Instance == null)
                return;

            probabilityPopup.Show(HeroSummonManager.Instance.GetCurrentSummonLevel());
        }

        private void OpenAchievementPopup()
        {
            summonAchievementRewardView?.Show(SummonAchievementTab.Heroic);
        }

        private void HideAllPopups()
        {
        }

        private void TrySummon(string optionId)
        {
            if (HeroSummonManager.Instance == null)
                return;

            if (!HeroSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("Not enough resource");
                return;
            }

            if (paymentType == SummonPaymentType.Gem)
            {
                bool skipConfirm = HeroSummonManager.Instance.SaveData.SkipGemFallbackConfirm;
                if (skipConfirm)
                {
                    ExecuteSummon(optionId, paymentType);
                    return;
                }

                ShowGemConfirm(optionId, paidAmount);
                return;
            }

            ExecuteSummon(optionId, paymentType);
        }

        private void ShowGemConfirm(string optionId, int gemCost)
        {
            if (confirmPopup == null)
            {
                ExecuteSummon(optionId, SummonPaymentType.Gem);
                return;
            }

            confirmPopup.Show(gemCost, () => ExecuteSummon(optionId, SummonPaymentType.Gem));
        }

        private void ExecuteSummon(string optionId, SummonPaymentType paymentType)
        {
            sequencePopup?.SetBusyReplacing(true);

            var result = HeroSummonManager.Instance.ExecuteSummon(optionId, paymentType);

            sequencePopup?.SetBusyReplacing(false);

            if (result == null)
                return;

            if (sequencePopup != null)
            {
                if (sequencePopup.IsShowing)
                    sequencePopup.ReplaceResult(result);
                else
                    sequencePopup.ShowFirstResult(result, TrySummonFromPopup, optionId);
            }

            RefreshView();
        }

        private void TrySummonFromPopup(string optionId)
        {
            TrySummon(optionId);
        }
    }
}