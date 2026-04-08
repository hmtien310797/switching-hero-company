using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroCollectionView : AnimatedUIView
    {
        [Header("References")] [SerializeField]
        private HeroProgressionDatabaseSO heroDatabase;

        [SerializeField] private HeroRarityVisualConfigSO heroRarityVisualConfig;
        [SerializeField] private HeroUIIconConfigSO heroUIIconConfig;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private HeroCollectionItemUI itemPrefab;
        [SerializeField] private Button combatFormationButton;

        [Header("Element Filters")] [SerializeField]
        private HeroCollectionFilterButton allElementButton;

        [SerializeField] private List<ElementFilterEntry> elementFilterButtons = new();

        [Header("Class Filters")] [SerializeField]
        private HeroCollectionFilterButton allClassButton;

        [SerializeField] private List<HeroClassFilterEntry> heroClassFilterButtons = new();

        [Header("Optional")] [SerializeField] private bool showOnlyAcquired = false;

        public List<HeroCollectionItemUI> spawnedItems = new();
        public List<HeroCollectionItemViewData> allItemsData = new();

        private ElementFilterMode currentElementFilter = ElementFilterMode.All;
        private HeroClassFilterMode currentHeroClassFilter = HeroClassFilterMode.All;

        private HeroCollectionItemUI currentSelectedItem;

        private void Start()
        {
            InitFilters();
            RefreshAll();
            combatFormationButton.onClick.AddListener(() => UIManager.Instance.TogglePopupAsync<HeroSwitchPopupView>());
        }

        public void RefreshAll()
        {
            BuildAllData();
            ApplyFiltersAndRebuild();
        }
        
        private void OnEnable()
        {
            if (HeroProgressionManager.Instance != null)
                HeroProgressionManager.Instance.OnHeroCollectionChanged += HandleHeroCollectionChanged;

            RefreshAll();
        }

        private void OnDisable()
        {
            if (HeroProgressionManager.Instance != null)
                HeroProgressionManager.Instance.OnHeroCollectionChanged -= HandleHeroCollectionChanged;
        }

        private void HandleHeroCollectionChanged(HeroCollectionChangedArgs args)
        {
            RefreshAll();
        }

        private void InitFilters()
        {
            if (allElementButton != null)
            {
                allElementButton.Init(() =>
                {
                    currentElementFilter = ElementFilterMode.All;
                    RefreshElementFilterVisual();
                    ApplyFiltersAndRebuild();
                });
            }

            for (int i = 0; i < elementFilterButtons.Count; i++)
            {
                var entry = elementFilterButtons[i];
                if (entry.Button == null) continue;

                var filter = entry.Filter;
                entry.Button.Init(() =>
                {
                    currentElementFilter = filter;
                    RefreshElementFilterVisual();
                    ApplyFiltersAndRebuild();
                });
            }

            if (allClassButton != null)
            {
                allClassButton.Init(() =>
                {
                    currentHeroClassFilter = HeroClassFilterMode.All;
                    RefreshClassFilterVisual();
                    ApplyFiltersAndRebuild();
                });
            }

            for (int i = 0; i < heroClassFilterButtons.Count; i++)
            {
                var entry = heroClassFilterButtons[i];
                if (entry.Button == null) continue;

                var filter = entry.Filter;
                entry.Button.Init(() =>
                {
                    currentHeroClassFilter = filter;
                    RefreshClassFilterVisual();
                    ApplyFiltersAndRebuild();
                });
            }

            RefreshElementFilterVisual();
            RefreshClassFilterVisual();
        }

        private void RefreshElementFilterVisual()
        {
            if (allElementButton != null)
                allElementButton.SetSelected(currentElementFilter == ElementFilterMode.All);

            for (int i = 0; i < elementFilterButtons.Count; i++)
            {
                var entry = elementFilterButtons[i];
                if (entry.Button != null)
                    entry.Button.SetSelected(entry.Filter == currentElementFilter);
            }
        }

        private void RefreshClassFilterVisual()
        {
            if (allClassButton != null)
                allClassButton.SetSelected(currentHeroClassFilter == HeroClassFilterMode.All);

            for (int i = 0; i < heroClassFilterButtons.Count; i++)
            {
                var entry = heroClassFilterButtons[i];
                if (entry.Button != null)
                    entry.Button.SetSelected(entry.Filter == currentHeroClassFilter);
            }
        }

        private void BuildAllData()
        {
            allItemsData.Clear();

            if (heroDatabase == null || HeroProgressionManager.Instance == null ||
                HeroProgressionManager.Instance.Service == null)
            {
                Debug.LogWarning("HeroCollectionView missing database or progression manager.");
                return;
            }

            var service = HeroProgressionManager.Instance.Service;

            for (int i = 0; i < heroDatabase.Heroes.Count; i++)
            {
                var hero = heroDatabase.Heroes[i];
                if (hero == null) continue;

                var data = HeroCollectionItemViewDataFactory.Build(
                    hero,
                    heroDatabase,
                    service,
                    heroRarityVisualConfig,
                    heroUIIconConfig);

                if (data != null)
                    allItemsData.Add(data);
            }

            allItemsData.Sort(HeroCollectionItemViewDataFactory.Sort);
        }

        private void ApplyFiltersAndRebuild()
        {
            ClearSpawnedItems();

            var filtered = allItemsData.Where(PassFilter).ToList();

            foreach (var data in filtered)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(data);
                item.SetClickCallback(OnClickHeroItem);
                item.SetButtonInteractable(true);
                item.SetDimmed(false);
                item.SetSelected(false);
                item.SetReadyHighlight(false);

                spawnedItems.Add(item);
            }
        }

        private bool PassFilter(HeroCollectionItemViewData data)
        {
            if (showOnlyAcquired && !data.IsAcquired)
                return false;

            if (currentElementFilter != ElementFilterMode.All)
            {
                if (!MatchElement(data.Element, currentElementFilter))
                    return false;
            }

            if (currentHeroClassFilter != HeroClassFilterMode.All)
            {
                if (!MatchHeroClass(data.HeroClass, currentHeroClassFilter))
                    return false;
            }

            return true;
        }
        
        private void OnClickHeroItem(HeroCollectionItemUI item)
        {
            if (item == null || item.Data == null)
                return;

            if (currentSelectedItem != null && currentSelectedItem != item)
            {
                currentSelectedItem.SetSelected(false);
                currentSelectedItem.SetReadyHighlight(false);
            }

            currentSelectedItem = item;
            currentSelectedItem.SetSelected(true);
            currentSelectedItem.SetReadyHighlight(false);

            OpenHeroInfo(item.Data.HeroId);
        }

        private void OpenHeroInfo(int heroId)
        {
            Debug.Log($"Open hero info: {heroId}");
            
        }

        private bool MatchElement(Element element, ElementFilterMode filter)
        {
            switch (filter)
            {
                case ElementFilterMode.Fire: return element == Element.Fire;
                case ElementFilterMode.Water: return element == Element.Water;
                case ElementFilterMode.Wood: return element == Element.Wood;
                case ElementFilterMode.Earth: return element == Element.Earth;
                case ElementFilterMode.Metal: return element == Element.Metal;
                default: return true;
            }
        }

        private bool MatchHeroClass(HeroClass heroClass, HeroClassFilterMode filter)
        {
            switch (filter)
            {
                case HeroClassFilterMode.Warrior: return heroClass == HeroClass.Warrior;
                case HeroClassFilterMode.Assassin: return heroClass == HeroClass.Assassin;
                case HeroClassFilterMode.Tank: return heroClass == HeroClass.Archer;
                case HeroClassFilterMode.Mage: return heroClass == HeroClass.Mage;
                default: return true;
            }
        }

        private void ClearSpawnedItems()
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                    Destroy(spawnedItems[i].gameObject);
            }

            spawnedItems.Clear();
            currentSelectedItem = null;
        }
    }

    [System.Serializable]
    public class ElementFilterEntry
    {
        public ElementFilterMode Filter;
        public HeroCollectionFilterButton Button;
    }

    [System.Serializable]
    public class HeroClassFilterEntry
    {
        public HeroClassFilterMode Filter;
        public HeroCollectionFilterButton Button;
    }

    public enum ElementFilterMode
    {
        All,
        Fire,
        Water,
        Wood,
        Earth,
        Metal
    }

    public enum HeroClassFilterMode
    {
        All,
        Warrior,
        Assassin,
        Tank,
        Mage
    }
}