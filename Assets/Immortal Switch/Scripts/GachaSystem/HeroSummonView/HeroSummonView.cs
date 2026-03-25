using System.Collections.Generic;
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

        [Header("Popup")]
        [SerializeField] private HeroSummonConfirmPopup confirmPopup;
        [SerializeField] private HeroSummonRewardClaimView rewardClaimView;
        [SerializeField] private HeroSummonSequencePopup sequencePopup;

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
            
            confirmPopup.Hide();
            sequencePopup.Hide();
        }

        private void BindButtons()
        {
            if (summonButtonA != null)
                summonButtonA.Init(optionAId, TrySummon);

            if (summonButtonB != null)
                summonButtonB.Init(optionBId, TrySummon);
        }

        public void RefreshView()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            RefreshSummonLevel();
            RefreshClaimableRewards();

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

            int totalRoll = manager.SaveData.TotalRoll;
            int currentRequired = 0;
            int nextRequired = currentRequired;

            var levels = manager.Config.SummonLevels;
            for (int i = 0; i < levels.Count; i++)
            {
                var entry = levels[i];
                if (entry == null) continue;

                if (entry.SummonLevel == currentLevel)
                    currentRequired = entry.TotalRollRequired;

                if (entry.SummonLevel == currentLevel + 1)
                {
                    nextRequired = entry.TotalRollRequired;
                    break;
                }
            }

            if (nextRequired <= currentRequired)
            {
                if (summonLevelProgressFill != null)
                    summonLevelProgressFill.fillAmount = 1f;

                return;
            }

            int currentProgress = totalRoll - currentRequired;
            int needed = nextRequired - currentRequired;

            if (summonLevelProgressFill != null)
                summonLevelProgressFill.fillAmount = needed <= 0 ? 0f : Mathf.Clamp01((float)currentProgress / needed);
        }

        private void RefreshClaimableRewards()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            if (rewardClaimView != null)
                rewardClaimView.Refresh();
        }

        private void TrySummon(string optionId)
        {
            if (HeroSummonManager.Instance == null)
                return;

            if (!HeroSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("Not enough currency to summon");
                return;
            }

            if (paymentType == SummonPaymentType.Gem)
            {
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
                () => ExecuteSummon(optionId, SummonPaymentType.Gem));
        }

        private void ExecuteSummon(string optionId, SummonPaymentType paymentType)
        {
            var result = HeroSummonManager.Instance.ExecuteSummon(optionId, paymentType);
            if (result == null)
                return;

            if (sequencePopup != null)
            {
                sequencePopup.Show(result, () =>
                {
                    if (HeroSummonManager.Instance.CanSummon(optionId, out var nextPaymentType, out _))
                    {
                        if (nextPaymentType == SummonPaymentType.Gem)
                        {
                            ShowGemConfirm(optionId, HeroSummonManager.Instance.Service.GetOption(optionId).GemCost);
                            return;
                        }

                        ExecuteSummon(optionId, nextPaymentType);
                    }
                    else
                    {
                        // TODO: Show Not Enough Resource Popup
                        Debug.Log("Not enough resource for auto summon");
                    }
                });
            }

            RefreshView();
        }

    }
}