using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.SummonSystem.Shared.Base;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public class HeroSummonView : BaseSummonPanelView
    {
        public override SummonCategory Category => SummonCategory.Hero;

        [Header("Buttons")]
        [SerializeField]
        private SummonButtonUI summonButtonA;

        [SerializeField]
        private SummonButtonUI summonButtonB;

        [Header("Texts")]
        [SerializeField]
        private TMP_Text summonLevelText;

        [Header("Progress")]
        [SerializeField]
        private Image summonLevelProgressFill;

        [Header("Reward Preview")]
        [SerializeField]
        private SummonLevelRewardPreviewUI levelRewardPreviewUI;

        [Header("Popup")]
        [SerializeField]
        private HeroSummonSequencePopup sequencePopup;

        [SerializeField]
        private HeroSummonProbabilityPopup probabilityPopup;

        [Header("Achievement")]
        [SerializeField]
        private SummonAchievementRewardView summonAchievementRewardView;

        [SerializeField]
        private Button summonAchievementButton;

        [Header("Probability")]
        [SerializeField]
        private Button probabilityInfoButton;

        [Header("Option Id")]
        [SerializeField]
        private string optionAId = "summon_30";

        [SerializeField]
        private string optionBId = "summon_50";

        private SpriteAtlas heroSpriteAtlas;
        private bool isBound;
        private bool isSummoning;

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

        public void SetHeroSpriteAtlas(SpriteAtlas spriteAtlas)
        {
            heroSpriteAtlas = spriteAtlas;
            probabilityPopup.SetHeroSpriteAtlas(spriteAtlas);
            sequencePopup.SetHeroSpriteAtlas(spriteAtlas);
        }

        public override bool HasNotification()
        {
            if (HeroSummonManager.Instance == null ||
                HeroSummonManager.Instance.Service == null)
                return false;

            var claimables = HeroSummonManager.Instance.Service.GetClaimableRewardLevels();
            return claimables != null && claimables.Count > 0;
        }

        public override void RefreshView()
        {
            if (HeroSummonManager.Instance == null ||
                HeroSummonManager.Instance.Service == null)
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
                CurrencyLedgerService.Instance.OnAnyLedgerChanged -= RefreshView;
        }

        private void BindButtonsIfNeeded()
        {
            if (isBound)
                return;

            summonButtonA?.Init(optionAId, TrySummon, SummonCategory.Hero);
            summonButtonB?.Init(optionBId, TrySummon, SummonCategory.Hero);

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
            if (probabilityPopup == null ||
                HeroSummonManager.Instance == null)
                return;

            probabilityPopup.Show(HeroSummonManager.Instance.GetCurrentSummonLevel());
        }

        private void OpenAchievementPopup()
        {
            summonAchievementRewardView?.Show(SummonAchievementTab.Heroic);
        }

        private void HideAllPopups()
        {
            sequencePopup.Hide();
            probabilityPopup.Hide();
            summonAchievementRewardView.Hide();
        }

        private void TrySummon(string optionId)
        {
            if (isSummoning)
                return;

            if (!HeroSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("[HeroSummon] Not enough resource");
                return;
            }

            if (paymentType == SummonPaymentType.Gem)
            {
                bool skipConfirm = HeroSummonManager.Instance.SaveData.SkipGemFallbackConfirm;

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
            /*if (confirmPopup == null)
            {
                ExecuteSummonAsync(optionId).Forget();
                return;
            }

            confirmPopup.Show(gemCost, () => ExecuteSummonAsync(optionId).Forget());*/
            UIManager.Instance
                .OpenPopupAsync<PopupConfirmView>(new PopupConfirmArgs(
                    "Cảnh báo",
                    $"Không đủ Vé Anh hùng.\nLần triệu hồi này sẽ tiêu tốn {gemCost} Kim cương.\nXác nhận?",
                    () => ExecuteSummonAsync(optionId).Forget(),
                    onDoNotShowAgainChanged: OnDoNotShowAgainChanged
                ))
                .Forget();
        }

        private void OnDoNotShowAgainChanged(bool value)
        {
            if (HeroSummonManager.Instance != null)
            {
                HeroSummonManager.Instance.SaveData.SkipGemFallbackConfirm = value;
                HeroSummonManager.Instance.Save();
            }
        }

        private async UniTaskVoid ExecuteSummonAsync(string optionId)
        {
            if (isSummoning)
                return;

            if (!NakamaClient.Instance.IsLoggedIn)
            {
                Debug.LogWarning("[HeroSummon] No active session — not logged in.");
                return;
            }

            isSummoning = true;
            summonButtonA?.SetInteractable(false);
            summonButtonB?.SetInteractable(false);

            try
            {
                var response = await NakamaClient.Instance.SummonHeroAsync(optionId);

                if (!response.Success)
                {
                    Debug.LogWarning($"[HeroSummon] summon/execute failed: {response.Error}");
                    return;
                }

                var option = HeroSummonManager.Instance.Service.GetOption(optionId);

                if (option != null)
                {
                    GameEventManager.Trigger(GameEvents.ON_SUMMON_HERO, option.RollCount);
                }

                // Cập nhật currency HUD từ server
                CurrencyManager.Instance.Set(CurrencyType.summon_ticket_hero, response.CurrencyBalances.HeroTicket);
                CurrencyManager.Instance.Set(CurrencyType.diamond, response.CurrencyBalances.Diamond);

                // Sync local save data
                HeroSummonManager.Instance.ApplyServerResponse(response);

                // Map server entries → HeroSummonResult để drive animation
                Enum.TryParse<SummonPaymentType>(response.PaymentType, true, out var parsedPayment);

                var result = new HeroSummonResult
                {
                    PaymentType = parsedPayment,
                    PaidAmount = response.PaidAmount,
                    OldTotalRoll = response.OldTotalRoll,
                    NewTotalRoll = response.NewTotalRoll,
                    OldSummonLevel = response.OldSummonLevel,
                    NewSummonLevel = response.NewSummonLevel,
                    NewlyUnlockedRewardLevels = response.NewlyUnlockedRewardLevels != null
                        ? new List<int>(response.NewlyUnlockedRewardLevels)
                        : new List<int>()
                };

                foreach (var entry in response.Entries)
                {
                    var heroData = DatabaseManager.Instance.GetHeroDataById(entry.HeroId);
                    Enum.TryParse<SummonRarity>(entry.Rarity, true, out var rarity);

                    result.Entries.Add(new HeroSummonResultEntry
                    {
                        HeroAsset = heroData,
                        HeroName = entry.HeroName,
                        IsNewHero = entry.IsNew,
                        ShardGained = entry.ShardGained,
                        IsPityHit = entry.IsPityHit,
                        Rarity = rarity
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
                Debug.LogError($"[HeroSummon] summon/execute error {ex.StatusCode}: {ex.Message}");
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