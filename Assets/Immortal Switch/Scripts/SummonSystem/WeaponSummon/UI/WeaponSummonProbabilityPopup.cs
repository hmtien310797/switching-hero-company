using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI
{
    public class WeaponSummonProbabilityPopup : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Level")]
        [SerializeField] private TMP_Text summonLevelText;

        [Header("Tier Rows")]
        [SerializeField] private WeaponSummonProbabilityRowUI gradeDRow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeCRow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeBRow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeARow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeSRow;
        [SerializeField] private WeaponSummonProbabilityRowUI gradeSSRow;

        [Header("Star Rows")]
        [SerializeField] private WeaponSummonStarProbabilityRowUI star1Row;
        [SerializeField] private WeaponSummonStarProbabilityRowUI star2Row;
        [SerializeField] private WeaponSummonStarProbabilityRowUI star3Row;
        [SerializeField] private WeaponSummonStarProbabilityRowUI star4Row;
        [SerializeField] private WeaponSummonStarProbabilityRowUI star5Row;

        [Header("Tier Icons")]
        [SerializeField] private Sprite gradeDIcon;
        [SerializeField] private Sprite gradeCIcon;
        [SerializeField] private Sprite gradeBIcon;
        [SerializeField] private Sprite gradeAIcon;
        [SerializeField] private Sprite gradeSIcon;
        [SerializeField] private Sprite gradeSSIcon;

        private readonly List<WeaponSummonLevelEntry> cachedLevels = new();
        private int currentIndex;

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

            if (WeaponSummonManager.Instance == null || WeaponSummonManager.Instance.Config == null)
                return;

            if (WeaponSummonManager.Instance.Config.SummonLevels == null)
                return;

            cachedLevels.AddRange(
                WeaponSummonManager.Instance.Config.SummonLevels
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

            gradeDRow?.Bind(gradeDIcon, levelData.GradeDRate);
            gradeCRow?.Bind(gradeCIcon, levelData.GradeCRate);
            gradeBRow?.Bind(gradeBIcon, levelData.GradeBRate);
            gradeARow?.Bind(gradeAIcon, levelData.GradeARate);
            gradeSRow?.Bind(gradeSIcon, levelData.GradeSRate);
            gradeSSRow?.Bind(gradeSSIcon, levelData.GradeSSRate);

            star1Row?.Bind(1, levelData.Star1Rate);
            star2Row?.Bind(2, levelData.Star2Rate);
            star3Row?.Bind(3, levelData.Star3Rate);
            star4Row?.Bind(4, levelData.Star4Rate);
            star5Row?.Bind(5, levelData.Star5Rate);

            if (prevButton != null)
                prevButton.interactable = currentIndex > 0;

            if (nextButton != null)
                nextButton.interactable = currentIndex < cachedLevels.Count - 1;
        }

        private void RefreshEmpty()
        {
            if (summonLevelText != null)
                summonLevelText.text = "Lv.-";

            gradeDRow?.Bind(gradeDIcon, 0f);
            gradeCRow?.Bind(gradeCIcon, 0f);
            gradeBRow?.Bind(gradeBIcon, 0f);
            gradeARow?.Bind(gradeAIcon, 0f);
            gradeSRow?.Bind(gradeSIcon, 0f);
            gradeSSRow?.Bind(gradeSSIcon, 0f);

            star1Row?.Bind(1, 0f);
            star2Row?.Bind(2, 0f);
            star3Row?.Bind(3, 0f);
            star4Row?.Bind(4, 0f);
            star5Row?.Bind(5, 0f);

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