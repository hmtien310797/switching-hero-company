﻿using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthView : AnimatedUIView
    {
        [SerializeField] private GrowthUpgradePanelView panelView;
        [SerializeField] private GrowthStatUIViewDatabaseSO statUiDatabase;

        [Header("Tier Popup")]
        [SerializeField] private GrowthTierUpgradePopupView tierUpgradePopupView;
        [SerializeField] private Sprite[] tierIcons; 

        [Header("UI State")]
        [SerializeField] private int selectedUpgradeAmount = 1;

        private GrowthManager growthManager;
        private GrowthUpgradePanelBinder binder;
        private GrowthTierUpgradePopupBinder popupBinder;

        private void Awake()
        {
            growthManager = GrowthManager.Instance;

            binder = new GrowthUpgradePanelBinder(
                growthManager.Service,
                statUiDatabase
            );

            popupBinder = new GrowthTierUpgradePopupBinder(
                growthManager.Service,
                statUiDatabase
            );
        }

        private void OnEnable()
        {
            growthManager.OnGrowthChanged += RefreshUI;
            growthManager.OnTierReadyToUpgradePopup += HandleTierReadyPopup;
            RefreshUI();
            growthManager.CheckAndNotifyTierReady();
        }

        private void OnDisable()
        {
            growthManager.OnGrowthChanged -= RefreshUI;
            growthManager.OnTierReadyToUpgradePopup -= HandleTierReadyPopup;
        }

        public void RefreshUI()
        {
            var panelData = binder.Build(
                growthManager.PlayerGold,
                selectedUpgradeAmount
            );

            panelView.Bind(
                panelData,
                growthManager.PlayerGold,
                selectedUpgradeAmount,
                OnClickUpgradeStat,
                OnChangeUpgradeAmount
            );
        }

        private void OnChangeUpgradeAmount(int amount)
        {
            selectedUpgradeAmount = amount;
            RefreshUI();
        }

        private void OnClickUpgradeStat(StatType stat)
        {
            growthManager.TryUpgrade(stat, selectedUpgradeAmount);
        }

        private void HandleTierReadyPopup(int currentTier, int nextTier, bool isActive = true)
        {
            if (tierUpgradePopupView == null)
                return;
            
            if (!isActive)
                return;

            var currentIcon = GetIconForTier(currentTier);
            var nextIcon = GetIconForTier(nextTier);

            var popupData = popupBinder.Build(
                currentTier,
                nextTier,
                currentIcon,
                nextIcon
            );

            tierUpgradePopupView.Show(popupData, () =>
            {
                growthManager.UnlockTier(nextTier);
                tierUpgradePopupView.Hide();
                RefreshUI();
            });
        }

        private Sprite GetIconForTier(int tier)
        {
            if (tierIcons == null || tierIcons.Length == 0)
                return null;

            int iconIndex = Mathf.Max(0, (tier - 1) / 10);
            iconIndex = Mathf.Clamp(iconIndex, 0, tierIcons.Length - 1);
            return tierIcons[iconIndex];
        }
    }
}