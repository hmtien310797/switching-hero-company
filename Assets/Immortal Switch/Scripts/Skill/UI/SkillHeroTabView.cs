using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillHeroTabView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selected;

        private int heroId;

        public Button Button => button;
        public int HeroId => heroId;

        public void Setup(int heroId, Sprite iconSprite, bool isSelected)
        {
            this.heroId = heroId;
            icon.sprite = iconSprite;
            selected.SetActive(isSelected);
        }

        public void SetSelected(bool value)
        {
            selected.SetActive(value);
        }
    }
}