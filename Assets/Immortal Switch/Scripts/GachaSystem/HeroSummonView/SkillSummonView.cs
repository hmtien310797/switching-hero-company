using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SkillSummon;
using Immortal_Switch.Scripts.Summon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonView : BaseSummonPanelView
    {
        public override SummonCategory Category => SummonCategory.Skill;

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
        [SerializeField] private SkillSummonSequencePopup sequencePopup;
        [SerializeField] private SkillSummonProbabilityPopup probabilityPopup;

        [Header("Achievement")]
        [SerializeField] private SummonAchievementRewardView summonAchievementRewardView;

        [Header("Option Id")]
        [SerializeField] private string optionAId = "summon_10";
        [SerializeField] private string optionBId = "summon_50";

        private bool isBound;

        private void OnEnable()
        {
            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged += RefreshView;

            BindButtonsIfNeeded();
            RefreshView();
        }

        private void OnDisable()
        {
            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged -= RefreshView;

            HideAllPopups();
        }

        protected override void OnHidePanel()
        {
            HideAllPopups();
        }

        public override bool HasNotification()
        {
            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return false;

            var claimables = SkillSummonManager.Instance.Service.GetClaimableRewardLevels();
            return claimables != null && claimables.Count > 0;
        }

        private void BindButtonsIfNeeded()
        {
            if (isBound)
                return;

            if (summonButtonA != null)
                summonButtonA.Init(optionAId, TrySummon);

            if (summonButtonB != null)
                summonButtonB.Init(optionBId, TrySummon);

            isBound = true;
        }

        private void HideAllPopups()
        {

        }

        public override void RefreshView()
        {
            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Service == null)
                return;

            RefreshSummonLevel();
            levelRewardPreviewUI?.Refresh();
            summonButtonA?.Refresh();
            summonButtonB?.Refresh();
        }

        private void RefreshSummonLevel()
        {
            int currentLevel = SkillSummonManager.Instance.GetCurrentSummonLevel();

            if (summonLevelText != null)
                summonLevelText.text = $"Lv.{currentLevel}";

            int currentProgress = SkillSummonManager.Instance.Service.GetCurrentLevelProgressRoll();
            int currentRequired = SkillSummonManager.Instance.Service.GetCurrentLevelRequiredRoll();

            if (summonLevelProgressFill != null)
            {
                if (currentRequired <= 0)
                    summonLevelProgressFill.fillAmount = 1f;
                else
                    summonLevelProgressFill.fillAmount = Mathf.Clamp01((float)currentProgress / currentRequired);
            }
        }

        // public void OpenProbabilityPopup()
        // {
        //     if (probabilityPopup == null || SkillSummonManager.Instance == null)
        //         return;
        //
        //     probabilityPopup.Show(SkillSummonManager.Instance.GetCurrentSummonLevel());
        // }
        //
        // public void OpenAchievementPopup()
        // {
        //     summonAchievementRewardView?.Show(SummonAchievementTab.Skill);
        // }

        private void TrySummon(string optionId)
        {
            if (SkillSummonManager.Instance == null)
                return;

            if (!SkillSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("Not enough resource");
                return;
            }

            if (paymentType == SkillSummonPaymentType.Gem)
            {
                bool skipConfirm = SkillSummonManager.Instance.SaveData.SkipGemFallbackConfirm;
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
                ExecuteSummon(optionId, SkillSummonPaymentType.Gem);
                return;
            }

            confirmPopup.Show(gemCost, () => ExecuteSummon(optionId, SkillSummonPaymentType.Gem));
        }

        private void ExecuteSummon(string optionId, SkillSummonPaymentType paymentType)
        {
            sequencePopup?.SetBusyReplacing(true);

            var result = SkillSummonManager.Instance.ExecuteSummon(optionId, paymentType);

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