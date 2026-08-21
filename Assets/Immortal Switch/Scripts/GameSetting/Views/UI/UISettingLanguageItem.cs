using System;
using Immortal_Switch.Scripts.Localization;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.GameSetting.Views.UI
{
    public class UISettingLanguageItem : MonoBehaviour
    {
        [SerializeField]
        private Button btnLanguage;

        [SerializeField]
        private TextMeshProUGUI txtLanguage;

        [SerializeField]
        private Image imgBg;

        [PreviewField]
        [SerializeField]
        private Sprite sprUnselect;

        [PreviewField]
        [SerializeField]
        private Sprite sprSelect;

        // --- Private Fields ---
        private string _languageCode;
        private string _languageName;
        private Action<string> _onChangeLanguage;

        private void Awake()
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            btnLanguage.onClick.AddListener(OnClickChangeLanguage);
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
            btnLanguage.onClick.RemoveListener(OnClickChangeLanguage);
        }

        private void OnLanguageChanged(string langCode)
        {
            RefreshLocalizeText();
        }

        private void OnClickChangeLanguage()
        {
            _onChangeLanguage?.Invoke(_languageCode);
        }

        public void Bind(string languageName, string languageCode, Action<string> onChangeLanguage)
        {
            _languageName = languageName;
            _languageCode = languageCode;
            _onChangeLanguage = onChangeLanguage;

            RefreshLocalizeText();
        }

        public void SetSelected(bool selected)
        {
            imgBg.sprite = selected ? sprSelect : sprUnselect;
        }

        private void RefreshLocalizeText()
        {
            txtLanguage.text = LocalizationManager.GetText(_languageName);
        }
    }
}