using System.Collections.Generic;
using Battle;
using Common;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Equipment.UI;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Equipment.UIRuntime
{
    public class UIWeaponView : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private WeaponViewDataProvider dataProvider;

        [Header("Mode Tabs")] [SerializeField] private Button btnStandardTab;
        [SerializeField] private Button btnExclusiveTab;
        [SerializeField] private GameObject standardRoot;
        [SerializeField] private GameObject exclusiveRoot;

        [Header("Class Tabs")] [SerializeField]
        private UIWeaponClassTabItem[] uIWeaponClassTabItems;

        [Header("Shared Weapon Item Container")] [SerializeField]
        private Transform weaponItemContainer;

        [SerializeField] private UIStandardWeaponItem standardItemPrefab;
        [SerializeField] private UIExclusiveWeaponItem exclusiveItemPrefab;

        [Header("Detail")] [SerializeField] private UIWeaponDetailPanel detailPanel;

        private readonly List<UIStandardWeaponItem> standardItems = new();
        private readonly List<UIExclusiveWeaponItem> exclusiveItems = new();
        private StandardWeaponTabViewModel vmStandard;
        
        private WeaponMainTab currentMainTab = WeaponMainTab.Standard;
        private HeroClass selectedClass = HeroClass.Archer;
        private int selectedStandardWeaponId;
        private int selectedHeroId;

        private void Awake()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
        }

        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
        }

        private UniTask OnClickTutorial(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 46:
                    OnSelectStandardWeapon(vmStandard.Weapons[0].WeaponId);
                    break;
            }

            return UniTask.CompletedTask;
        }

        private RectTransform OnResolveTarget(string arg1, int arg2)
        {
            switch (arg2)
            {
                case 46:
                    return standardItems[0].transform as RectTransform;
                
                default:
                    return null;
            }
        }

        public void Setup(WeaponViewDataProvider provider, int heroId, HeroClass defaultClass, List<HeroActor> deployedHeroes)
        {
            dataProvider = provider;
            selectedHeroId = heroId;
            selectedClass = defaultClass;

            if (dataProvider != null)
                dataProvider.SetDeployedHeroes(deployedHeroes);

            BindButtons();
            RefreshAll();
        }

        private void OnEnable()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            if (btnStandardTab != null)
            {
                btnStandardTab.onClick.RemoveListener(OnClickStandardTab);
                btnStandardTab.onClick.AddListener(OnClickStandardTab);
            }

            // Exclusive weapon ẩn ở v1 — BE chưa có master data mapping hero→exclusive_weapon_id,
            // mọi request category="exclusive" luôn trả EXCLUSIVE_NOT_FOUND/EXCLUSIVE_NOT_OWNED
            // (xem docs/api_weapon_equip_upgrade.md mục 1, 9). Ẩn hẳn tab để người chơi không bấm
            // vào rồi luôn gặp lỗi. currentMainTab mặc định Standard nên không cần xử lý gì thêm.
            if (btnExclusiveTab != null)
            {
                btnExclusiveTab.onClick.RemoveListener(OnClickExclusiveTab);
                btnExclusiveTab.gameObject.SetActive(false);
            }

            if (exclusiveRoot != null)
                exclusiveRoot.SetActive(false);
        }

        public void RefreshAll()
        {
            RefreshMode();
            RefreshCurrentTab();
        }

        public void SetDeployedHeroes(List<HeroActor> heroes)
        {
            if (dataProvider != null)
                dataProvider.SetDeployedHeroes(heroes);
        }

        private void RefreshMode()
        {
            if (standardRoot != null)
                standardRoot.SetActive(currentMainTab == WeaponMainTab.Standard);

            if (exclusiveRoot != null)
                exclusiveRoot.SetActive(currentMainTab == WeaponMainTab.Exclusive);
        }

        private void RefreshCurrentTab()
        {
            if (dataProvider == null)
                return;

            switch (currentMainTab)
            {
                case WeaponMainTab.Standard:
                    RefreshStandardTab();
                    break;

                case WeaponMainTab.Exclusive:
                    RefreshExclusiveTab();
                    break;
            }
        }

        private void RefreshStandardTab()
        {
            var vm = dataProvider.BuildStandardTab(selectedClass, selectedStandardWeaponId, selectedHeroId);
            vmStandard = vm;

            if (vm == null)
                return;

            BindClassTabs(vm.ClassTabs);
            BindStandardItems(vm.Weapons);
            HideExclusiveItems();

            if (vm.SelectedDetail != null &&
                detailPanel != null)
                detailPanel.Bind(vm.SelectedDetail, selectedHeroId, RefreshAll);
        }

        private void RefreshExclusiveTab()
        {
            var vm = dataProvider.BuildExclusiveTab(selectedHeroId);

            if (vm == null)
                return;

            BindExclusiveItems(vm.ExclusiveCard);
            HideStandardItems();

            if (vm.SelectedDetail != null &&
                detailPanel != null)
                detailPanel.Bind(vm.SelectedDetail, selectedHeroId, RefreshAll);
        }

        private void BindClassTabs(List<WeaponClassTabViewModel> tabs)
        {
            for (int i = 0; i < uIWeaponClassTabItems.Length; i++)
            {
                bool active = i < tabs.Count;
                uIWeaponClassTabItems[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                uIWeaponClassTabItems[i].Bind(tabs[i], OnSelectClassTab);
            }
        }
        
        private float GetScaledWeaponStatValue(float baseValue, int level)
        {
            if (level <= 1)
                return baseValue;

            return baseValue * (1f + (level - 1) * 0.05f);
        }

        private void BindStandardItems(List<StandardWeaponCardViewModel> items)
        {
            EnsureStandardItemPool(items.Count);

            for (int i = 0; i < standardItems.Count; i++)
            {
                bool active = i < items.Count;
                standardItems[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                standardItems[i].Bind(items[i], OnSelectStandardWeapon);
            }
        }

        private void BindExclusiveItems(ExclusiveWeaponCardViewModel item)
        {
            int count = item != null ? 1 : 0;
            EnsureExclusiveItemPool(count);

            for (int i = 0; i < exclusiveItems.Count; i++)
            {
                bool active = i < count;
                exclusiveItems[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                exclusiveItems[i].Bind(item, OnSelectExclusiveWeapon);
            }
        }

        private void HideStandardItems()
        {
            for (int i = 0; i < standardItems.Count; i++)
            {
                if (standardItems[i] != null)
                    standardItems[i].gameObject.SetActive(false);
            }
        }

        private void HideExclusiveItems()
        {
            for (int i = 0; i < exclusiveItems.Count; i++)
            {
                if (exclusiveItems[i] != null)
                    exclusiveItems[i].gameObject.SetActive(false);
            }
        }

        private void EnsureStandardItemPool(int targetCount)
        {
            while (standardItems.Count < targetCount)
            {
                var item = Instantiate(standardItemPrefab, weaponItemContainer);
                standardItems.Add(item);
            }
        }

        private void EnsureExclusiveItemPool(int targetCount)
        {
            while (exclusiveItems.Count < targetCount)
            {
                var item = Instantiate(exclusiveItemPrefab, weaponItemContainer);
                exclusiveItems.Add(item);
            }
        }

        private void OnClickStandardTab()
        {
            currentMainTab = WeaponMainTab.Standard;
            RefreshAll();
        }

        private void OnClickExclusiveTab()
        {
            currentMainTab = WeaponMainTab.Exclusive;
            RefreshAll();
        }

        private void OnSelectClassTab(HeroClass heroClass)
        {
            selectedClass = heroClass;
            selectedStandardWeaponId = 0;
            
            HeroActor heroController = UserDataCache.Instance.TryGetActiveHeroByClass(heroClass);
            if (PvEBattleController.Instance != null &&
                heroController != null)
            {
                selectedHeroId = heroController.GetHeroId();
            }

            RefreshStandardTab();
        }

        private void OnSelectStandardWeapon(int weaponId)
        {
            selectedStandardWeaponId = weaponId;
            RefreshStandardTab();
        }

        private void OnSelectExclusiveWeapon(int heroId)
        {
            selectedHeroId = heroId;
            RefreshExclusiveTab();
        }

        public void SetFocusedHero(int heroId, HeroClass heroClass)
        {
            selectedHeroId = heroId;
            selectedClass = heroClass;
            RefreshAll();
        }
    }

    public enum WeaponMainTab
    {
        Standard = 0,
        Exclusive = 1
    }
}