using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace Immortal_Switch.Scripts.Localization.Components
{
    /// <summary>
    /// Component tự động cập nhật text theo ngôn ngữ hiện tại dùng Unity Localization.
    /// Gán tableName + key trong Inspector, tự động cập nhật khi đổi ngôn ngữ runtime.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizeText : MonoBehaviour
    {
        [SerializeField]
        private string localizationKey;

        [SerializeField]
        private TextMeshProUGUI txtLabel;

        // --- Private Fields ---
        private LocalizedString _localizedString;

        private void Awake()
        {
            if (txtLabel == null)
            {
                txtLabel = GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(localizationKey))
            {
                return;
            }

            _localizedString = new LocalizedString(LocalizationManager.TABLE_NAME, localizationKey);
            _localizedString.StringChanged += OnStringChanged;
        }

        private void OnDisable()
        {
            if (_localizedString != null)
            {
                _localizedString.StringChanged -= OnStringChanged;
                _localizedString = null;
            }
        }

        private void OnStringChanged(string value)
        {
            if (txtLabel != null)
            {
                txtLabel.text = value;
            }
        }

        /// <summary>
        /// Gán key mới và cập nhật text ngay lập tức.
        /// </summary>
        public void SetKey(string key)
        {
            localizationKey = key;

            if (_localizedString != null)
            {
                _localizedString.StringChanged -= OnStringChanged;
            }

            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _localizedString = new LocalizedString(LocalizationManager.TABLE_NAME, key);
            _localizedString.StringChanged += OnStringChanged;
        }
    }
}