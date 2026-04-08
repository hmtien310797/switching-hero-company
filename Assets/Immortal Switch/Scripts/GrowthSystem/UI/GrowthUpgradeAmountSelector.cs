using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUpgradeAmountSelector : MonoBehaviour
    {
        [SerializeField] private SegmentedControl segmentedControl;

        private readonly List<int> amounts = new() { 1, 10, 100 };
        private Action<int> onAmountChanged;

        public void Initialize(int currentAmount, Action<int> onChanged)
        {
            onAmountChanged = onChanged;

            int defaultIndex = GetIndexByAmount(currentAmount);

            segmentedControl.Initialize(
                new[] { "x1", "x10", "x100" },
                defaultIndex,
                OnSegmentChanged
            );
        }

        public void SetCurrentAmount(int amount, bool notify = false)
        {
            int index = GetIndexByAmount(amount);
            segmentedControl.SetSelected(index, notify);
        }

        public void SetUseSlider(bool useSlider)
        {
            segmentedControl.SetUseSliderHighlight(useSlider);
        }

        private void OnSegmentChanged(int index)
        {
            if (index < 0 || index >= amounts.Count)
                return;

            onAmountChanged?.Invoke(amounts[index]);
        }

        private int GetIndexByAmount(int amount)
        {
            for (int i = 0; i < amounts.Count; i++)
            {
                if (amounts[i] == amount)
                    return i;
            }

            return 0;
        }
    }
}