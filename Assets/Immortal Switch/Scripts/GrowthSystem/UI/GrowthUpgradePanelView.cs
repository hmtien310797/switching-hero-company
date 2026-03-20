using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.StatSystem;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUpgradePanelView : MonoBehaviour
    {
        [Header("Top")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private GrowthUpgradeAmountSelector amountSelector;

        [Header("Tier Progress")]
        [SerializeField] private Image tierProgressBar;

        [Header("Stats")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private StatTierView prefab;

        private readonly List<StatTierView> views = new();

        public void Bind(
            GrowthUpgradePanelData panelData,
            int gold,
            int selectedAmount,
            Action<StatType> onUpgrade,
            Action<int> onAmountChanged)
        {
            if (goldText != null)
                goldText.text = gold.ToString("N0");

            if (amountSelector != null)
                amountSelector.Initialize(selectedAmount, onAmountChanged);

            if (tierProgressBar != null)
                tierProgressBar.fillAmount = panelData.TierProgressPercent;

            Ensure(panelData.Rows.Count);

            for (int i = 0; i < views.Count; i++)
            {
                bool active = i < panelData.Rows.Count;
                views[i].gameObject.SetActive(active);

                if (active)
                    views[i].Initialize(panelData.Rows[i], onUpgrade);
            }
        }

        private void Ensure(int count)
        {
            while (views.Count < count)
            {
                var v = Instantiate(prefab, contentRoot);
                views.Add(v);
            }
        }
    }
}