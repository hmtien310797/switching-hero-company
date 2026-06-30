using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class SegmentedControlStatic : MonoBehaviour
    {
        [Header("Prebuilt Options")]
        [SerializeField] private List<SegmentedControlOption> options;

        [Header("Highlight (optional)")]
        [SerializeField] private RectTransform sliderHighlight;
        [SerializeField] private bool useSliderHighlight = true;

        private Action<int> onValueChanged;
        private int currentIndex = -1;

        public void Initialize(int defaultIndex, Action<int> callback)
        {
            onValueChanged = callback;

            BindOptions();

            SetSelected(defaultIndex, true);
        }

        public SegmentedControlOption GetOptions(int idx)
        {
            if (idx < 0 ||
                idx >= options.Count)
            {
                return null;
            }

            return options[idx];
        }

        private void BindOptions()
        {
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;

                if (options[i] == null)
                    continue;

                options[i].Bind(() => OnOptionClicked(index));
            }
        }

        private void OnOptionClicked(int index)
        {
            SetSelected(index);
        }

        public void SetSelected(int index, bool force = false)
        {
            if (!force && currentIndex == index)
                return;

            currentIndex = index;

            UpdateVisual();

            onValueChanged?.Invoke(index);
        }

        private void UpdateVisual()
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] == null)
                    continue;

                options[i].SetSelected(i == currentIndex);
            }

            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            if (!useSliderHighlight || sliderHighlight == null)
                return;

            if (currentIndex < 0 || currentIndex >= options.Count)
                return;

            var target = options[currentIndex].GetComponent<RectTransform>();

            sliderHighlight.position = target.position;
            sliderHighlight.sizeDelta = target.sizeDelta;
        }

        public int GetCurrentIndex()
        {
            return currentIndex;
        }
    }
}