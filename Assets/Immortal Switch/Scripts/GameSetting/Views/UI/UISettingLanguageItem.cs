using System;
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
        private Action<string> _onChangeLanguage;

        private void Awake()
        {
            btnLanguage.onClick.AddListener(OnClickChangeLanguage);
        }

        private void OnDestroy()
        {
            btnLanguage.onClick.RemoveListener(OnClickChangeLanguage);
        }

        private void OnClickChangeLanguage()
        {
            _onChangeLanguage?.Invoke(_languageCode);
        }

        public void Bind(string languageName, string languageCode, Action<string> onChangeLanguage)
        {
            _languageCode = languageCode;
            _onChangeLanguage = onChangeLanguage;
            txtLanguage.text = languageName;
        }

        public void SetSelected(bool selected)
        {
            imgBg.sprite = selected ? sprSelect : sprUnselect;
        }
    }
}