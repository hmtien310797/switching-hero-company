using System.Collections.Generic;
using Immortal_Switch.Scripts.Summon;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SummonHubView : AnimatedUIView
    {
        [Header("Panels")]
        [SerializeField] private List<BaseSummonPanelView> panels;
        [SerializeField] private List<GameObject> currencyViews;

        [Header("Default")]
        [SerializeField] private SummonCategory defaultCategory = SummonCategory.Hero;

        [Header("Navigation")]
        [SerializeField] private SegmentedControlStatic segmentedControl;
        [SerializeField] private bool useSliderHighlight = true;

        private BaseSummonPanelView currentPanel;

        private static readonly List<string> DefaultLabels = new()
        {
            "Hero",
            "Weapon",
            "Skill",
            "Pet"
        };

        private void Awake()
        {
            BindSegmentedControl();
            HideAllPanelsImmediate();
        }

        private void Start()
        {
            if (segmentedControl != null)
            {
                segmentedControl.SetSelected((int)defaultCategory, true);
            }
            else
            {
                SwitchTo(defaultCategory);
            }
        }

        private void BindSegmentedControl()
        {
            if (segmentedControl == null)
                return;

            segmentedControl.Initialize((int)defaultCategory, OnSegmentChanged);
        }

        private void OnSegmentChanged(int index)
        {
            var category = (SummonCategory)index;
            SwitchTo(category);
        }

        private void HideAllPanelsImmediate()
        {
            currentPanel = null;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] != null)
                    panels[i].HidePanel();

                if (currencyViews != null && i < currencyViews.Count && currencyViews[i] != null)
                    currencyViews[i].SetActive(false);
            }
        }

        public void SwitchTo(SummonCategory category)
        {
            BaseSummonPanelView target = null;
            int targetIndex = -1;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null)
                    continue;

                if (panels[i].Category == category)
                {
                    target = panels[i];
                    targetIndex = i;
                    break;
                }
            }

            if (target == null)
            {
                Debug.LogWarning($"No panel found for category: {category}");
                return;
            }

            if (currentPanel == target)
            {
                currentPanel.RefreshView();
                return;
            }

            HideAllPanelsImmediate();

            currentPanel = target;
            currentPanel.ShowPanel();
            currentPanel.RefreshView();

            if (currencyViews != null && targetIndex >= 0 && targetIndex < currencyViews.Count && currencyViews[targetIndex] != null)
                currencyViews[targetIndex].SetActive(true);
        }
    }
}