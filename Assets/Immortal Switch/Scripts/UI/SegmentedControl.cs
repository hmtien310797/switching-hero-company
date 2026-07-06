using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immortal_Switch.Scripts.UI
{
    public class SegmentedControl : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform optionRoot;
        [SerializeField] private SegmentedControlOption optionPrefab;

        [Header("Slider Highlight")]
        [SerializeField] private RectTransform sliderHighlight;
        [SerializeField] private bool useSliderHighlight = true;
        [SerializeField] private bool hideSliderWhenDisabled = true;

        private readonly List<SegmentedControlOption> spawnedOptions = new();
        private readonly List<string> labels = new();

        private int selectedIndex = -1;
        private Action<int> onValueChanged;

        public int SelectedIndex => selectedIndex;

        public void Initialize(IReadOnlyList<string> optionLabels, int defaultIndex, Action<int> onChanged)
        {
            labels.Clear();
            for (int i = 0; i < optionLabels.Count; i++)
            {
                labels.Add(optionLabels[i]);
            }

            onValueChanged = onChanged;

            EnsureOptionCount(labels.Count);

            for (int i = 0; i < spawnedOptions.Count; i++)
            {
                bool active = i < labels.Count;
                spawnedOptions[i].gameObject.SetActive(active);

                if (active)
                {
                    spawnedOptions[i].Initialize(labels[i], i, OnOptionClicked);
                }
            }

            SetSelected(defaultIndex, false);
            RefreshVisual();
        }

        public void SetSelected(int index, bool notify = true)
        {
            if (labels.Count == 0)
                return;

            index = Mathf.Clamp(index, 0, labels.Count - 1);

            if (selectedIndex == index)
            {
                RefreshVisual();
                if (notify)
                    onValueChanged?.Invoke(selectedIndex);
                return;
            }

            selectedIndex = index;
            RefreshVisual();

            if (notify)
                onValueChanged?.Invoke(selectedIndex);
        }

        public void SetUseSliderHighlight(bool value)
        {
            useSliderHighlight = value;
            RefreshSliderVisibility();
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            for (int i = 0; i < spawnedOptions.Count; i++)
            {
                if (!spawnedOptions[i].gameObject.activeSelf)
                    continue;

                spawnedOptions[i].SetSelected(i == selectedIndex);
            }

            RefreshSliderVisibility();

            if (useSliderHighlight)
            {
                SnapSliderToSelected();
            }
        }

        private void OnOptionClicked(int index)
        {
            SetSelected(index, true);
        }

        private void EnsureOptionCount(int targetCount)
        {
            if (optionRoot == null || optionPrefab == null)
                return;

            while (spawnedOptions.Count < targetCount)
            {
                var option = Instantiate(optionPrefab, optionRoot);
                spawnedOptions.Add(option);
            }
        }

        private void RefreshSliderVisibility()
        {
            if (sliderHighlight == null)
                return;

            bool show = useSliderHighlight;

            if (!useSliderHighlight && hideSliderWhenDisabled)
                show = false;

            sliderHighlight.gameObject.SetActive(show);
        }

        private void SnapSliderToSelected()
        {
            if (sliderHighlight == null || selectedIndex < 0 || selectedIndex >= spawnedOptions.Count)
                return;

            var target = spawnedOptions[selectedIndex];
            if (target == null || !target.gameObject.activeSelf)
                return;

            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect == null)
                return;

            sliderHighlight.anchorMin = targetRect.anchorMin;
            sliderHighlight.anchorMax = targetRect.anchorMax;
            sliderHighlight.pivot = targetRect.pivot;
            sliderHighlight.anchoredPosition = targetRect.anchoredPosition;
            sliderHighlight.sizeDelta = targetRect.sizeDelta;
        }
    }
}