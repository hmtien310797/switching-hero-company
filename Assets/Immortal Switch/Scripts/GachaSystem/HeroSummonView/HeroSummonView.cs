using Immortal_Switch.Scripts.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonView : BaseSummonMainView<SummonPaymentType, HeroSummonResult>
    {
        public override SummonCategory Category => SummonCategory.Hero;

        [Header("Reward Preview")]
        [SerializeField] private HeroSummonLevelRewardPreviewUI levelRewardPreviewUI;

        [Header("Popup")]
        [SerializeField] private HeroSummonConfirmPopup confirmPopup;
        [SerializeField] private HeroSummonSequencePopup sequencePopup;
        [SerializeField] private HeroSummonProbabilityPopup probabilityPopup;

        [Header("Achievement")]
        [SerializeField] private SummonAchievementRewardView summonAchievementRewardView;

        public override bool HasNotification()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return false;

            var claimables = HeroSummonManager.Instance.Service.GetClaimableRewardLevels();
            return claimables != null && claimables.Count > 0;
        }

        protected override void SubscribeEvents()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged += RefreshView;
        }

        protected override void UnsubscribeEvents()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged -= RefreshView;
        }

        protected override bool CanRefresh()
        {
            return HeroSummonManager.Instance != null && HeroSummonManager.Instance.Service != null;
        }

        protected override int GetCurrentSummonLevel()
        {
            return HeroSummonManager.Instance.GetCurrentSummonLevel();
        }

        protected override int GetCurrentLevelProgressRoll()
        {
            return HeroSummonManager.Instance.Service.GetCurrentLevelProgressRoll();
        }

        protected override int GetCurrentLevelRequiredRoll()
        {
            return HeroSummonManager.Instance.Service.GetCurrentLevelRequiredRoll();
        }

        protected override void RefreshRewardPreview()
        {
            levelRewardPreviewUI?.Refresh();
        }

        protected override bool CanSummon(string optionId, out SummonPaymentType paymentType, out int paidAmount)
        {
            paymentType = SummonPaymentType.Ticket;
            paidAmount = 0;
            return HeroSummonManager.Instance != null &&
                   HeroSummonManager.Instance.CanSummon(optionId, out paymentType, out paidAmount);
        }

        protected override HeroSummonResult DoExecuteSummon(string optionId, SummonPaymentType paymentType)
        {
            return HeroSummonManager.Instance.ExecuteSummon(optionId, paymentType);
        }

        protected override bool IsGemPayment(SummonPaymentType paymentType)
        {
            return paymentType == SummonPaymentType.Gem;
        }

        protected override SummonPaymentType GetGemPaymentType()
        {
            return SummonPaymentType.Gem;
        }

        protected override bool ShouldSkipGemConfirm()
        {
            return HeroSummonManager.Instance != null &&
                   HeroSummonManager.Instance.SaveData.SkipGemFallbackConfirm;
        }

        protected override bool HasConfirmPopup()
        {
            return confirmPopup != null;
        }

        protected override void ShowConfirmPopup(int gemCost, System.Action onConfirm)
        {
            confirmPopup.Show(gemCost, onConfirm);
        }

        protected override void HideAllPopups()
        {
            confirmPopup?.Hide();
            sequencePopup?.Hide();
            probabilityPopup?.Hide();
            summonAchievementRewardView?.Hide();
        }

        protected override void OpenProbabilityPopup()
        {
            if (probabilityPopup == null || HeroSummonManager.Instance == null)
                return;

            int currentLevel = HeroSummonManager.Instance.GetCurrentSummonLevel();
            probabilityPopup.Show(currentLevel);
        }

        protected override void OpenAchievementPopup()
        {
            summonAchievementRewardView?.Show();
        }

        protected override void SetSequenceBusy(bool value)
        {
            sequencePopup?.SetBusyReplacing(value);
        }

        protected override bool IsSequenceShowing()
        {
            return sequencePopup != null && sequencePopup.IsShowing;
        }

        protected override void ReplaceSequenceResult(HeroSummonResult result)
        {
            sequencePopup?.ReplaceResult(result);
        }

        protected override void ShowSequenceFirstResult(HeroSummonResult result, System.Action<string> summonAction, string optionId)
        {
            sequencePopup?.ShowFirstResult(result, summonAction, optionId);
        }
    }
}