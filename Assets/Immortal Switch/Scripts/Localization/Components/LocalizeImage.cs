using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Immortal_Switch.Scripts.Localization.Components
{
    /// <summary>
    /// Component tự động đổi sprite theo ngôn ngữ hiện tại.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LocalizeImage : MonoBehaviour
    {
        [System.Serializable]
        public class LanguageSpriteEntry
        {
            [ValueDropdown("@GetAllLangCodes()")]
            public string langCode;

            [PreviewField]
            public Sprite sprite;

            private IEnumerable<string> GetAllLangCodes()
            {
                var locales = LocalizationSettings.AvailableLocales?.Locales;
                return locales == null ? new List<string>() : new List<string>(locales.Select(t => t.Identifier.Code));
            }
        }

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = true)]
        [OnInspectorInit(nameof(AutoPopulateEntries))]
        private List<LanguageSpriteEntry> sprites = new();

        [SerializeField]
        [Required]
        private Image imgTarget;

        private void Awake()
        {
            if (imgTarget == null)
            {
                imgTarget = GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            ApplySprite();
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void OnLanguageChanged(string langCode)
        {
            ApplySprite();
        }

        public void ApplySprite()
        {
            if (imgTarget == null ||
                sprites.Count == 0)
            {
                return;
            }

            var currentCode = LocalizationManager.Instance.CurrentLangCode;

            foreach (var entry in sprites)
            {
                if (entry.langCode == currentCode &&
                    entry.sprite != null)
                {
                    imgTarget.sprite = entry.sprite;
                    return;
                }
            }

            // Fallback: sprite đầu tiên có
            foreach (var entry in sprites)
            {
                if (entry.sprite != null)
                {
                    imgTarget.sprite = entry.sprite;
                    return;
                }
            }
        }

#if UNITY_EDITOR
        private void AutoPopulateEntries()
        {
            if (sprites.Count > 0)
            {
                return;
            }

            var locales = LocalizationSettings.AvailableLocales?.Locales;

            if (locales == null)
            {
                return;
            }

            foreach (var x in locales)
            {
                sprites.Add(new LanguageSpriteEntry { langCode = x.Identifier.Code });
            }
        }
#endif
    }
}