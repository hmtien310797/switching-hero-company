using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroCollectionView : AnimatedUIView
    {
        [Header("References")] [SerializeField]
        private HeroProgressionDatabaseSO heroDatabase;

        [SerializeField] private HeroRarityVisualConfigSO heroRarityVisualConfig;
        [SerializeField] private HeroSummonRarityVisualConfigSO heroSummonRarityVisualConfigSo;
        [SerializeField] private HeroUIIconConfigSO heroUIIconConfig;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private HeroCollectionItemUI itemPrefab;
        [SerializeField] private Button combatFormationButton;
        [SerializeField] private Button upgradeAllHeroButton;

        [Header("Element Filters")] [SerializeField]
        private HeroCollectionFilterButton allElementButton;

        [SerializeField] private List<ElementFilterEntry> elementFilterButtons = new();

        [Header("Class Filters")] [SerializeField]
        private HeroCollectionFilterButton allClassButton;

        [SerializeField] private List<HeroClassFilterEntry> heroClassFilterButtons = new();

        [Header("Optional")] [SerializeField] private bool showOnlyAcquired = false;

        public List<HeroCollectionItemUI> spawnedItems = new();
        [ShowInInspector]
        public List<HeroCollectionItemViewData> allItemsData = new();

        private ElementFilterMode currentElementFilter = ElementFilterMode.All;
        private HeroClassFilterMode currentHeroClassFilter = HeroClassFilterMode.All;

        private HeroCollectionItemUI currentSelectedItem;
        private List<HeroDataSO> allHeroData;
        private SpriteAtlas heroSpriteAlas;
        private const string HeroSpriteAtlasKey = "hero_sprite_atlas";
        private readonly HashSet<int> lineupHeroIds = new();

        private void Awake()
        {
            allHeroData = MasterDataCache.Instance.GetAllHeroData();
        }
        
        private void Start()
        {
            InitFilters();
            combatFormationButton.onClick.AddListener(() =>
            {
                UIManager.Instance.TogglePopupAsync<HeroSwitchPopupView>(heroSpriteAlas).Forget();
            });
            upgradeAllHeroButton.onClick.AddListener(HeroProgressionManager.Instance.UpgradeAllHeroes);
        }

        private void RefreshAll()
        {
            BuildAllData();
            ApplyFiltersAndRebuild();
        }
        
        public override async UniTask PlayShowAsync(object args)
        {
            if (heroSpriteAlas == null)
            {
                heroSpriteAlas = await AddressableSpriteAtlasService.AcquireAtlasAsync(HeroSpriteAtlasKey);
            }

            await RefreshFromServerAsync();

            base.PlayShowAsync(args).Forget();
        }

        /// <summary>
        /// Gọi hero/list lấy data mới nhất (owned + lineup + shards) cho tài khoản hiện tại,
        /// rồi sync vào HeroProgressionManager — tránh hiển thị data cũ leak từ tài khoản/session khác.
        /// </summary>
        private async UniTask RefreshFromServerAsync()
        {
            HeroListResponse response = null;

            try
            {
                response = await NakamaClient.Instance.GetHeroListAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HeroCollectionView] Failed to fetch hero/list: {ex.Message}");
                return;
            }

            if (response == null || HeroProgressionManager.Instance == null) return;

            HeroProgressionManager.Instance.SyncFromServer(response.Owned, response.Shards);

            lineupHeroIds.Clear();
            if (response.Lineup != null && response.Owned != null)
            {
                foreach (var uid in response.Lineup)
                {
                    if (string.IsNullOrEmpty(uid)) continue;

                    foreach (var heroInstance in response.Owned)
                    {
                        if (heroInstance.Uid == uid)
                        {
                            lineupHeroIds.Add(heroInstance.HeroId);
                            break;
                        }
                    }
                }
            }
        }

        public override void OnShow(object args)
        {
            if (HeroProgressionManager.Instance != null)
                HeroProgressionManager.Instance.OnHeroCollectionChanged += HandleHeroCollectionChanged;

            RefreshAll();
            base.OnShow(args);
        }

        public override void OnHide()
        {
            if (HeroProgressionManager.Instance != null)
                HeroProgressionManager.Instance.OnHeroCollectionChanged -= HandleHeroCollectionChanged;
            base.OnHide();
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
            var lineups = new List<int>(lineupHeroIds);

            for (int i = 0; i < allHeroData.Count; i++)
            {
                var hero = allHeroData[i];
                if (hero == null) continue;

                var data = HeroCollectionItemViewDataFactory.Build(
                    hero,
                    heroSummonRarityVisualConfigSo,
                    heroDatabase,
                    service,
                    heroRarityVisualConfig,
                    heroUIIconConfig, heroSpriteAlas);

                if (data != null)
                {
                    data.IsInLineup = lineupHeroIds.Contains(hero.Id);
                    data.LineupIdx = lineups.FindIndex(v => v == hero.Id);
                    allItemsData.Add(data);
                }
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

            OpenHeroInfo(item.Data.HeroId).Forget();
        }

        private async UniTask OpenHeroInfo(int heroId)
        {
            Debug.Log($"Open hero info: {heroId}");
            var ui = await UIManager.Instance.OpenPopupAsync<HeroInfoView>();

            if (ui != null)
            {
                ui.Bind(heroId);
            }
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