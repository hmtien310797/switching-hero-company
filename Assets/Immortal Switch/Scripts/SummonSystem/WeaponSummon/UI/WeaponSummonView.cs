using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Currency;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Views;
using Immortal_Switch.Scripts.SummonSystem.Shared.Base;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.Shared.UI;
using Immortal_Switch.Scripts.Tutorial;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonView : BaseSummonPanelView
    {
        public override SummonCategory Category => SummonCategory.Weapon;

        [Header("Buttons")] [SerializeField] private SummonButtonUI summonButtonA;
        [SerializeField] private SummonButtonUI summonButtonB;

        [Header("Texts")] [SerializeField] private TMP_Text summonLevelText;

        [Header("Progress")] [SerializeField] private Image summonLevelProgressFill;

        [Header("Reward Preview")] [SerializeField]
        private SummonLevelRewardPreviewUI levelRewardPreviewUI;

        [Header("Popup")]
        [SerializeField] private WeaponSummonProbabilityPopup probabilityPopup;
        [SerializeField] private WeaponSummonSequencePopup sequencePopup;

        [Header("Achievement")] [SerializeField]
        private SummonAchievementRewardView summonAchievementRewardView;

        [SerializeField] private Button summonAchievementButton;

        [Header("Probability")] [SerializeField]
        private Button probabilityInfoButton;

        [Header("Option Id")] [SerializeField] private string optionAId = "summon_30";
        [SerializeField] private string optionBId = "summon_50";

        private bool isBound;
        private bool isSummoning;

        private void Awake()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
        }

        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
        }

        private UniTask OnClickTutorial(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 39:
                {
                    TrySummon(optionBId);
                    break;
                }
            }

            return UniTask.CompletedTask;
        }

        private RectTransform OnResolveTarget(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 39:
                    return summonButtonB.transform as RectTransform;

                default:
                    return null;
            }
        }

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
            if (WeaponSummonManager.Instance == null ||
                WeaponSummonManager.Instance.Service == null)
                return false;

            var claimables = WeaponSummonManager.Instance.Service.GetClaimableRewardLevels();
            return claimables != null && claimables.Count > 0;
        }

        public override void RefreshView()
        {
            if (WeaponSummonManager.Instance == null ||
                WeaponSummonManager.Instance.Service == null)
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
                CurrencyLedgerService.Instance.OnAnyLedgerChanged += RefreshView;
        }

        private void UnsubscribeEvents()
        {
            if (WeaponSummonManager.Instance != null)
                WeaponSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (CurrencyManager.Instance != null)
                CurrencyLedgerService.Instance.OnAnyLedgerChanged -= RefreshView;
        }

        private void BindButtonsIfNeeded()
        {
            if (isBound)
                return;

            summonButtonA?.Init(optionAId, TrySummon, SummonCategory.Weapon);
            summonButtonB?.Init(optionBId, TrySummon, SummonCategory.Weapon);

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
            if (probabilityPopup == null ||
                WeaponSummonManager.Instance == null)
                return;

            probabilityPopup.Show(WeaponSummonManager.Instance.GetCurrentSummonLevel());
        }

        private void OpenAchievementPopup()
        {
            summonAchievementRewardView?.Show(SummonAchievementTab.Weapon);
        }

        private void TrySummon(string optionId)
        {
            if (isSummoning)
                return;

            if (WeaponSummonManager.Instance == null)
            {
                Debug.Log("[WeaponSummon] WeaponSummonManager has no instance");
                return;
            }

            if (!WeaponSummonManager.Instance.CanSummon(optionId, out var paymentType, out var paidAmount))
            {
                Debug.Log("[WeaponSummon] Not enough resource");
                return;
            }

            if (paymentType == WeaponSummonPaymentType.Gem)
            {
                bool skipConfirm = WeaponSummonManager.Instance.SaveData.SkipGemFallbackConfirm;

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
                    () => ExecuteSummonAsync(optionId).Forget()
                ))
                .Forget();
        }

        private async UniTaskVoid ExecuteSummonAsync(string optionId)
        {
            if (isSummoning)
                return;

            if (!NakamaClient.Instance.IsLoggedIn)
            {
                Debug.LogWarning("[WeaponSummon] No active session — not logged in.");
                return;
            }

            isSummoning = true;
            summonButtonA?.SetInteractable(false);
            summonButtonB?.SetInteractable(false);

            try
            {
                var response = await NakamaClient.Instance.SummonWeaponAsync(optionId);

                if (!response.Success)
                {
                    Debug.LogWarning($"[WeaponSummon] summon/execute failed: {response.Error}");
                    return;
                }

                // Cập nhật currency HUD từ server
                CurrencyManager.Instance.Set(CurrencyType.WeaponTicket, response.CurrencyBalances.WeaponTicket);
                CurrencyManager.Instance.Set(CurrencyType.diamond, response.CurrencyBalances.Diamond);

                // Sync local save data
                WeaponSummonManager.Instance.ApplyServerResponse(response);

                // Map server entries → WeaponSummonResult để drive animation
                var result = new WeaponSummonResult
                {
                    PaidAmount = response.PaidAmount,
                    OldTotalRoll = response.OldTotalRoll,
                    NewTotalRoll = response.NewTotalRoll,
                    OldSummonLevel = response.OldSummonLevel,
                    NewSummonLevel = response.NewSummonLevel,
                    NewlyUnlockedRewardLevels = response.NewlyUnlockedRewardLevels != null
                        ? new List<int>(response.NewlyUnlockedRewardLevels)
                        : new List<int>()
                };

                Enum.TryParse<WeaponSummonPaymentType>(response.PaymentType, true, out var parsedPayment);
                result.PaymentType = parsedPayment;

                foreach (var entry in response.Entries)
                {
                    Enum.TryParse<WeaponTier>(entry.Grade, true, out var tier);
                    var weaponDef = WeaponSummonManager.Instance.Config.GetWeapon(entry.WeaponId, entry.WeaponName);

                    Enum.TryParse<EItemTier>(entry.Grade, true, out var itemTier);
                    var tierInfo = DatabaseManager.Instance.ItemTierDb.Get(itemTier);

                    int totalShardAfter = WeaponManager.Instance != null
                        ? WeaponManager.Instance.ApplyStandardSummonEntry(entry.WeaponId, entry.ShardGained)
                        : 0;

                    result.Entries.Add(new WeaponSummonResultEntry
                    {
                        RollIndex = entry.RollIndex,
                        Weapon = weaponDef,
                        WeaponId = entry.WeaponId,
                        WeaponName = entry.WeaponName,
                        Icon = weaponDef != null ? weaponDef.Icon : null,
                        Tier = tier,
                        Star = entry.Star,
                        IsNewWeapon = entry.IsNew,
                        ShardGained = entry.ShardGained,
                        TotalShardAfter = totalShardAfter,
                        TierInfo = tierInfo,
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
                Debug.LogError($"[WeaponSummon] summon/execute error {ex.StatusCode}: {ex.Message}");
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