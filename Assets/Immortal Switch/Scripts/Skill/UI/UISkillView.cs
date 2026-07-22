using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Helper;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.Items.ScriptableObjects;
using Immortal_Switch.Scripts.Shared;
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
        [SerializeField] private bool enableDebugLog = true;

        private void LogView(string message)
        {
            if (!enableDebugLog) return;
            Debug.Log($"[UISkillView] {message}", this);
        }

        private void LogWarningView(string message)
        {
            if (!enableDebugLog) return;
            Debug.LogWarning($"[UISkillView] {message}", this);
        }

        private void LogErrorView(string message)
        {
            Debug.LogError($"[UISkillView] {message}", this);
        }

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
                LogErrorView("ClassButtons is null or empty.");
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

            LogView($"OpenDefaultClass -> selectedClass={selectedClass}");
            RefreshCurrentContext();
        }

        private void OnClickClass(HeroClass heroClass)
        {
            if (isReplaceMode)
            {
                LogView($"Closing replace popup before switching class to {heroClass}");
                CloseReplacePopup();
            }

            selectedClass = heroClass;
            selectedHero = dataProvider != null ? dataProvider.GetAssignedHeroByClass(heroClass) : null;
            selectedSkill = null;

            LogView(
                $"OnClickClass -> selectedClass={selectedClass}, hero={(selectedHero != null ? selectedHero.HeroId.ToString() : "null")}");
            RefreshCurrentContext();
        }

        private void OnClickHeroTab(int heroIndex)
        {
            if (isReplaceMode)
            {
                LogWarningView("Ignored hero tab click because replace mode is active.");
                return;
            }

            var activeHeroes = dataProvider.GetAssignedHeroes();
            if (heroIndex < 0 || heroIndex >= activeHeroes.Count)
            {
                LogErrorView($"OnClickHeroTab invalid index={heroIndex}, activeHeroesCount={activeHeroes.Count}");
                return;
            }

            selectedHero = activeHeroes[heroIndex];
            selectedClass = selectedHero.HeroClass;
            selectedSkill = null;

            LogView($"OnClickHeroTab -> heroId={selectedHero.HeroId}, class={selectedClass}");
            RefreshCurrentContext();
        }
        
        private async UniTaskVoid OnClickAutoEquip()
        {
            if (isAutoEquipping)
            {
                LogWarningView("Auto equip ignored because another auto equip is running.");
                return;
            }

            if (isReplaceMode)
            {
                LogWarningView("Auto equip ignored because replace mode is active.");
                return;
            }

            if (selectedHero == null)
            {
                LogWarningView("Auto equip failed because selectedHero is null.");
                return;
            }

            if (dataProvider == null)
            {
                LogErrorView("Auto equip failed because dataProvider is null.");
                return;
            }

            isAutoEquipping = true;
            SetAutoEquipButtonState(false, "Đang trang bị...");

            try
            {
                int heroId = selectedHero.HeroId;

                LogView(
                    $"OnClickAutoEquip started -> " +
                    $"heroId={selectedHero.HeroId}, class={selectedHero.HeroClass}");

                SkillAutoEquipResult result =
                    await dataProvider.TryAutoEquipSkillsToHero(selectedHero);

                if (!result.Success)
                {
                    LogWarningView(
                        $"Auto equip failed -> heroId={heroId}");
                    return;
                }

                LogView(
                    $"Auto equip completed -> " +
                    $"heroId={heroId}, " +
                    $"hasChanged={result.HasChanged}, " +
                    $"equippedCount={result.EquippedCount}, " +
                    $"skills=[{string.Join(",", result.EquippedSkillIds)}]");

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
                LogWarningView("Ignored grid skill click because replace mode is active.");
                return;
            }

            if (skillData == null)
            {
                LogErrorView("OnClickGridSkill received null skillData.");
                return;
            }

            selectedSkill = skillData;
            LogView($"OnClickGridSkill -> skillId={skillData.SkillId}, skillName={skillData.SkillName}");
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

            LogView(
                $"RefreshCurrentContext -> selectedClass={selectedClass}, hasAssignedHero={hasAssignedHero}, isReplaceMode={isReplaceMode}");

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

                LogWarningView($"Class {selectedClass} has no assigned hero. Showing warning mode.");
                return;
            }

            selectedHero = ResolveSelectedHero();

            if (selectedHero == null)
            {
                LogErrorView($"ResolveSelectedHero returned null while class {selectedClass} is assigned.");
                return;
            }

            LogView(
                $"ResolvedHero -> heroId={selectedHero.HeroId}, class={selectedHero.HeroClass}, equippedCount={selectedHero.EquippedSkillIds?.Count(id => id > 0) ?? 0}");

            BindHeroTabs();
            BindEquippedSlots();

            var pool = dataProvider.GetSortedPoolForHero(selectedHero);
            selectedSkill = ValidateSelectedSkill(pool, selectedHero);

            var itemTier = EnumHelper.TierSkillToItemTier(selectedSkill.SkillTier);
            var tierInfo = ItemTierVisualImageService.GetItemTierEntry(itemTier);

            LogView(
                $"SelectedSkill -> {(selectedSkill != null ? $"{selectedSkill.SkillId}-{selectedSkill.SkillName}" : "null")}");

            RebuildGridByClass(selectedClass, selectedHero);
            BindDetail(selectedHero, selectedSkill, tierInfo);

            if (isReplaceMode)
            {
                if (selectedHero == null || pendingReplaceSkill == null)
                {
                    LogWarningView("ReplaceMode invalid state. Auto closing popup.");
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
            LogView($"ResolveSelectedHero -> activeHeroesCount={activeHeroes.Count}");

            if (selectedHero != null)
            {
                var matched = activeHeroes.FirstOrDefault(x => x != null && x.HeroId == selectedHero.HeroId);
                if (matched != null)
                {
                    LogView($"ResolveSelectedHero -> keep current heroId={matched.HeroId}");
                    return matched;
                }

                LogWarningView($"Previously selected heroId={selectedHero.HeroId} is no longer active.");
            }

            var heroByClass = dataProvider.GetAssignedHeroByClass(selectedClass);
            if (heroByClass != null)
            {
                LogView($"ResolveSelectedHero -> fallback by class heroId={heroByClass.HeroId}");
                return heroByClass;
            }

            var first = activeHeroes.FirstOrDefault();
            if (first != null)
                LogWarningView($"ResolveSelectedHero -> fallback to first active heroId={first.HeroId}");

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
                if (equipButtonText != null) equipButtonText.text = "Trang bị";
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

            if (detailLevelText != null) detailLevelText.text = $"Cấp.{state.Level}";
            if (detailNameText != null) detailNameText.text = skillData.SkillName;
            //if (detailTypeText != null) detailTypeText.text = $"{skillData.CastType} kỹ năng";
            if (detailDescText != null) detailDescText.text = skillData.BuildDescription(state.Level);
            if (detailShardText != null) detailShardText.text = $"{state.CurrentShard}/{state.RequiredShard}";
            if (detailShardFill != null)
                detailShardFill.fillAmount =
                    state.RequiredShard > 0 ? (float)state.CurrentShard / state.RequiredShard : 0f;

            if (!state.IsOwned)
            {
                if (equipButton != null) equipButton.interactable = false;
                if (equipButtonText != null) equipButtonText.text = "Trang bị";
                return;
            }

            if (equipButton != null) equipButton.interactable = hero != null;
            if (equipButtonText != null) equipButtonText.text = state.IsEquipped ? "Tháo trang bị" : "Trang bị";
        }

        private void OnClickEquipOrUnequip()
        {
            if (selectedHero == null)
            {
                LogErrorView("OnClickEquipOrUnequip failed because selectedHero is null.");
                return;
            }

            if (selectedSkill == null)
            {
                LogErrorView("OnClickEquipOrUnequip failed because selectedSkill is null.");
                return;
            }

            var state = dataProvider.BuildSkillState(selectedHero, selectedSkill);
            if (state == null)
            {
                LogErrorView($"BuildSkillState returned null for heroId={selectedHero.HeroId}, skillId={selectedSkill.SkillId}");
                return;
            }

            LogView($"OnClickEquipOrUnequip -> heroId={selectedHero.HeroId}, skillId={selectedSkill.SkillId}, isOwned={state.IsOwned}, isEquipped={state.IsEquipped}");

            if (!state.IsOwned)
            {
                LogWarningView($"Skill {selectedSkill.SkillId} is not owned. Equip ignored.");
                return;
            }

            if (state.IsEquipped)
            {
                bool success = dataProvider.TryUnequipSkillFromHero(selectedHero, selectedSkill.SkillId);
                LogView($"Unequip result -> success={success}");
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
            LogView($"Open replace mode -> heroId={selectedHero.HeroId}, pendingSkillId={pendingReplaceSkill.SkillId}");
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
                replaceInstructionText.text = "Vui lòng chọn ô để trang bị.";

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
                LogWarningView("OnClickReplaceSlot ignored because not in replace mode.");
                return;
            }

            if (selectedHero == null)
            {
                LogErrorView("OnClickReplaceSlot failed because selectedHero is null.");
                return;
            }

            if (pendingReplaceSkill == null)
            {
                LogErrorView("OnClickReplaceSlot failed because pendingReplaceSkill is null.");
                return;
            }

            LogView($"OnClickReplaceSlot -> heroId={selectedHero.HeroId}, slotIndex={slotIndex}, newSkillId={pendingReplaceSkill.SkillId}");

            bool success = await dataProvider.TryReplaceSkillOnHero(selectedHero, slotIndex, pendingReplaceSkill.SkillId);
            if (!success)
            {
                LogWarningView($"Replace failed -> heroId={selectedHero.HeroId}, slotIndex={slotIndex}, skillId={pendingReplaceSkill.SkillId}");
                return;
            }

            selectedSkill = pendingReplaceSkill;
            LogView($"Replace success -> selectedSkillId={selectedSkill.SkillId}");

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