using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.SkillSummon
{
    public class SkillSummonProbabilityPopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Level")]
        [SerializeField] private TMP_Text summonLevelText;

        [Header("Rows")]
        [SerializeField] private WeaponSummonProbabilityRowUI gradeBRow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeARow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeSRow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeSSRow;
        
        private readonly List<SkillSummonLevelEntry> cachedLevels = new();
        private int currentIndex = 0;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            if (prevButton != null)
            {
                prevButton.onClick.RemoveAllListeners();
                prevButton.onClick.AddListener(ShowPrevLevel);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(ShowNextLevel);
            }

            Hide();
        }

        public void Show(int currentSummonLevel)
        {
            BuildCachedLevels();

            if (cachedLevels.Count == 0)
            {
                Debug.LogWarning("SkillSummonProbabilityPopup: No summon levels found.");
                SetVisible(true);
                RefreshEmpty();
                return;
            }

            currentIndex = FindNearestIndex(currentSummonLevel);
            SetVisible(true);
            RefreshView();
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void BuildCachedLevels()
        {
            cachedLevels.Clear();

            if (SkillSummonManager.Instance == null || SkillSummonManager.Instance.Config == null)
                return;

            if (SkillSummonManager.Instance.Config.SummonLevels == null)
                return;

            cachedLevels.AddRange(
                SkillSummonManager.Instance.Config.SummonLevels
                    .Where(x => x != null)
                    .OrderBy(x => x.SummonLevel)
            );
        }

        private int FindNearestIndex(int summonLevel)
        {
            if (cachedLevels.Count == 0)
                return 0;

            for (int i = 0; i < cachedLevels.Count; i++)
            {
                if (cachedLevels[i].SummonLevel == summonLevel)
                    return i;
            }

            int nearestIndex = 0;
            int nearestDistance = Mathf.Abs(cachedLevels[0].SummonLevel - summonLevel);

            for (int i = 1; i < cachedLevels.Count; i++)
            {
                int distance = Mathf.Abs(cachedLevels[i].SummonLevel - summonLevel);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private void ShowPrevLevel()
        {
            if (cachedLevels.Count == 0)
                return;

            currentIndex = Mathf.Max(0, currentIndex - 1);
            RefreshView();
        }

        private void ShowNextLevel()
        {
            if (cachedLevels.Count == 0)
                return;

            currentIndex = Mathf.Min(cachedLevels.Count - 1, currentIndex + 1);
            RefreshView();
        }

        private void RefreshView()
        {
            if (cachedLevels.Count == 0)
            {
                RefreshEmpty();
                return;
            }

            var levelData = cachedLevels[currentIndex];

            if (summonLevelText != null)
                summonLevelText.text = $"Lv.{levelData.SummonLevel}";

            gradeBRow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.UnCommon), levelData.GradeBRate);
            gradeARow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.Common), levelData.GradeARate);
            gradeSRow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.Legendary), levelData.GradeSRate);
            gradeSSRow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.Mythic) , levelData.GradeSSRate);

            if (prevButton != null)
                prevButton.interactable = currentIndex > 0;

            if (nextButton != null)
                nextButton.interactable = currentIndex < cachedLevels.Count - 1;
        }

        private void RefreshEmpty()
        {
            if (summonLevelText != null)
                summonLevelText.text = "Lv.-";

            gradeBRow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.UnCommon) , 0f);
            gradeARow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.Common), 0f);
            gradeSRow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.Legendary), 0f);
            gradeSSRow?.Bind(HeroImageService.GetHeroTierIcon(HeroProgressTier.Mythic), 0f);

            if (prevButton != null)
                prevButton.interactable = false;

            if (nextButton != null)
                nextButton.interactable = false;
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