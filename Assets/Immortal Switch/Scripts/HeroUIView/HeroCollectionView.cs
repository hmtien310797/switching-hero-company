using System.Collections.Generic;
using System.Linq;
using Immortal_Switch.Hero;
using Immortal_Switch.Scripts.UI;
using UnityEngine;

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

        [Header("Element Filters")] [SerializeField]
        private HeroCollectionFilterButton allElementButton;

        [SerializeField] private List<ElementFilterEntry> elementFilterButtons = new();

        [Header("Class Filters")] [SerializeField]
        private HeroCollectionFilterButton allClassButton;

        [SerializeField] private List<HeroClassFilterEntry> heroClassFilterButtons = new();

        [Header("Optional")] [SerializeField] private bool showOnlyAcquired = false;

        private readonly List<HeroCollectionItemUI> spawnedItems = new();
        private readonly List<HeroCollectionItemViewData> allItemsData = new();

        private ElementFilterMode currentElementFilter = ElementFilterMode.All;
        private HeroClassFilterMode currentHeroClassFilter = HeroClassFilterMode.All;

        private HeroCollectionItemUI currentSelectedItem;

        private void Start()
        {
            InitFilters();
            RefreshAll();
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

                var data = BuildItemData(hero, service);
                allItemsData.Add(data);
            }

            allItemsData.Sort(SortHeroData);
        }

        private HeroCollectionItemViewData BuildItemData(HeroDataSO hero, HeroProgressionService service)
        {
            var progressionConfig = heroDatabase.GetProgressionConfig(hero.Id);
            bool isAcquired = service.HasHero(hero.Id);

            HeroProgressTier displayTier = HeroProgressTier.Common;

            if (isAcquired)
            {
                var owned = service.GetOrCreateOwnedHero(hero.Id);
                displayTier = owned.CurrentTier;
            }
            else if (progressionConfig != null)
            {
                displayTier = progressionConfig.StartingTier;
            }

            var viewData = new HeroCollectionItemViewData
            {
                HeroId = hero.Id,
                HeroName = hero.Name,
                PortraitIcon = hero.PortraitIcon,
                ShardIcon = hero.ShardIcon,
                RarityIcon = heroRarityVisualConfig != null ? heroRarityVisualConfig.GetIcon(displayTier) : null,
                ElementIcon = heroUIIconConfig != null ? heroUIIconConfig.GetElementIcon(hero.Element) : null,
                HeroClassIcon = heroUIIconConfig != null ? heroUIIconConfig.GetHeroClassIcon(hero.HeroClass) : null,
                IsAcquired = isAcquired,
                SummonRarity = hero.SummonRarity,
                Element = hero.Element,
                HeroClass = hero.HeroClass,
                DisplayTier = displayTier
            };

            if (!viewData.IsAcquired)
            {
                int maxStarAtStartingTier = progressionConfig != null
                    ? progressionConfig.GetMaxStarInTier(displayTier)
                    : 0;

                viewData.CurrentStarInTier = 0;
                viewData.MaxStarInTier = maxStarAtStartingTier;
                viewData.CurrentShard = 0;
                viewData.RequiredShardToNext = 0;
                viewData.ProgressNormalized = 0f;
                viewData.IsMaxNode = false;

                return viewData;
            }

            var ownedData = service.GetOrCreateOwnedHero(hero.Id);
            var currentNode = service.GetCurrentNode(hero.Id);
            int maxStar = service.GetMaxStarInCurrentTier(hero.Id);

            viewData.CurrentStarInTier = ownedData.CurrentStarInTier;
            viewData.MaxStarInTier = maxStar;
            viewData.CurrentShard = ownedData.CurrentShard;
            viewData.IsMaxNode = currentNode == null || currentNode.IsMaxNode;

            if (currentNode == null || currentNode.IsMaxNode)
            {
                viewData.RequiredShardToNext = 0;
                viewData.ProgressNormalized = 1f;
            }
            else
            {
                viewData.RequiredShardToNext = currentNode.ShardCostToNext;
                viewData.ProgressNormalized = currentNode.ShardCostToNext <= 0
                    ? 0f
                    : Mathf.Clamp01((float)ownedData.CurrentShard / currentNode.ShardCostToNext);
            }

            return viewData;
        }

        private int SortHeroData(HeroCollectionItemViewData a, HeroCollectionItemViewData b)
        {
            if (a.IsAcquired != b.IsAcquired)
                return a.IsAcquired ? -1 : 1;

            int tierCompare = b.DisplayTier.CompareTo(a.DisplayTier);
            if (tierCompare != 0)
                return tierCompare;

            return a.HeroId.CompareTo(b.HeroId);
        }

        private void ApplyFiltersAndRebuild()
        {
            ClearSpawnedItems();

            var filtered = allItemsData.Where(PassFilter).ToList();

            for (int i = 0; i < filtered.Count; i++)
            {
                var item = Instantiate(itemPrefab, contentRoot);
                item.Bind(filtered[i]);
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