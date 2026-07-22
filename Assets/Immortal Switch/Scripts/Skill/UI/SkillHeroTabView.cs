using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillHeroTabView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image iconClass;
        [SerializeField] private GameObject selectedObject;

        private int heroId = -1;

        public Button Button => button;
        public int HeroId => heroId;

        public void Setup(int heroId, Sprite iconSprite, Sprite classIcon, bool isSelected)
        {
            this.heroId = heroId;

            if (icon != null)
                icon.sprite = iconSprite;
            
            if(iconClass != null)
                iconClass.sprite = classIcon;

            SetSelected(isSelected);
            gameObject.SetActive(true);
        }

        public void SetSelected(bool value)
        {
            if (selectedObject != null)
                selectedObject.SetActive(value);
        }

        public void Hide()
        {
            heroId = -1;
            gameObject.SetActive(false);
        }
    }
}