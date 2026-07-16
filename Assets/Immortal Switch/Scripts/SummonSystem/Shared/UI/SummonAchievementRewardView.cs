using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.SummonSystem.HeroSummon;
using Immortal_Switch.Scripts.SummonSystem.Shared.Data;
using Immortal_Switch.Scripts.SummonSystem.SkillSummon;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.SummonSystem.Shared.UI
{
    public class SummonAchievementRewardView : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        [Header("Tabs")]
        [SerializeField] private Button heroicTabButton;
        [SerializeField] private Button weaponTabButton;
        [SerializeField] private Button skillTabButton;
        [SerializeField] private Button petTabButton;

        [Header("Tab Visual")]
        [SerializeField] private GameObject heroicSelected;
        [SerializeField] private GameObject weaponSelected;
        [SerializeField] private GameObject skillSelected;
        [SerializeField] private GameObject petSelected;

        [Header("List")]
        [SerializeField] private Transform itemRoot;
        [SerializeField] private SummonAchievementRewardItemUI itemPrefab;

        [SerializeField] private Button closeButton;

        private SimpleUIPool<SummonAchievementRewardItemUI> itemPool;
        private SummonAchievementTab currentTab = SummonAchievementTab.Heroic;

        private void Awake()
        {
            EnsurePool();

            heroicTabButton?.onClick.AddListener(() => SelectTab(SummonAchievementTab.Heroic));
            weaponTabButton?.onClick.AddListener(() => SelectTab(SummonAchievementTab.Weapon));
            skillTabButton?.onClick.AddListener(() => SelectTab(SummonAchievementTab.Skill));
            petTabButton?.onClick.AddListener(() => SelectTab(SummonAchievementTab.Pet));
            closeButton?.onClick.AddListener(Hide);

            Hide();
        }

        private void OnEnable()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged += RefreshView;

            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged += RefreshView;

            SelectTab(currentTab);
        }

        private void OnDisable()
        {
            if (HeroSummonManager.Instance != null)
                HeroSummonManager.Instance.OnSummonDataChanged -= RefreshView;

            if (SkillSummonManager.Instance != null)
                SkillSummonManager.Instance.OnSummonDataChanged -= RefreshView;
        }

        private void EnsurePool()
        {
            if (itemPool != null)
                return;

            if (itemPrefab == null || itemRoot == null)
            {
                Debug.LogError("SummonAchievementRewardView: itemPrefab or itemRoot is null.", this);
                return;
            }

            itemPool = new SimpleUIPool<SummonAchievementRewardItemUI>(itemPrefab, itemRoot);
        }

        public void SelectTab(SummonAchievementTab tab)
        {
            currentTab = tab;
            RefreshTabVisual();
            RefreshView();
        }

        public void RefreshView()
        {
            EnsurePool();
            if (itemPool == null)
                return;

            SummonAchievementRewardListData data = BuildData(currentTab);

            for (int i = 0; i < data.Items.Count; i++)
            {
                var item = itemPool.Get(i);
                item.Bind(data.Items[i]);
            }

            itemPool.ReleaseFrom(data.Items.Count);
        }

        private SummonAchievementRewardListData BuildData(SummonAchievementTab tab)
        {
            switch (tab)
            {
                case SummonAchievementTab.Heroic:
                {
                    if (HeroSummonManager.Instance == null)
                        return new SummonAchievementRewardListData { Tab = tab };

                    return HeroSummonAchievementRewardBuilder.BuildHeroic(
                        HeroSummonManager.Instance.Config,
                        HeroSummonManager.Instance.SaveData
                    );
                }

                case SummonAchievementTab.Skill:
                {
                    if (SkillSummonManager.Instance == null)
                        return new SummonAchievementRewardListData { Tab = tab };

                    return SkillSummonAchievementRewardBuilder.BuildSkill(
                        SkillSummonManager.Instance.Config,
                        SkillSummonManager.Instance.SaveData
                    );
                }

                case SummonAchievementTab.Weapon:
                {
                    if (WeaponSummonManager.Instance == null)
                        return new SummonAchievementRewardListData { Tab = tab };

                    return WeaponSummonAchievementRewardBuilder.BuildWeapon(
                        WeaponSummonManager.Instance.Config,
                        WeaponSummonManager.Instance.SaveData
                    );
                }

                case SummonAchievementTab.Pet:
                default:
                    return new SummonAchievementRewardListData { Tab = tab };
            }
        }

        private void RefreshTabVisual()
        {
            if (heroicSelected != null)
                heroicSelected.SetActive(currentTab == SummonAchievementTab.Heroic);

            if (weaponSelected != null)
                weaponSelected.SetActive(currentTab == SummonAchievementTab.Weapon);

            if (skillSelected != null)
                skillSelected.SetActive(currentTab == SummonAchievementTab.Skill);

            if (petSelected != null)
                petSelected.SetActive(currentTab == SummonAchievementTab.Pet);
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        public void Hide() => SetVisible(false);

        public void Show(SummonAchievementTab tab)
        {
            SetVisible(true);
            SelectTab(tab);
        }
    }
}