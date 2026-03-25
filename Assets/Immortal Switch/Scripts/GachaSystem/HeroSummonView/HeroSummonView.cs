using System.Collections.Generic;
using Immortal_Switch.Scripts.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonView : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button summonButtonA;
        [SerializeField] private Button summonButtonB;

        [Header("Texts")]
        [SerializeField] private TMP_Text summonLevelText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text ticketText;
        [SerializeField] private TMP_Text gemText;
        [SerializeField] private TMP_Text rewardBadgeText;

        [Header("Progress")]
        [SerializeField] private Image summonLevelProgressFill;

        [Header("Popup")]
        [SerializeField] private HeroSummonConfirmPopup confirmPopup;
        [SerializeField] private HeroSummonResultPopup resultPopup;
        [SerializeField] private HeroSummonRewardClaimView rewardClaimView;

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
        }

        private void BindButtons()
        {
            if (summonButtonA != null)
            {
                summonButtonA.onClick.RemoveAllListeners();
                summonButtonA.onClick.AddListener(() => TrySummon(optionAId));
            }

            if (summonButtonB != null)
            {
                summonButtonB.onClick.RemoveAllListeners();
                summonButtonB.onClick.AddListener(() => TrySummon(optionBId));
            }
        }

        public void RefreshView()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            RefreshSummonLevel();
            RefreshCurrency();
            RefreshClaimableRewards();
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
                if (progressText != null)
                    progressText.text = "MAX";

                if (summonLevelProgressFill != null)
                    summonLevelProgressFill.fillAmount = 1f;

                return;
            }

            int currentProgress = totalRoll - currentRequired;
            int needed = nextRequired - currentRequired;

            if (progressText != null)
                progressText.text = $"{currentProgress}/{needed}";

            if (summonLevelProgressFill != null)
                summonLevelProgressFill.fillAmount = needed <= 0 ? 0f : Mathf.Clamp01((float)currentProgress / needed);
        }

        private void RefreshCurrency()
        {
            if (CurrencyManager.Instance == null) return;

            if (ticketText != null)
                ticketText.text = CurrencyManager.Instance.Get(CurrencyType.HeroTicket).ToString();

            if (gemText != null)
                gemText.text = CurrencyManager.Instance.Get(CurrencyType.Diamond).ToString();
        }

        private void RefreshClaimableRewards()
        {
            if (HeroSummonManager.Instance == null || HeroSummonManager.Instance.Service == null)
                return;

            List<int> claimable = HeroSummonManager.Instance.Service.GetClaimableRewardLevels();

            if (rewardBadgeText != null)
            {
                bool hasReward = claimable.Count > 0;
                rewardBadgeText.gameObject.SetActive(hasReward);

                if (hasReward)
                    rewardBadgeText.text = claimable.Count.ToString();
            }

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

            if (resultPopup != null)
                resultPopup.Show(result);

            RefreshView();
        }
    }
}