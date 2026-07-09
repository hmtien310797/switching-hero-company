using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Immortal_Switch.Scripts.Skill;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class HeroSkillDetailUI : MonoBehaviour
    {
        [SerializeField] private Image skillUI;
        [SerializeField] private TMP_Text skillNameAndLevel;
        [SerializeField] private TMP_Text skillDescription;
        [SerializeField] private Button skillButton;
        
        private SkillDataSO _skillData;

        public void Bind(int currentLevel, SkillDataSO skillData, Action<SkillDataSO, int> skillButtonCallback)
        {
            _skillData = skillData;
            skillUI.sprite = _skillData.SkillIcon;
            skillNameAndLevel.text = $"{_skillData.SkillName} Lv.{currentLevel}";
            skillDescription.text = skillData.BuildDescription(currentLevel);
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(() => skillButtonCallback(_skillData, currentLevel));
        }
    }
}