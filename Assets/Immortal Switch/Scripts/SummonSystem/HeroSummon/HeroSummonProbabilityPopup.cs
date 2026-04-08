using System.Collections.Generic;
using Immortal_Switch.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.HeroSummon
{
    public class HeroSummonProbabilityPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;

        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text summonLevelText;

        [SerializeField] private Transform sectionRoot;
        [SerializeField] private HeroSummonProbabilitySectionUI sectionPrefab;

        private SimpleUIPool<HeroSummonProbabilitySectionUI> sectionPool;

        private List<int> availableLevels = new();
        private int currentIndex;

        private void Awake()
        {
            sectionPool = new SimpleUIPool<HeroSummonProbabilitySectionUI>(sectionPrefab, sectionRoot);

            closeButton?.onClick.AddListener(Hide);
            prevButton?.onClick.AddListener(GoPrev);
            nextButton?.onClick.AddListener(GoNext);

            Hide();
        }

        public void Show(int startLevel)
        {
            BuildLevels();

            currentIndex = availableLevels.IndexOf(startLevel);
            if (currentIndex < 0) currentIndex = 0;

            SetVisible(true);
            Refresh();
        }

        private void BuildLevels()
        {
            availableLevels.Clear();

            var config = HeroSummonManager.Instance?.Config;
            if (config == null) return;

            foreach (var lv in config.SummonLevels)
                if (lv != null)
                    availableLevels.Add(lv.SummonLevel);

            availableLevels.Sort();
        }

        private void Refresh()
        {
            if (availableLevels.Count == 0) return;

            int level = availableLevels[currentIndex];
            summonLevelText.text = level.ToString();

            var data = HeroSummonProbabilityCalculator.Build(
                HeroSummonManager.Instance.Config,
                level
            );

            for (int i = 0; i < data.Sections.Count; i++)
            {
                var section = sectionPool.Get(i);
                section.Bind(data.Sections[i]);
            }

            sectionPool.ReleaseFrom(data.Sections.Count);

            prevButton.interactable = currentIndex > 0;
            nextButton.interactable = currentIndex < availableLevels.Count - 1;
        }

        private void GoPrev()
        {
            if (currentIndex <= 0) return;
            currentIndex--;
            Refresh();
        }

        private void GoNext()
        {
            if (currentIndex >= availableLevels.Count - 1) return;
            currentIndex++;
            Refresh();
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        public void Hide() => SetVisible(false);
    }
}