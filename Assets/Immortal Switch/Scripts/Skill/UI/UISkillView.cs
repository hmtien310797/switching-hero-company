using System.Collections.Generic;
using Immortal_Switch.Hero;
using Scripts.Battle;
using UnityEngine;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class UISkillView : MonoBehaviour
    {
        [Header("Class Buttons")]
        [SerializeField] private SkillClassButtonView[] classButtons;

        [Header("Hero Tabs")]
        [SerializeField] private SkillHeroTabView[] heroTabs;

        [Header("Equipped Slots")]
        [SerializeField] private SkillEquippedSlotView[] equippedSlots;

        [Header("Skill Grid")]
        [SerializeField] private Transform gridRoot;
        [SerializeField] private SkillItemView skillItemPrefab;

        [Header("Replace Popup")]
        [SerializeField] private GameObject replacePopup;
        [SerializeField] private SkillReplaceSlotView[] replaceSlots;

        private HeroClass selectedClass;
        private int selectedHeroId;
        private int selectedSkillId;

        private List<SkillItemView> currentItems = new();

        // =========================
        // INIT
        // =========================
        private void Start()
        {
            BindClassButtons();
        }

        private void BindClassButtons()
        {
            for (int i = 0; i < classButtons.Length; i++)
            {
                int idx = i;
                classButtons[i].Button.onClick.AddListener(() =>
                {
                    OnClickClass((HeroClass)idx);
                });
            }
        }

        // =========================
        // CLASS CLICK
        // =========================
        private void OnClickClass(HeroClass heroClass)
        {
            selectedClass = heroClass;

            RefreshClassButtons();
            RefreshContent();
        }

        private void RefreshClassButtons()
        {
            foreach (var btn in classButtons)
            {
                bool isSelected = (HeroClass)System.Array.IndexOf(classButtons, btn) == selectedClass;
                btn.SetSelected(isSelected);

                bool isEquipped = IsClassActive(selectedClass);
                btn.SetEquipped(isEquipped);
            }
        }

        // =========================
        // MAIN CONTENT
        // =========================
        private void RefreshContent()
        {
            var heroes = GetActiveHeroesByClass(selectedClass);

            if (heroes.Count == 0)
            {
                ShowUnassignedMode();
                return;
            }

            ShowAssignedMode();

            selectedHeroId = heroes[0];
            RefreshHeroTabs(heroes);
            RefreshEquipped();
            RefreshGrid();
        }

        // =========================
        // HERO
        // =========================
        private void RefreshHeroTabs(List<int> heroIds)
        {
            for (int i = 0; i < heroTabs.Length; i++)
            {
                if (i >= heroIds.Count)
                {
                    heroTabs[i].gameObject.SetActive(false);
                    continue;
                }

                heroTabs[i].gameObject.SetActive(true);

                int heroId = heroIds[i];
                var hero = GetHeroController(heroId);

                heroTabs[i].Setup(heroId, hero.HeroIcon, heroId == selectedHeroId);

                heroTabs[i].Button.onClick.RemoveAllListeners();
                heroTabs[i].Button.onClick.AddListener(() =>
                {
                    selectedHeroId = heroId;
                    RefreshEquipped();
                    RefreshGrid();
                });
            }
        }

        // =========================
        // EQUIPPED
        // =========================
        private void RefreshEquipped()
        {
            var hero = GetHeroController(selectedHeroId);
            var dict = hero.SkillIdDict;

            int i = 0;
            foreach (var kvp in dict)
            {
                int skillId = kvp.Value;

                var data = MasterDataCache.Instance.GetSkillDataById(skillId);

                equippedSlots[i].Setup(i, skillId, data.skillIcon, skillId == selectedSkillId);

                int captured = skillId;

                equippedSlots[i].Button.onClick.RemoveAllListeners();
                equippedSlots[i].Button.onClick.AddListener(() =>
                {
                    selectedSkillId = captured;
                    RefreshEquipped();
                    RefreshGrid();
                });

                i++;
            }
        }

        // =========================
        // GRID
        // =========================
        private void RefreshGrid()
        {
            foreach (var item in currentItems)
                Destroy(item.gameObject);

            currentItems.Clear();

            var skills = GetSkillsByClass(selectedClass);

            foreach (var skill in skills)
            {
                var item = Instantiate(skillItemPrefab, gridRoot);

                bool isEquipped = IsSkillEquipped(selectedHeroId, skill.SkillId);
                bool isOwned = true; // TODO

                item.Setup(
                    skill.SkillId,
                    skill.skillIcon,
                    isEquipped,
                    isOwned,
                    20,
                    22,
                    skill.SkillId == selectedSkillId
                );

                item.Button.onClick.AddListener(() =>
                {
                    selectedSkillId = skill.SkillId;
                    OnClickSkill(skill.SkillId);
                    RefreshGrid();
                    RefreshEquipped();
                });

                currentItems.Add(item);
            }
        }

        // =========================
        // SKILL ACTION
        // =========================
        private void OnClickSkill(int skillId)
        {
            if (IsSkillEquipped(selectedHeroId, skillId))
            {
                Unequip(skillId);
            }
            else
            {
                TryEquip(skillId);
            }
        }

        private void TryEquip(int skillId)
        {
            if (HasEmptySlot(selectedHeroId))
            {
                EquipToEmpty(skillId);
            }
            else
            {
                OpenReplacePopup(skillId);
            }
        }

        private void OpenReplacePopup(int newSkillId)
        {
            replacePopup.SetActive(true);

            var hero = GetHeroController(selectedHeroId);

            int i = 0;
            foreach (var kvp in hero.SkillIdDict)
            {
                int skillId = kvp.Value;

                var data = MasterDataCache.Instance.GetSkillDataById(skillId);

                replaceSlots[i].Setup(i, data.skillIcon);

                int capturedSlot = i;

                replaceSlots[i].Button.onClick.RemoveAllListeners();
                replaceSlots[i].Button.onClick.AddListener(() =>
                {
                    ReplaceSkill(capturedSlot, newSkillId);
                    replacePopup.SetActive(false);
                    RefreshEquipped();
                    RefreshGrid();
                });

                i++;
            }
        }

        // =========================
        // MOCK / TODO
        // =========================
        private bool IsClassActive(HeroClass cls) => true;
        private List<int> GetActiveHeroesByClass(HeroClass cls) => new() { 1 };
        private PlayerHeroController GetHeroController(int id) => null;
        private List<SkillDataSO> GetSkillsByClass(HeroClass cls) => new();
        private bool IsSkillEquipped(int heroId, int skillId) => false;
        private bool HasEmptySlot(int heroId) => false;

        private void EquipToEmpty(int skillId) { }
        private void Unequip(int skillId) { }
        private void ReplaceSkill(int slot, int newSkillId) { }

        private void ShowUnassignedMode() { }
        private void ShowAssignedMode() { }
    }
}