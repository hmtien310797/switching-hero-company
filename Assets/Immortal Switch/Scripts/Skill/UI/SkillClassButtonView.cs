using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillClassButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selected;
        [SerializeField] private GameObject equippedBadge;

        public Button Button => button;

        public void Setup(Sprite sprite, bool isEquipped, bool isSelected)
        {
            icon.sprite = sprite;
            equippedBadge.SetActive(isEquipped);
            selected.SetActive(isSelected);
        }

        public void SetSelected(bool value)
        {
            selected.SetActive(value);
        }

        public void SetEquipped(bool value)
        {
            equippedBadge.SetActive(value);
        }
    }
}