using System.Collections.Generic;
using Immortal_Switch.Scripts.SkillSummon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SkillSummonProbabilityPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;

        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text summonLevelText;

        [Header("Items")]
        [SerializeField] private SkillSummonProbabilityItemUI gradeBItem;
        [SerializeField] private SkillSummonProbabilityItemUI gradeAItem;
        [SerializeField] private SkillSummonProbabilityItemUI gradeSItem;
        [SerializeField] private SkillSummonProbabilityItemUI gradeSSItem;

        private readonly List<int> availableLevels = new();
        private int currentIndex;

        private void Awake()
        {
            closeButton?.onClick.AddListener(Hide);
            prevButton?.onClick.AddListener(GoPrev);
            nextButton?.onClick.AddListener(GoNext);
            Hide();
        }

        public void Show(int startLevel)
        {
            BuildLevels();

            currentIndex = availableLevels.IndexOf(startLevel);
            if (currentIndex < 0)
                currentIndex = 0;

            SetVisible(true);
            Refresh();
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void BuildLevels()
        {
            availableLevels.Clear();

            var config = SkillSummonManager.Instance != null ? SkillSummonManager.Instance.Config : null;
            if (config == null || config.SummonLevels == null)
                return;

            for (int i = 0; i < config.SummonLevels.Count; i++)
            {
                var entry = config.SummonLevels[i];
                if (entry == null)
                    continue;

                availableLevels.Add(entry.SummonLevel);
            }

            availableLevels.Sort();
        }

        private void Refresh()
        {
            if (availableLevels.Count == 0)
                return;

            int level = availableLevels[currentIndex];

            if (summonLevelText != null)
                summonLevelText.text = $"Lv.{level}";

            var config = SkillSummonManager.Instance != null ? SkillSummonManager.Instance.Config : null;
            if (config == null)
                return;

            var levelEntry = config.GetExactLevelEntry(level);
            if (levelEntry == null)
                return;

            gradeBItem?.Bind("B", levelEntry.GradeBRate);
            gradeAItem?.Bind("A", levelEntry.GradeARate);
            gradeSItem?.Bind("S", levelEntry.GradeSRate);
            gradeSSItem?.Bind("SS", levelEntry.GradeSSRate);

            if (prevButton != null)
                prevButton.interactable = currentIndex > 0;

            if (nextButton != null)
                nextButton.interactable = currentIndex < availableLevels.Count - 1;
        }

        private void GoPrev()
        {
            if (currentIndex <= 0)
                return;

            currentIndex--;
            Refresh();
        }

        private void GoNext()
        {
            if (currentIndex >= availableLevels.Count - 1)
                return;

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
    }
}