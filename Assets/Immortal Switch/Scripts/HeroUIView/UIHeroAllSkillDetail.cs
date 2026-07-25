using Immortal_Switch.Scripts.Addressable;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.Skill;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.HeroUIView
{
    public class UIHeroAllSkillDetail : MonoBehaviour
    {
        [SerializeField] private Image skillIcon;
        [SerializeField] private TMP_Text[] descriptionLevels;
        [SerializeField] private GameObject[] selectionPanels;
        [SerializeField] private TMP_Text skillNameTmpText;
        [SerializeField] private TMP_Text skillLevelTmpText;
        [SerializeField] private Button button;

        private SkillDataSO _skillData;
        private int _level;

        private void Start()
        {
            button.onClick.AddListener(() => gameObject.SetActive(false));
        }

        public void Show(SkillDataSO skillData, int level)
        {
            _skillData = skillData;
            _level = level;
            skillIcon.sprite = SkillImageService.GetSkillIcon(skillData);
            Refresh();
            gameObject.SetActive(true);
        }

        private void Refresh()
        {
            if (_skillData == null)
                return;

            for (int i = 0; i < descriptionLevels.Length; i++)
            {
                int displayLevel = i + 1;
                descriptionLevels[i].text = _skillData.GetDisplayDescription(displayLevel);
            }

            if (selectionPanels != null)
            {
                for (int i = 0; i < selectionPanels.Length; i++)
                {
                    selectionPanels[i].SetActive(false);
                }

                if (selectionPanels.Length > 0)
                {
                    int safeIndex = Mathf.Clamp(
                        _level - 1,
                        0,
                        selectionPanels.Length - 1);

                    selectionPanels[safeIndex].SetActive(true);
                }
            }

            skillNameTmpText.text = _skillData.GetLocalizedSkillName();
            skillLevelTmpText.text = $"Lv.{_level}";
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
            Refresh();
        }
    }
}
