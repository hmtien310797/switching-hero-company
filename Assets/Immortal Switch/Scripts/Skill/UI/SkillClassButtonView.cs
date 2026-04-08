using Immortal_Switch.Scripts.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillClassButtonView : MonoBehaviour
    {
        [SerializeField] private HeroClass heroClass;
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selectedObject;
        [SerializeField] private GameObject equippedBadgeE;

        public HeroClass HeroClass => heroClass;
        public Button Button => button;

        public void Setup(Sprite iconSprite, bool isAssigned, bool isSelected)
        {
            if (icon != null)
                icon.sprite = iconSprite;

            SetAssigned(isAssigned);
            SetSelected(isSelected);
        }

        public void SetSelected(bool value)
        {
            if (selectedObject != null)
                selectedObject.SetActive(value);
        }

        public void SetAssigned(bool value)
        {
            if (equippedBadgeE != null)
                equippedBadgeE.SetActive(value);
        }
    }
}