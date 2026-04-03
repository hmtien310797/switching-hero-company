using System.Collections.Generic;
using Immortal_Switch.Scripts.GachaSystem;
using Immortal_Switch.Scripts.GachaSystem.HeroSummonView;
using UnityEngine;
using Immortal_Switch.Scripts.UI;

namespace Immortal_Switch.Scripts.Summon
{
    public class SummonHubView : AnimatedUIView
    {
        [Header("Navigation")]
        [SerializeField] private SummonCategoryButtonView[] navButtons;

        [Header("Panels")]
        [SerializeField] private BaseSummonPanelView[] panelViews;

        [Header("Default")]
        [SerializeField] private SummonCategory defaultCategory = SummonCategory.Hero;

        private readonly Dictionary<SummonCategory, BaseSummonPanelView> panelLookup = new();
        private SummonCategory currentCategory;
        private bool initialized;

        private void Awake()
        {
            BuildLookup();
            BindNavButtons();
            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized)
                return;

            OpenCategory(defaultCategory);
        }

        private void BuildLookup()
        {
            panelLookup.Clear();

            if (panelViews == null)
                return;

            for (int i = 0; i < panelViews.Length; i++)
            {
                var panel = panelViews[i];
                if (panel == null)
                    continue;

                panelLookup[panel.Category] = panel;
                panel.HidePanel();
            }
        }

        private void BindNavButtons()
        {
            if (navButtons == null)
                return;

            for (int i = 0; i < navButtons.Length; i++)
            {
                var nav = navButtons[i];
                if (nav == null || nav.Button == null)
                    continue;

                var captured = nav;
                captured.Button.onClick.RemoveAllListeners();
                captured.Button.onClick.AddListener(() => OpenCategory(captured.Category));
            }
        }

        public void OpenCategory(SummonCategory category)
        {
            currentCategory = category;

            foreach (var pair in panelLookup)
            {
                if (pair.Value == null)
                    continue;

                if (pair.Key == category)
                {
                    pair.Value.ShowPanel();
                    pair.Value.RefreshView();
                }
                else
                {
                    pair.Value.HidePanel();
                }
            }

            RefreshNavVisual();
        }

        public void RefreshCurrentCategory()
        {
            if (panelLookup.TryGetValue(currentCategory, out var panel) && panel != null)
                panel.RefreshView();

            RefreshNavVisual();
        }

        public SummonCategory GetCurrentCategory()
        {
            return currentCategory;
        }

        private void RefreshNavVisual()
        {
            if (navButtons == null)
                return;

            for (int i = 0; i < navButtons.Length; i++)
            {
                var nav = navButtons[i];
                if (nav == null)
                    continue;

                nav.SetSelected(nav.Category == currentCategory);

                bool hasRedDot = false;
                if (panelLookup.TryGetValue(nav.Category, out var panel) && panel != null)
                    hasRedDot = panel.HasNotification();

                nav.SetRedDot(hasRedDot);
            }
        }
    }
}