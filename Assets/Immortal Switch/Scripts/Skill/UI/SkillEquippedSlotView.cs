using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillEquippedSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject emptyObj;
        [SerializeField] private GameObject selected;

        private int skillId = -1;
        private int slotIndex;

        public Button Button => button;
        public int SkillId => skillId;
        public int SlotIndex => slotIndex;

        public void Setup(int slotIndex, int skillId, Sprite iconSprite, bool isSelected)
        {
            this.slotIndex = slotIndex;
            this.skillId = skillId;

            bool hasSkill = skillId > 0;

            emptyObj.SetActive(!hasSkill);
            icon.gameObject.SetActive(hasSkill);

            if (hasSkill)
                icon.sprite = iconSprite;

            selected.SetActive(isSelected);
        }

        public void SetSelected(bool value)
        {
            selected.SetActive(value);
        }
    }
}