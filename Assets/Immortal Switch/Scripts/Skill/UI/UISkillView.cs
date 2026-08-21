using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared;
using Immortal_Switch.Scripts.Shared.Constants;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class UISkillView : MonoBehaviour
    {
        private SkillViewDataProvider dataProvider;

        [Header("Class Buttons")] 
        [SerializeField]
        private SkillClassButtonView[] classButtons;

        [Header("Roots")] 
        [SerializeField] private GameObject assignedContentRoot;
        [SerializeField] private GameObject unassignedContentRoot;
        [SerializeField] private GameObject heroTabsRoot;
        [SerializeField] private GameObject equippedSlotsRoot;

        [Header("Hero Tabs")] 
        [SerializeField] private SkillHeroTabView[] heroTabs;

        [Header("Equipped Slots")] 
        [SerializeField] private SkillEquippedSlotView[] equippedSlots;

        [Header("Grid")] 
        [SerializeField] private Transform gridRoot;
        [SerializeField] private SkillItemView skillItemPrefab;

        [Header("Detail")] 
        [SerializeField] private Image detailIcon;
        [SerializeField] private Image bgImg;
        [SerializeField] private Image frameImg;
        [SerializeField] private TMP_Text detailLevelText;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailTypeText;
        [SerializeField] private TMP_Text detailDescText;
        [SerializeField] private TMP_Text detailShardText;
        [SerializeField] private Image detailShardFill;

        [Header("Warning")] 
        [SerializeField] private TMP_Text warningText;

        [Header("Buttons")] 
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonText;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button glyphButton;
        [SerializeField] private Button autoEquipButton;
        [SerializeField] private TMP_Text autoEquipButtonText;

        [Header("Replace Popup")] 
        [SerializeField] private GameObject replacePopupRoot;

        [SerializeField] private SkillHeroTabView replaceHeroPreview;
        [SerializeField] private Image replacePendingSkillIcon;
        [SerializeField] private TMP_Text replaceInstructionText;
        [SerializeField] private SkillReplaceSlotView[] replaceSlotViews;
        [SerializeField] private Button replaceCloseButton;

        private readonly List<SkillItemView> spawnedGridItems = new();

        private HeroClass selectedClass;
        private SkillViewHeroContext selectedHero;
        private SkillDataSO selectedSkill;
        private SkillDataSO pendingReplaceSkill;
        private bool isReplaceMode;
        private bool isAutoEquipping;

        private void Awake()
        {
            dataProvider = SkillViewDataProvider.Instance;
            BindStaticEvents();
        }

        private void OnEnable()
        {
            if (dataProvider != null)
                dataProvider.OnDataChanged += RefreshCurrentContext;
        }

        private void OnDisable()
        {
            selectedClass = HeroClass.None;
            if (dataProvider != null)
                dataProvider.OnDataChanged -= RefreshCurrentContext;
        }

        private void BindStaticEvents()
        {
            foreach (var button in classButtons)
            {
                if (button == null) continue;

                var captured = button;
                captured.Button.onClick.AddListener(() => OnClickClass(captured.HeroClass));
            }

            for (int i = 0; i < heroTabs.Length; i++)
            {
                int index = i;
                heroTabs[i].Button.onClick.AddListener(() => OnClickHeroTab(index));
            }

            for (int i = 0; i < equippedSlots.Length; i++)
            {
                int index = i;
                equippedSlots[i].Button.onClick.AddListener(() => OnClickEquippedSlot(index));
            }

            if (equipButton != null)
                equipButton.onClick.AddListener(OnClickEquipOrUnequip);
            
            if (autoEquipButton != null)
                autoEquipButton.onClick.AddListener(() => OnClickAutoEquip().Forget());

            if (replaceCloseButton != null)
                replaceCloseButton.onClick.AddListener(CloseReplacePopup);
        }

        public void OpenDefaultClass(bool open = true)
        {
            gameObject.SetActive(open);
            if (!open)
            {
                return;
            }
            if (classButtons == null || classButtons.Length == 0)
            {
                return;
            }

            if (selectedClass == HeroClass.None)
            {
                var firstAssigned = classButtons.FirstOrDefault(x =>
                    x != null && dataProvider != null && dataProvider.HasAssignedHero(x.HeroClass));
                selectedClass = firstAssigned != null ? firstAssigned.HeroClass : classButtons[0].HeroClass;
            }

            selectedHero = null;
            selectedSkill = null;
            
            RefreshCurrentContext();
        }

        private void OnClickClass(HeroClass heroClass)
        {
            if (isReplaceMode)
            {
                CloseReplacePopup();
            }

            selectedClass = heroClass;
            selectedHero = dataProvider != null ? dataProvider.GetAssignedHeroByClass(heroClass) : null;
            selectedSkill = null;
            
            RefreshCurrentContext();
        }

        private void OnClickHeroTab(int heroIndex)
        {
            if (isReplaceMode)
            {
                return;
            }

            var activeHeroes = dataProvider.GetAssignedHeroes();
            if (heroIndex < 0 || heroIndex >= activeHeroes.Count)
            {
                return;
            }

            selectedHero = activeHeroes[heroIndex];
            selectedClass = selectedHero.HeroClass;
            selectedSkill = null;
            
            RefreshCurrentContext();
        }
        
        private async UniTaskVoid OnClickAutoEquip()
        {
            if (isAutoEquipping)
            {
                return;
            }

            if (isReplaceMode)
            {
                return;
            }

            if (selectedHero == null)
            {
                return;
            }

            if (dataProvider == null)
            {
                return;
            }

            isAutoEquipping = true;
            SetAutoEquipButtonState(false, "Đang trang bị...");

            try
            {
                int heroId = selectedHero.HeroId;
                

                SkillAutoEquipResult result =
                    await dataProvider.TryAutoEquipSkillsToHero(selectedHero);

                if (!result.Success)
                {
                    return;
                }
                

                /*
                 * Lấy lại context mới từ DataProvider.
                 * Không tiếp tục dùng selectedHero cũ vì danh sách EquippedSkillIds
                 * trong context cũ có thể chưa được cập nhật.
                 */
                selectedHero = dataProvider.GetAssignedHeroByClass(selectedClass);

                RefreshCurrentContext();
            }
            finally
            {
                isAutoEquipping = false;
                SetAutoEquipButtonState(selectedHero != null, "Tự động trang bị");
            }
        }
        
        private void SetAutoEquipButtonState(bool interactable, string buttonText)
        {
            if (autoEquipButton != null)
                autoEquipButton.interactable = interactable;

            if (autoEquipButtonText != null)
                autoEquipButtonText.text = buttonText;
        }
        
        public void RefreshAll()
        {
            // RefreshHeroTabs();
            // RefreshSkillGrid();
            // RefreshEquippedSlots();
            // RefreshSelectedSkillDetail();
        }

        public void SetSelectedHeroClass(HeroClass heroClass)
        {
            selectedClass = heroClass;
        }

        private void OnClickEquippedSlot(int slotIndex)
        {
            if (isReplaceMode || selectedHero == null)
                return;

            if (selectedHero.EquippedSkillIds == null || slotIndex < 0 ||
                slotIndex >= selectedHero.EquippedSkillIds.Count)
                return;

            int skillId = selectedHero.EquippedSkillIds[slotIndex];
            selectedSkill = dataProvider.GetClassPool(selectedHero.HeroClass)
                .FirstOrDefault(x => x != null && x.SkillId == skillId);

            RefreshCurrentContext();
        }

        private void OnClickGridSkill(SkillDataSO skillData)
        {
            if (isReplaceMode)
            {
                return;
            }

            if (skillData == null)
            {
                return;
            }

            selectedSkill = skillData;
            RefreshCurrentContext();
        }

        private void RefreshCurrentContext()
        {
            if (dataProvider ==null)
            {
                dataProvider = SkillViewDataProvider.Instance;
            }

            RefreshClassButtons();

            bool hasAssignedHero = dataProvider.HasAssignedHero(selectedClass);

            if (assignedContentRoot != null)
                assignedContentRoot.SetActive(hasAssignedHero);

            if (unassignedContentRoot != null)
                unassignedContentRoot.SetActive(!hasAssignedHero);

            if (!hasAssignedHero)
            {
                selectedHero = null;

                var notInBattleHeroSkillPool = dataProvider.GetSortedPoolForNotInBattleHero(selectedClass);
                selectedSkill = ValidateSelectedSkill(notInBattleHeroSkillPool, selectedHero);
                var itemTier1 = EnumHelper.TierSkillToItemTier(selectedSkill.SkillTier);
                var tierInfo1 = ItemTierVisualImageService.GetItemTierEntry(itemTier1);

                if (warningText != null)
                    warningText.text = "Cannot equip skill because this Hero's class is not assigned";

                BindHeroTabs();
                BindEquippedSlots();
                RebuildGridByClass(selectedClass, null);
                BindDetail(null, selectedSkill, tierInfo1);
                CloseReplacePopup();
                
                return;
            }

            selectedHero = ResolveSelectedHero();

            if (selectedHero == null)
            {
                return;
            }

            BindHeroTabs();
            BindEquippedSlots();

            var pool = dataProvider.GetSortedPoolForHero(selectedHero);
            selectedSkill = ValidateSelectedSkill(pool, selectedHero);

            var itemTier = EnumHelper.TierSkillToItemTier(selectedSkill.SkillTier);
            var tierInfo = ItemTierVisualImageService.GetItemTierEntry(itemTier);

            RebuildGridByClass(selectedClass, selectedHero);
            BindDetail(selectedHero, selectedSkill, tierInfo);

            if (isReplaceMode)
            {
                if (selectedHero == null || pendingReplaceSkill == null)
                {
                    CloseReplacePopup();
                }
                else
                {
                    OpenReplacePopupInternal();
                }
            }
        }

        private SkillViewHeroContext ResolveSelectedHero()
        {
            var activeHeroes = dataProvider.GetAssignedHeroes();

            if (selectedHero != null)
            {
                var matched = activeHeroes.FirstOrDefault(x => x != null && x.HeroId == selectedHero.HeroId);
                if (matched != null)
                {
                    return matched;
                }
                
            }

            var heroByClass = dataProvider.GetAssignedHeroByClass(selectedClass);
            if (heroByClass != null)
            {
                return heroByClass;
            }

            var first = activeHeroes.FirstOrDefault();
            return first;
        }

        private SkillDataSO ValidateSelectedSkill(List<SkillDataSO> pool, SkillViewHeroContext hero)
        {
            if (pool == null || pool.Count == 0)
                return null;

            if (selectedSkill != null && pool.Any(x => x != null && x.SkillId == selectedSkill.SkillId))
                return selectedSkill;

            if (hero != null && hero.EquippedSkillIds != null)
            {
                foreach (var skillId in hero.EquippedSkillIds)
                {
                    var equippedSkill = pool.FirstOrDefault(x => x != null && x.SkillId == skillId);
                    if (equippedSkill != null)
                        return equippedSkill;
                }
            }

            return pool[0];
        }

        private void RefreshClassButtons()
        {
            foreach (var button in classButtons)
            {
                if (button == null) continue;

                bool isAssigned = dataProvider != null && dataProvider.HasAssignedHero(button.HeroClass);
                bool isSelected = button.HeroClass == selectedClass;
                button.SetAssigned(isAssigned);
                button.SetSelected(isSelected);
            }
        }

        private void BindHeroTabs()
        {
            var activeHeroes = dataProvider.GetAssignedHeroes();

            for (int i = 0; i < heroTabs.Length; i++)
            {
                if (i >= activeHeroes.Count)
                {
                    heroTabs[i].Hide();
                    continue;
                }

                var hero = activeHeroes[i];
                bool isSelected = selectedHero != null && hero.HeroId == selectedHero.HeroId;
                heroTabs[i].Setup(hero.HeroId, hero.HeroIcon, hero.classIcon, isSelected);
            }
        }

        private void BindEquippedSlots()
        {
            for (int i = 0; i < equippedSlots.Length; i++)
            {
                if (selectedHero == null || selectedHero.EquippedSkillIds == null ||
                    i >= selectedHero.EquippedSkillIds.Count)
                {
                    equippedSlots[i].Setup(i, -1, null, false);
                    continue;
                }

                int skillId = selectedHero.EquippedSkillIds[i];
                var skillData = dataProvider.GetClassPool(selectedHero.HeroClass)
                    .FirstOrDefault(x => x != null && x.SkillId == skillId);

                bool isSelected = selectedSkill != null && skillData != null &&
                                  selectedSkill.SkillId == skillData.SkillId;
                equippedSlots[i].Setup(i, skillId, SkillImageService.GetSkillIcon(skillData), isSelected);
            }
        }

        private void RebuildGridByClass(HeroClass heroClass, SkillViewHeroContext hero)
        {
            foreach (var item in spawnedGridItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            spawnedGridItems.Clear();

            var pool = hero != null ? dataProvider.GetSortedPoolForHero(hero) : dataProvider.GetSortedPoolForNotInBattleHero(heroClass);

            foreach (var skillData in pool)
            {
                var state = dataProvider.BuildSkillState(hero, skillData);
                if (state == null) continue;

                var item = Instantiate(skillItemPrefab, gridRoot);
                bool isSelected = selectedSkill != null && skillData != null &&
                                  selectedSkill.SkillId == skillData.SkillId;

                item.name = $"skill_{skillData.SkillClass}_{skillData.SkillId}";
                var itemTier = EnumHelper.TierSkillToItemTier(skillData.SkillTier);
                var tierInfo = ItemTierVisualImageService.GetItemTierEntry(itemTier);
                
                item.Setup(state, tierInfo, isSelected, OnClickGridSkill);
                spawnedGridItems.Add(item);
            }
        }

        private void BindDetail(SkillViewHeroContext hero, SkillDataSO skillData, ItemTierEntry tierInfo)
        {
            if (skillData == null)
            {
                if (detailIcon != null) detailIcon.sprite = null;
                if (detailLevelText != null) detailLevelText.text = string.Empty;
                if (detailNameText != null) detailNameText.text = string.Empty;
                if (detailTypeText != null) detailTypeText.text = string.Empty;
                if (detailDescText != null) detailDescText.text = string.Empty;
                if (detailShardText != null) detailShardText.text = string.Empty;
                if (detailShardFill != null) detailShardFill.fillAmount = 0f;

                if (equipButton != null) equipButton.interactable = false;
                if (equipButtonText != null) equipButtonText.text = LocalizationManager.GetText(LocalizationKeys.UI_EQUIP_GEAR);
                return;
            }

            var state = dataProvider.BuildSkillState(hero, skillData);

            if (detailIcon != null) detailIcon.sprite = SkillImageService.GetSkillIcon(skillData);

            if (tierInfo != null)
            {
                if (bgImg != null)
                    bgImg.sprite = tierInfo.background;

                if (frameImg != null)
                    frameImg.sprite = tierInfo.border;
            }

            if (detailLevelText != null) detailLevelText.text = LocalizationManager.GetText(LocalizationKeys.UI_LV, state.Level);
            if (detailNameText != null) detailNameText.text = skillData.GetLocalizedSkillName();
            //if (detailTypeText != null) detailTypeText.text = $"{skillData.CastType} kỹ năng";
            if (detailDescText != null) detailDescText.text = skillData.BuildDescription(state.Level);
            if (detailShardText != null) detailShardText.text = $"{state.CurrentShard}/{state.RequiredShard}";
            if (detailShardFill != null)
                detailShardFill.fillAmount =
                    state.RequiredShard > 0 ? (float)state.CurrentShard / state.RequiredShard : 0f;

            if (!state.IsOwned)
            {
                if (equipButton != null) equipButton.interactable = false;
                if (equipButtonText != null) equipButtonText.text = LocalizationManager.GetText(LocalizationKeys.UI_EQUIP_GEAR);
                return;
            }

            if (equipButton != null) equipButton.interactable = hero != null;
            if (equipButtonText != null) equipButtonText.text = state.IsEquipped ? LocalizationManager.GetText(LocalizationKeys.UI_UNEQUIP_GEAR) : LocalizationManager.GetText(LocalizationKeys.UI_EQUIP_GEAR);
        }

        private void OnClickEquipOrUnequip()
        {
            if (selectedHero == null)
            {
                return;
            }

            if (selectedSkill == null)
            {
                return;
            }

            var state = dataProvider.BuildSkillState(selectedHero, selectedSkill);
            if (state == null)
            {
                return;
            }

            if (!state.IsOwned)
            {
                return;
            }

            if (state.IsEquipped)
            {
                dataProvider.TryUnequipSkillFromHero(selectedHero, selectedSkill.SkillId);
                return;
            }

            // EquippedSkillIds giờ giữ đúng 5 slot thật (0 = trống), không còn bị dồn lại — đếm số
            // slot thật sự có skill (>0) thay vì dùng List.Count (luôn là 5).
            int equippedCount = selectedHero.EquippedSkillIds?.Count(id => id > 0) ?? 0;
            if (equippedCount < 5)
            {
                dataProvider.TryEquipSkillToHero(selectedHero, selectedSkill.SkillId);
                return;
            }

            pendingReplaceSkill = selectedSkill;
            isReplaceMode = true;
            OpenReplacePopupInternal();
        }

        private void OpenReplacePopupInternal()
        {
            if (replacePopupRoot != null)
                replacePopupRoot.SetActive(true);

            if (replaceHeroPreview != null && selectedHero != null)
                replaceHeroPreview.Setup(selectedHero.HeroId, selectedHero.HeroIcon, selectedHero.classIcon, true);

            if (replacePendingSkillIcon != null)
                replacePendingSkillIcon.sprite = SkillImageService.GetSkillIcon(pendingReplaceSkill);

            if (replaceInstructionText != null)
                replaceInstructionText.text = LocalizationManager.GetText(LocalizationKeys.UI_SELECT_A_SLOT);

            for (int i = 0; i < replaceSlotViews.Length; i++)
            {
                if (selectedHero == null || selectedHero.EquippedSkillIds == null ||
                    i >= selectedHero.EquippedSkillIds.Count)
                {
                    replaceSlotViews[i].gameObject.SetActive(false);
                    continue;
                }

                int slotIndex = i;
                int skillId = selectedHero.EquippedSkillIds[i];

                var skillData = dataProvider.GetClassPool(selectedHero.HeroClass)
                    .FirstOrDefault(x => x != null && x.SkillId == skillId);

                replaceSlotViews[i].Setup(slotIndex, SkillImageService.GetSkillIcon(skillData));
                replaceSlotViews[i].Button.onClick.RemoveAllListeners();
                replaceSlotViews[i].Button.onClick.AddListener(() => OnClickReplaceSlot(slotIndex).Forget());
            }
        }

        private async UniTask OnClickReplaceSlot(int slotIndex)
        {
            if (!isReplaceMode)
            {
                return;
            }

            if (selectedHero == null)
            {
                return;
            }

            if (pendingReplaceSkill == null)
            {
                return;
            }

            bool success = await dataProvider.TryReplaceSkillOnHero(selectedHero, slotIndex, pendingReplaceSkill.SkillId);
            if (!success)
            {
                return;
            }

            selectedSkill = pendingReplaceSkill;

            CloseReplacePopup();
            RefreshCurrentContext();
        }

        private void CloseReplacePopup()
        {
            isReplaceMode = false;
            pendingReplaceSkill = null;

            if (replacePopupRoot != null)
                replacePopupRoot.SetActive(false);
        }
    }
}