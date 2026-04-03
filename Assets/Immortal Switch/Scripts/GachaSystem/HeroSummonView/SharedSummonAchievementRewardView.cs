using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GachaSystem.HeroSummonView
{
    public class SharedSummonAchievementRewardView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private SharedSummonAchievementRewardItemUI itemPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<SharedSummonAchievementRewardItemUI> spawnedItems = new();

        private void Awake()
        {
            closeButton?.onClick.AddListener(Hide);
            Hide();
        }

        public void Show(List<SharedSummonAchievementItemData> items)
        {
            Rebuild(items);
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        private void Rebuild(List<SharedSummonAchievementItemData> items)
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                    Destroy(spawnedItems[i].gameObject);
            }

            spawnedItems.Clear();

            if (items == null || itemPrefab == null || itemRoot == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                var item = Instantiate(itemPrefab, itemRoot);
                item.Bind(items[i]);
                spawnedItems.Add(item);
            }
        }
    }
}