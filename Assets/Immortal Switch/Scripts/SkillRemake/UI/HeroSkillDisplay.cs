using System;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Skill;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.UI.Skill
{
    public sealed class HeroSkillDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private GameObject emptyVisual;
        [SerializeField] private GameObject lockedVisual;

        [Header("Fallback")]
        [SerializeField] private Sprite emptyClassSkillSprite;
        [SerializeField] private Sprite missingSkillSprite;

        public event Action<HeroSkillDisplay> Clicked;

        public HeroSkillSlotKind SlotKind { get; private set; }
        public int SlotIndex { get; private set; } = -1;
        public SkillDataSO SkillData { get; private set; }
        public bool IsEmptyClassSlot { get; private set; }

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }

            SetCooldown(0f, 0f);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        public void BindClassSkill(int slotIndex, SkillDataSO skillData, Sprite emptySprite = null)
        {
            SlotKind = HeroSkillSlotKind.ClassSkill;
            SlotIndex = slotIndex;
            SkillData = skillData;
            IsEmptyClassSlot = skillData == null;

            if (skillData == null)
            {
                Sprite sprite = emptySprite != null ? emptySprite : emptyClassSkillSprite;
                SetIcon(sprite);
                SetEmptyVisual(true);
                SetCooldown(0f, 0f);
                return;
            }

            SetIcon(GetSkillIcon(skillData));
            SetEmptyVisual(false);
        }

        public void BindUltimateSkill(SkillDataSO skillData, Sprite missingSprite = null)
        {
            SlotKind = HeroSkillSlotKind.UltimateSkill;
            SlotIndex = -1;
            SkillData = skillData;
            IsEmptyClassSlot = false;

            Sprite sprite = skillData != null ? GetSkillIcon(skillData) : (missingSprite != null ? missingSprite : missingSkillSprite);
            SetIcon(sprite);
            SetEmptyVisual(false);
        }

        public void SetCooldown(float remaining, float duration)
        {
            remaining = Mathf.Max(0f, remaining);
            duration = Mathf.Max(0f, duration);

            bool hasCooldown = duration > 0f && remaining > 0f;

            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(hasCooldown);
                cooldownOverlay.fillAmount = hasCooldown ? Mathf.Clamp01(remaining / duration) : 0f;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        public void SetLocked(bool locked)
        {
            if (lockedVisual != null)
                lockedVisual.SetActive(locked);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(this);
        }

        private void SetIcon(Sprite sprite)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        private void SetEmptyVisual(bool active)
        {
            if (emptyVisual != null)
                emptyVisual.SetActive(active);
        }

        private Sprite GetSkillIcon(SkillDataSO skillData)
        {
            if (skillData == null)
                return missingSkillSprite;

            // Adjust this property name if your SkillDataSO uses SkillIcon/Icon instead.
            return SkillImageService.GetSkillIcon(skillData);
        }
    }
}
