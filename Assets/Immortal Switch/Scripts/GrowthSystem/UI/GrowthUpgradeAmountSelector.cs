using System;
using System.Collections.Generic;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.GrowthSystem.UI
{
    public class GrowthUpgradeAmountSelector : MonoBehaviour
    {
        [SerializeField] private SegmentedControlOption btnX1;
        [SerializeField] private SegmentedControlOption btnX10;
        [SerializeField] private SegmentedControlOption btnX100;

        private readonly List<int> amounts = new() { 1, 10, 100 };
        private Action<int> onAmountChanged;

        public void Initialize(int currentAmount, Action<int> onChanged)
        {
            onAmountChanged = onChanged;
            btnX1.Bind(() => OnSegmentChanged(0));
            btnX10.Bind(() => OnSegmentChanged(1));
            btnX100.Bind(() => OnSegmentChanged(2));
            
            btnX1.SetSelected(currentAmount == 1);
            btnX10.SetSelected(currentAmount == 10);
            btnX100.SetSelected(currentAmount == 100);
        }

        public void SetCurrentAmount(int amount, bool notify = false)
        {
            // int index = GetIndexByAmount(amount);
            // segmentedControl.SetSelected(index, notify);
        }

        public void SetUseSlider(bool useSlider)
        {
            // segmentedControl.SetUseSliderHighlight(useSlider);
        }

        private void OnSegmentChanged(int index)
        {
            if (index < 0 ||
                index >= amounts.Count)
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