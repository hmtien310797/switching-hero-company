using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Shared.Constants;
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
        private int _currentLevel;

        public void Bind(int currentLevel, SkillDataSO skillData, Action<SkillDataSO, int> skillButtonCallback)
        {
            _skillData = skillData;
            _currentLevel = _skillData != null
                ? _skillData.GetSafeLevel(currentLevel)
                : currentLevel;
            skillUI.sprite = SkillImageService.GetSkillIcon(_skillData);
            RefreshText();
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(() => skillButtonCallback(_skillData, _currentLevel));
        }

        private void RefreshText()
        {
            if (_skillData == null)
                return;

            skillNameAndLevel.text = $"{_skillData.GetLocalizedSkillName()} {LocalizationManager.GetText(LocalizationKeys.UI_LV, _currentLevel)}";
            skillDescription.text = _skillData.GetDisplayDescription(_currentLevel);
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(string langCode)
        {
            RefreshText();
        }
    }
}
