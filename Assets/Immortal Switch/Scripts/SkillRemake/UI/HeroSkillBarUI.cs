using System;
using System.Collections.Generic;
using Battle;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Skill;
using Immortal_Switch.Scripts.Tutorial;
using UnityEngine;
using UnityEngine.Events;

namespace Immortal_Switch.Scripts.UI.Skill
{
    [Serializable]
    public sealed class HeroSkillEquipRequestEvent : UnityEvent<HeroActor, int>
    {
    }

    public sealed class HeroSkillBarUI : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private List<HeroSkillDisplay> classSkillSlots = new();
        [SerializeField] private HeroSkillDisplay ultimateSkillSlot;

        [Header("Fallback Sprites")]
        [SerializeField] private Sprite emptyClassSkillSprite;
        [SerializeField] private Sprite missingUltimateSprite;

        [Header("Behaviour")]
        [SerializeField] private bool updateCooldownEveryFrame = true;
        [SerializeField] private bool logClickResult = true;

        [Header("Events")]
        [SerializeField] private HeroSkillEquipRequestEvent onRequestEquipClassSkill = new();

        public event Action<HeroActor, int> RequestEquipClassSkill;
        public event Action<HeroActor> BoundHeroChanged;

        private HeroActor currentHero;
        private HeroSkillController currentSkillController;

        public HeroActor CurrentHero => currentHero;
        public HeroSkillController CurrentSkillController => currentSkillController;

        private void Awake()
        {
            TutorialManager.Instance.OnResolveTarget += OnResolveTarget;
            TutorialManager.Instance.OnClick += OnClickTutorial;
            BindSlotClickEvents();
        }

        private UniTask OnClickTutorial(string arg1, int arg2)
        {
            // step 5
            if (arg2 == 5)
            {
                HandleSlotClicked(classSkillSlots[0]);
            }

            return UniTask.CompletedTask;
        }

        private RectTransform OnResolveTarget(string arg1, int arg2)
        {
            // step 5
            if (arg2 == 5)
            {
                return classSkillSlots[0].transform as RectTransform;
            }

            return null;
        }

        private void OnEnable()
        {
            BindControllerEvent();
            Refresh();
        }

        private void OnDisable()
        {
            UnbindControllerEvent();
        }

        private void OnDestroy()
        {
            TutorialManager.Instance.OnResolveTarget -= OnResolveTarget;
            TutorialManager.Instance.OnClick -= OnClickTutorial;
        }

        private void Update()
        {
            if (!updateCooldownEveryFrame)
                return;

            RefreshCooldowns();
        }

        public void BindHero(HeroActor hero)
        {
            if (currentHero == hero)
            {
                Refresh();
                return;
            }

            UnbindControllerEvent();

            currentHero = hero;
            currentSkillController = hero != null ? hero.GetComponent<HeroSkillController>() : null;

            BindControllerEvent();
            Refresh();
            BoundHeroChanged?.Invoke(currentHero);
        }

        public void Clear()
        {
            BindHero(null);
        }

        public void Refresh()
        {
            RefreshSlots();
            RefreshCooldowns();
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < classSkillSlots.Count; i++)
            {
                HeroSkillDisplay slot = classSkillSlots[i];
                if (slot == null)
                    continue;

                SkillDataSO skill = currentSkillController != null ? currentSkillController.GetClassSkillAt(i) : null;
                slot.BindClassSkill(i, skill, emptyClassSkillSprite);
                slot.SetLocked(currentSkillController == null);
                slot.SetInteractable(currentSkillController != null && currentHero != null);
            }

            if (ultimateSkillSlot != null)
            {
                SkillDataSO ultimateSkill = currentSkillController != null ? currentSkillController.GetUltimateSkill() : null;
                ultimateSkillSlot.BindUltimateSkill(ultimateSkill, missingUltimateSprite);
                ultimateSkillSlot.SetLocked(currentSkillController == null || ultimateSkill == null);
                ultimateSkillSlot.SetInteractable(currentSkillController != null && ultimateSkill != null);
            }
        }

        private void RefreshCooldowns()
        {
            if (currentSkillController == null)
            {
                ClearCooldowns();
                return;
            }

            for (int i = 0; i < classSkillSlots.Count; i++)
            {
                HeroSkillDisplay slot = classSkillSlots[i];
                if (slot == null)
                    continue;

                float remaining = currentSkillController.GetClassSkillCooldownRemaining(i);
                float duration = currentSkillController.GetClassSkillCooldownDuration(i);
                slot.SetCooldown(remaining, duration);
            }

            if (ultimateSkillSlot != null)
            {
                ultimateSkillSlot.SetCooldown(
                    currentSkillController.GetUltimateCooldownRemaining(),
                    currentSkillController.GetUltimateCooldownDuration());
            }
        }

        private void ClearCooldowns()
        {
            for (int i = 0; i < classSkillSlots.Count; i++)
            {
                if (classSkillSlots[i] != null)
                    classSkillSlots[i].SetCooldown(0f, 0f);
            }

            if (ultimateSkillSlot != null)
                ultimateSkillSlot.SetCooldown(0f, 0f);
        }

        private void BindSlotClickEvents()
        {
            for (int i = 0; i < classSkillSlots.Count; i++)
            {
                HeroSkillDisplay slot = classSkillSlots[i];
                if (slot == null)
                    continue;

                slot.Clicked -= HandleSlotClicked;
                slot.Clicked += HandleSlotClicked;
            }

            if (ultimateSkillSlot != null)
            {
                ultimateSkillSlot.Clicked -= HandleSlotClicked;
                ultimateSkillSlot.Clicked += HandleSlotClicked;
            }
        }

        private void HandleSlotClicked(HeroSkillDisplay display)
        {
            if (display == null || currentHero == null || currentSkillController == null)
                return;

            if (display.SlotKind == HeroSkillSlotKind.ClassSkill)
            {
                HandleClassSkillSlotClicked(display);
                return;
            }

            HandleUltimateSlotClicked();
        }

        private void HandleClassSkillSlotClicked(HeroSkillDisplay display)
        {
            if (BattleFlowController.Instance.IsDungeonLocked)
            {
                return;
            }
            
            int slotIndex = display.SlotIndex;

            if (display.IsEmptyClassSlot)
            {
                RequestEquipClassSkill?.Invoke(currentHero, slotIndex);
                onRequestEquipClassSkill?.Invoke(currentHero, slotIndex);

                EquipViewData equipViewData = new EquipViewData()
                {
                    Type = EquipViewType.SkillView,
                    Data1 = currentHero.HeroClass
                };

                UIManager.Instance.TogglePopupAsync<EquipView>(args:equipViewData).Forget();
                if (logClickResult)
                    Debug.Log($"[HeroSkillBarUI] Request equip class skill. Hero={currentHero.name}, Slot={slotIndex}", this);

                return;
            }

            currentSkillController.TryCastClassSkillAtAsync(slotIndex).Forget();
        }

        private void HandleUltimateSlotClicked()
        {
            currentSkillController.TryCastUltimateAsync().Forget();
        }

        private void BindControllerEvent()
        {
            if (currentSkillController == null)
                return;

            currentSkillController.SkillsChanged -= HandleSkillsChanged;
            currentSkillController.SkillsChanged += HandleSkillsChanged;
        }

        private void UnbindControllerEvent()
        {
            if (currentSkillController == null)
                return;

            currentSkillController.SkillsChanged -= HandleSkillsChanged;
        }

        private void HandleSkillsChanged(HeroSkillController controller)
        {
            if (controller != currentSkillController)
                return;

            Refresh();
        }
    }
}
