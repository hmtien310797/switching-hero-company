using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.Shared.Base;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
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
        [SerializeField] private Button summonAchievementButton;
        [SerializeField] private SummonAchievementRewardView summonAchievementRewardView;
        
        [Header("Probability")]
        [SerializeField] private Button probabilityInfoButton;

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
            
            if (summonAchievementButton != null)
            {
                summonAchievementButton.onClick.RemoveAllListeners();
                summonAchievementButton.onClick.AddListener(OpenAchievementPopup);
            }
            
            if (probabilityInfoButton != null)
            {
                probabilityInfoButton.onClick.RemoveAllListeners();
                probabilityInfoButton.onClick.AddListener(OpenProbabilityPopup);
            }

            isBound = true;
        }
        
        private void OpenProbabilityPopup()
        {
            if (probabilityPopup == null || SkillSummonManager.Instance == null)
                return;

            probabilityPopup.Show(SkillSummonManager.Instance.GetCurrentSummonLevel());
        }

        private void HideAllPopups()
        {

        }
        
        private void OpenAchievementPopup()
        {
            summonAchievementRewardView?.Show(SummonAchievementTab.Skill);
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
            {
                Debug.Log("skill summon dont have instance");
                return;
            }

            if (!SkillSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("Not enough resource");
                return;
            }

            if (paymentType == SummonPaymentType.Gem)
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
                ExecuteSummon(optionId, SummonPaymentType.Gem);
                return;
            }

            confirmPopup.Show(gemCost, () => ExecuteSummon(optionId, SummonPaymentType.Gem));
        }

        private void ExecuteSummon(string optionId, SummonPaymentType paymentType)
        {
            sequencePopup?.SetBusyReplacing(true);

            Debug.Log($"OptionId: {optionId}");
            var result = SkillSummonManager.Instance.ExecuteSummon(optionId, paymentType);

            sequencePopup?.SetBusyReplacing(false);

            if (result == null)
                return;

            Debug.Log($"Result: {JsonConvert.SerializeObject(result)} - {sequencePopup?.IsShowing}");
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