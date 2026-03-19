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
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private GrowthUpgradeAmountSelector amountSelector;

        [SerializeField] private Transform contentRoot;
        [SerializeField] private StatTierView prefab;

        private readonly List<StatTierView> views = new();

        public void Bind(
            List<StatTierViewData> datas,
            int gold,
            int selectedAmount,
            Action<StatType> onUpgrade,
            Action<int> onAmountChanged)
        {
            goldText.text = gold.ToString("N0");

            amountSelector.Initialize(selectedAmount, onAmountChanged);

            Ensure(datas.Count);

            for (int i = 0; i < views.Count; i++)
            {
                bool active = i < datas.Count;
                views[i].gameObject.SetActive(active);

                if (active)
                    views[i].Initialize(datas[i], onUpgrade);
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