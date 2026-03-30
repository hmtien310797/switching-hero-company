using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Skill.UI
{
    public class SkillItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject selected;
        [SerializeField] private GameObject equippedTag;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private Image progress;

        private int skillId;

        public Button Button => button;
        public int SkillId => skillId;

        public void Setup(
            int skillId,
            Sprite iconSprite,
            bool isEquipped,
            bool isOwned,
            int shard,
            int shardMax,
            bool isSelected)
        {
            this.skillId = skillId;

            icon.sprite = iconSprite;
            equippedTag.SetActive(isEquipped);
            lockedOverlay.SetActive(!isOwned);

            shardText.text = $"{shard}/{shardMax}";
            progress.fillAmount = shardMax > 0 ? (float)shard / shardMax : 0;

            selected.SetActive(isSelected);
        }

        public void SetSelected(bool value)
        {
            selected.SetActive(value);
        }
    }
}