using System.Collections.Generic;
using Immortal_Switch.Scripts.Summon;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SummonHubView : AnimatedUIView
    {
        [Header("Panels")]
        [SerializeField] private List<BaseSummonPanelView> panels;

        [Header("Default")]
        [SerializeField] private SummonCategory defaultCategory = SummonCategory.Hero;

        [Header("Navigation Buttons")]
        [SerializeField] private Button heroButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button petButton;

        private BaseSummonPanelView currentPanel;

        private void Awake()
        {
            BindButtons();
            HideAllPanelsImmediate();
        }

        private void Start()
        {
            SwitchTo(defaultCategory);
        }

        private void BindButtons()
        {
            if (heroButton != null)
                heroButton.onClick.AddListener(() => SwitchTo(SummonCategory.Hero));

            if (skillButton != null)
                skillButton.onClick.AddListener(() => SwitchTo(SummonCategory.Skill));

            if (weaponButton != null)
                weaponButton.onClick.AddListener(() => SwitchTo(SummonCategory.Weapon));

            if (petButton != null)
                petButton.onClick.AddListener(() => SwitchTo(SummonCategory.Pet));
        }

        private void HideAllPanelsImmediate()
        {
            currentPanel = null;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null)
                    continue;

                panels[i].HidePanel();
            }
        }

        public void SwitchTo(SummonCategory category)
        {
            BaseSummonPanelView target = null;

            for (int i = 0; i < panels.Count; i++)
            {
                if (panels[i] == null)
                    continue;

                if (panels[i].Category == category)
                {
                    target = panels[i];
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
        }
    }
}