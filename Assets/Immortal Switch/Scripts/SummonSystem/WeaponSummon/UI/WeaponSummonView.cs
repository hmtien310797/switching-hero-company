using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.SummonSystem.Shared.Base;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonView : BaseSummonPanelView
    {
        public override SummonCategory Category => SummonCategory.Weapon;

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
        [SerializeField] private WeaponSummonProbabilityPopup probabilityPopup;
        [SerializeField] private WeaponSummonSequencePopup sequencePopup;

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
            
        }

        protected override void OnShowPanel()
        {
            RefreshView();
        }

        protected override void OnHidePanel()
        {
            
        }

        public override bool HasNotification()
        {
            if (WeaponSummonManager.Instance == null || WeaponSummonManager.Instance.Service == null)
                return false;

            var claimables = WeaponSummonManager.Instance.Service.GetClaimableRewardLevels();
            return claimables != null && claimables.Count > 0;
        }

        public override void RefreshView()
        {
            if (WeaponSummonManager.Instance == null || WeaponSummonManager.Instance.Service == null)
                return;

            RefreshSummonLevel();
            levelRewardPreviewUI?.Refresh();
            summonButtonA?.Refresh();
            summonButtonB?.Refresh();
        }

        private void SubscribeEvents()
        {
            if (WeaponSummonManager.Instance != null)
                WeaponSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged += RefreshView;
        }

        private void UnsubscribeEvents()
        {
            if (WeaponSummonManager.Instance != null)
                WeaponSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnAnyCurrencyChanged -= RefreshView;
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
            int currentLevel = WeaponSummonManager.Instance.GetCurrentSummonLevel();

            if (summonLevelText != null)
                summonLevelText.text = $"Lv.{currentLevel}";

            int currentProgress = WeaponSummonManager.Instance.Service.GetCurrentLevelProgressRoll();
            int currentRequired = WeaponSummonManager.Instance.Service.GetCurrentLevelRequiredRoll();

            if (summonLevelProgressFill != null)
            {
                summonLevelProgressFill.fillAmount = currentRequired <= 0
                    ? 1f
                    : Mathf.Clamp01((float)currentProgress / currentRequired);
            }
        }

        private void OpenProbabilityPopup()
        {
            if (probabilityPopup == null || WeaponSummonManager.Instance == null)
                return;

            probabilityPopup.Show(WeaponSummonManager.Instance.GetCurrentSummonLevel());
        }

        private void OpenAchievementPopup()
        {
            summonAchievementRewardView?.Show(SummonAchievementTab.Weapon);
        }
        
        private void TrySummon(string optionId)
        {
            if (WeaponSummonManager.Instance == null)
                return;

            if (!WeaponSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("Not enough resource");
                return;
            }

            if (paymentType == WeaponSummonPaymentType.Gem)
            {
                bool skipConfirm = WeaponSummonManager.Instance.SaveData.SkipGemFallbackConfirm;
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
                ExecuteSummon(optionId, WeaponSummonPaymentType.Gem);
                return;
            }

            confirmPopup.Show(gemCost, () => ExecuteSummon(optionId, WeaponSummonPaymentType.Gem));
        }

        private void ExecuteSummon(string optionId, WeaponSummonPaymentType paymentType)
        {
            sequencePopup?.SetBusyReplacing(true);

            var result = WeaponSummonManager.Instance.ExecuteSummon(optionId, paymentType);

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