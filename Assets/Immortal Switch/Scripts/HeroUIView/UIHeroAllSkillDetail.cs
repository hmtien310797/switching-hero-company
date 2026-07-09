using Immortal_Switch.Scripts.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class UIHeroAllSkillDetail : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] descriptionLevels;
        [SerializeField] private GameObject[] selectionPanels;
        [SerializeField] private TMP_Text skillNameTmpText;
        [SerializeField] private TMP_Text skillLevelTmpText;
        [SerializeField] private Button button;

        private void Start()
        {
            button.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Show(SkillDataSO skillData, int level)
        {
            for (int i = 0; i < descriptionLevels.Length; i++)
            {
                int currentLevel = i + 1;
                descriptionLevels[i].text = skillData.BuildDescription(currentLevel);
            }
            
            for (int i = 0; i < selectionPanels.Length; i++)
            {
                selectionPanels[i].SetActive(false);
            }
            
            selectionPanels[level - 1].SetActive(true);
            skillNameTmpText.text = skillData.SkillName;
            skillLevelTmpText.text = $"Lv.{level}";
            gameObject.SetActive(true);
        }
    }
}