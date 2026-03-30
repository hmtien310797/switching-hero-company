using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillReplaceSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;

        private int slotIndex;

        public Button Button => button;
        public int SlotIndex => slotIndex;

        public void Setup(int slotIndex, Sprite iconSprite)
        {
            this.slotIndex = slotIndex;
            icon.sprite = iconSprite;
        }
    }
}