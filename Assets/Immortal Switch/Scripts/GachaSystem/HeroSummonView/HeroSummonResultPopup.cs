using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class HeroSummonResultPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text paymentText;
        [SerializeField] private TMP_Text summonLevelText;
        [SerializeField] private TMP_Text rewardUnlockText;

        [Header("Result List")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private HeroSummonResultItemUI itemPrefab;

        private readonly List<HeroSummonResultItemUI> spawnedItems = new();

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            Hide();
        }

        public void Show(HeroSummonResult result)
        {
            if (result == null) return;

            ClearItems();

            if (paymentText != null)
                paymentText.text = $"{result.PaymentType} - {result.PaidAmount}";

            if (summonLevelText != null)
            {
                if (result.NewSummonLevel > result.OldSummonLevel)
                    summonLevelText.text = $"Summon Level Up: {result.OldSummonLevel} → {result.NewSummonLevel}";
                else
                    summonLevelText.text = $"Summon Level: {result.NewSummonLevel}";
            }

            if (rewardUnlockText != null)
            {
                if (result.NewlyUnlockedRewardLevels.Count > 0)
                    rewardUnlockText.text = $"Unlocked Reward: {string.Join(", ", result.NewlyUnlockedRewardLevels)}";
                else
                    rewardUnlockText.text = string.Empty;
            }

            for (int i = 0; i < result.Entries.Count; i++)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(result.Entries[i]);
                spawnedItems.Add(item);
            }

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void ClearItems()
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                    Destroy(spawnedItems[i].gameObject);
            }

            spawnedItems.Clear();
        }
    }
}