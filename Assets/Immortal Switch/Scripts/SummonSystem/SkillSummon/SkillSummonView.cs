using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.Shared.Base;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
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
        [SerializeField] private string optionAId = "summon_30";
        [SerializeField] private string optionBId = "summon_50";

        private bool isBound;
        private bool isSummoning;

        private void OnEnable()
        {
            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyLedgerService.Instance.OnAnyLedgerChanged += RefreshView;

            BindButtonsIfNeeded();
            RefreshView();
        }

        private void OnDisable()
        {
            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyLedgerService.Instance.OnAnyLedgerChanged -= RefreshView;

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

        private void TrySummon(string optionId)
        {
            if (isSummoning)
                return;

            if (SkillSummonManager.Instance == null)
            {
                Debug.Log("[SkillSummon] SkillSummonManager has no instance");
                return;
            }

            if (!SkillSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("[SkillSummon] Not enough resource");
                return;
            }

            if (paymentType == SummonPaymentType.Gem)
            {
                bool skipConfirm = SkillSummonManager.Instance.SaveData.SkipGemFallbackConfirm;
                if (skipConfirm)
                {
                    ExecuteSummonAsync(optionId).Forget();
                    return;
                }

                ShowGemConfirm(optionId, paidAmount);
                return;
            }

            ExecuteSummonAsync(optionId).Forget();
        }

        private void ShowGemConfirm(string optionId, int gemCost)
        {
            if (confirmPopup == null)
            {
                ExecuteSummonAsync(optionId).Forget();
                return;
            }

            confirmPopup.Show(gemCost, () => ExecuteSummonAsync(optionId).Forget());
        }

        private async UniTaskVoid ExecuteSummonAsync(string optionId)
        {
            if (isSummoning)
                return;

            if (!NakamaClient.Instance.IsLoggedIn)
            {
                Debug.LogWarning("[SkillSummon] No active session — not logged in.");
                return;
            }

            isSummoning = true;
            summonButtonA?.SetInteractable(false);
            summonButtonB?.SetInteractable(false);

            try
            {
                var response = await NakamaClient.Instance.SummonSkillAsync(optionId);

                if (!response.Success)
                {
                    Debug.LogWarning($"[SkillSummon] summon/execute failed: {response.Error}");
                    return;
                }

                // Cập nhật currency HUD từ server
                CurrencyManager.Instance.Set(CurrencyType.SkillTicket, response.CurrencyBalances.SkillTicket);
                CurrencyManager.Instance.Set(CurrencyType.diamond, response.CurrencyBalances.Diamond);

                // Sync local save data
                SkillSummonManager.Instance.ApplyServerResponse(response);

                // Map server entries → SkillSummonResult để drive animation
                Enum.TryParse<SummonPaymentType>(response.PaymentType, true, out var parsedPayment);
                var result = new SkillSummonResult
                {
                    PaymentType               = parsedPayment,
                    PaidAmount                = response.PaidAmount,
                    OldTotalRoll              = response.OldTotalRoll,
                    NewTotalRoll              = response.NewTotalRoll,
                    OldSummonLevel            = response.OldSummonLevel,
                    NewSummonLevel            = response.NewSummonLevel,
                    NewlyUnlockedRewardLevels = response.NewlyUnlockedRewardLevels != null
                        ? new List<int>(response.NewlyUnlockedRewardLevels)
                        : new List<int>()
                };

                foreach (var entry in response.Entries)
                {
                    var skillData = MasterDataCache.Instance.GetSkillDataById(entry.SkillId);
                    Enum.TryParse<SkillSummonGrade>(entry.Grade, true, out var grade);

                    result.Entries.Add(new SkillSummonResultEntry
                    {
                        RollIndex   = entry.RollIndex,
                        SkillAsset  = skillData,
                        SkillId     = entry.SkillId,
                        SkillName   = entry.SkillName,
                        Grade       = grade,
                        IsNewSkill  = entry.IsNew,
                        ShardGained = entry.ShardGained
                    });
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
            catch (Nakama.ApiResponseException ex)
            {
                Debug.LogError($"[SkillSummon] summon/execute error {ex.StatusCode}: {ex.Message}");
            }
            finally
            {
                isSummoning = false;
                summonButtonA?.SetInteractable(true);
                summonButtonB?.SetInteractable(true);
            }
        }

        private void TrySummonFromPopup(string optionId)
        {
            TrySummon(optionId);
        }
    }
}
