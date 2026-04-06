using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonView : AnimatedUIView
    {
        [Header("Buttons")]
        [SerializeField] private HeroSummonButtonUI summonButtonA;
        [SerializeField] private HeroSummonButtonUI summonButtonB;

        [Header("Texts")]
        [SerializeField] private TMP_Text summonLevelText;

        [Header("Progress")]
        [SerializeField] private Image summonLevelProgressFill;

        [Header("Reward Preview")]
        [SerializeField] private HeroSummonLevelRewardPreviewUI levelRewardPreviewUI;

        [Header("Popup")]
        [SerializeField] private HeroSummonConfirmPopup confirmPopup;
        [SerializeField] private HeroSummonSequencePopup sequencePopup;
        
        [SerializeField] private Button probabilityInfoButton;
        [SerializeField] private HeroSummonProbabilityPopup probabilityPopup;
        
        [Header("Achievement")]
        [SerializeField] private Button summonAchievementButton;
        [SerializeField] private SummonAchievementRewardView summonAchievementRewardView;

        [Header("Option Id")]
        [SerializeField] private string optionAId = "summon_30";
        [SerializeField] private string optionBId = "summon_50";

        private void OnEnable()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged += RefreshView;

            BindButtons();
            RefreshView();
        }

        private void OnDisable()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged -= RefreshView;

            if (confirmPopup != null)
                confirmPopup.Hide();

            if (sequencePopup != null)
                sequencePopup.Hide();
            
            if(probabilityInfoButton != null)
                probabilityPopup.Hide();
            
            if(summonAchievementRewardView != null)
                summonAchievementRewardView.Hide();
        }

        private void BindButtons()
        {
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
                summonAchievementButton.onClick.AddListener(OpenSummonAchievementPopup);
            }
        }
        
        private void OpenSummonAchievementPopup()
        {
            if (summonAchievementRewardView != null)
                summonAchievementRewardView.Show();
        }
        
        private void OpenProbabilityPopup()
        {
            if (probabilityPopup == null || HeroSummonManager.Instance == null)
                return;

            int currentLevel = HeroSummonManager.Instance.GetCurrentSummonLevel();
            probabilityPopup.Show(currentLevel);
        }

        public void RefreshView()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            RefreshSummonLevel();
            RefreshRewardPreview();

            if (summonButtonA != null)
                summonButtonA.Refresh();

            if (summonButtonB != null)
                summonButtonB.Refresh();
        }

        private void RefreshSummonLevel()
        {
            var manager = HeroSummonManager.Instance;
            int currentLevel = manager.GetCurrentSummonLevel();

            if (summonLevelText != null)
                summonLevelText.text = $"Lv.{currentLevel}";

            int currentProgress = manager.Service.GetCurrentLevelProgressRoll();
            int currentRequired = manager.Service.GetCurrentLevelRequiredRoll();

            if (summonLevelProgressFill != null)
            {
                if (currentRequired <= 0)
                    summonLevelProgressFill.fillAmount = 1f;
                else
                    summonLevelProgressFill.fillAmount = Mathf.Clamp01((float)currentProgress / currentRequired);
            }
        }

        private void RefreshRewardPreview()
        {
            if (levelRewardPreviewUI != null)
                levelRewardPreviewUI.Refresh();
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

            confirmPopup.Show(
                gemCost,
                () => ExecuteSummon(optionId, SummonPaymentType.Gem)
            );
        }

        private void ExecuteSummon(string optionId, SummonPaymentType paymentType)
        {
            if (sequencePopup != null)
                sequencePopup.SetBusyReplacing(true);

            var result = HeroSummonManager.Instance.ExecuteSummon(optionId, paymentType);

            if (sequencePopup != null)
                sequencePopup.SetBusyReplacing(false);

            if (result == null)
            {
                Debug.Log("Summon failed or not enough resource");
                return;
            }

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