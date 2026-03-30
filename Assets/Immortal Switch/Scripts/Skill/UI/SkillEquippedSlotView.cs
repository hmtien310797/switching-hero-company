using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillEquippedSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject emptyObject;
        [SerializeField] private GameObject selectedObject;

        private int slotIndex;
        private int skillId = -1;

        public Button Button => button;
        public int SlotIndex => slotIndex;
        public int SkillId => skillId;

        public void Setup(int slotIndex, int skillId, Sprite iconSprite, bool isSelected)
        {
            this.slotIndex = slotIndex;
            this.skillId = skillId;

            bool hasSkill = skillId > 0;

            if (icon != null)
            {
                icon.gameObject.SetActive(hasSkill);
                if (hasSkill)
                    icon.sprite = iconSprite;
            }

            if (emptyObject != null)
                emptyObject.SetActive(!hasSkill);

            SetSelected(isSelected);
            gameObject.SetActive(true);
        }

        public void SetSelected(bool value)
        {
            if (selectedObject != null)
                selectedObject.SetActive(value && skillId > 0);
        }
    }
}