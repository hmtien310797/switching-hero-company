using System;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.GrowthSystem;
using Immortal_Switch.Scripts.GrowthSystem.UI;
using Immortal_Switch.Scripts.StatSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI
{
    public class GrowthView : AnimatedUIView
    {
        [SerializeField] private GrowthUpgradePanelView panelView;
        [SerializeField] private GrowthStatUIViewDatabaseSO statUiDatabase;
        [SerializeField] private GrowthDatabaseSO growthDatabase;

        [Header("Debug / Runtime")]
        [SerializeField] private int playerGold = 100000;
        [SerializeField] private int selectedUpgradeAmount = 1;
        [SerializeField] private int startUnlockedTier = 1;

        private GrowthSaveData saveData;
        private GrowthSystemService growthService;
        private GrowthUpgradePanelBinder binder;

        private void Awake()
        {
            saveData = new GrowthSaveData();
            saveData.CurrentUnlockedTier = startUnlockedTier;

            growthService = new GrowthSystemService(growthDatabase, saveData);
            binder = new GrowthUpgradePanelBinder(growthService, statUiDatabase);

            RefreshUI();
        }

        public void RefreshUI()
        {
            var datas = binder.Build(playerGold, selectedUpgradeAmount);
            panelView.Bind(
                datas,
                playerGold,
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
            int bought = growthService.Upgrade(stat, selectedUpgradeAmount, ref playerGold);
            if (bought <= 0)
                return;

            RefreshUI();
        }

        public void UnlockTier(int tier)
        {
            growthService.UnlockTier(tier);
            RefreshUI();
        }

        public void AddGold(int amount)
        {
            playerGold += amount;
            RefreshUI();
        }

        public void SetGold(int value)
        {
            playerGold = value;
            RefreshUI();
        }
    }
}