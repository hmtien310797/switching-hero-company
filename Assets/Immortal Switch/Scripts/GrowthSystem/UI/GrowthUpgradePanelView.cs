using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUpgradePanelView : MonoBehaviour
    {
        [Header("Top Info")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private GrowthUpgradeAmountSelector amountSelector;

        [Header("Stat List")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StatTierView statTierViewPrefab;

        private readonly List<StatTierView> spawnedViews = new();

        public void Bind(
            List<StatTierViewData> datas,
            int playerGold,
            int selectedUpgradeAmount,
            Action<StatType> onUpgradeClicked,
            Action<int> onUpgradeAmountChanged)
        {
            if (goldText != null)
                goldText.text = playerGold.ToString("N0");

            if (amountSelector != null)
                amountSelector.Initialize(selectedUpgradeAmount, onUpgradeAmountChanged);

            EnsureViewCount(datas.Count);

            for (int i = 0; i < spawnedViews.Count; i++)
            {
                bool active = i < datas.Count;
                spawnedViews[i].gameObject.SetActive(active);

                if (active)
                {
                    spawnedViews[i].Initialize(datas[i], onUpgradeClicked);
                }
            }
        }

        private void EnsureViewCount(int targetCount)
        {
            if (contentRoot == null || statTierViewPrefab == null)
                return;

            while (spawnedViews.Count < targetCount)
            {
                var view = Instantiate(statTierViewPrefab, contentRoot);
                spawnedViews.Add(view);
            }
        }
    }
}