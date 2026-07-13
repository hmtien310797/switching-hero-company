using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Shared.Constants;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Immortal_Switch.Scripts.Localization
{
    /// <summary>
    /// Quản lý localization dùng Unity Localization.
    /// Lưu ngôn ngữ hiện tại, fire OnLanguageChanged để các component cập nhật.
    /// </summary>
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        /// <summary>
        /// Fire khi ngôn ngữ thay đổi runtime. Các component LocalizeText / LocalizeImage
        /// subscribe event này để tự động cập nhật.
        /// </summary>
        public event Action<string> OnLanguageChanged;

        /// <summary>
        /// Tên String Table mặc định cho Default text.
        /// </summary>
        public const string TABLE_NAME = "Default";

        /// <summary>
        /// Mã ngôn ngữ hiện tại (ví dụ: "vi", "en", "ko", "ja").
        /// </summary>
        public string CurrentLangCode
        {
            get
            {
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : ValueConstants.DEFAULT_LANGUAGE;
            }
        }

        /// <summary>
        /// Danh sách các String Table cần preload trước khi vào game.
        /// </summary>
        private readonly List<string> _preloadTables = new()
        {
            TABLE_NAME,
        };

        public override async UniTask InitializeAsync()
        {
            // Chờ Unity Localization init xong
            await LocalizationSettings.InitializationOperation.ToUniTask();

            // Áp dụng ngôn ngữ đã lưu trong SettingManager
            if (SettingManager.Instance != null)
            {
                var savedLang = SettingManager.Instance.CurrentSetting.LangCode;
                var locale = LocalizationSettings.AvailableLocales.GetLocale(savedLang);

                if (locale != null &&
                    !locale.Equals(LocalizationSettings.SelectedLocale))
                {
                    LocalizationSettings.SelectedLocale = locale;
                }
            }

            // Preload các String Table cần thiết
            await PreloadTablesAsync();

            // Subscribe sau khi đã init xong để tránh fire event lúc đang load
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        }

        protected override void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            base.OnDestroy();
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            OnLanguageChanged?.Invoke(locale.Identifier.Code);
        }

        private async UniTask PreloadTablesAsync()
        {
            foreach (var tableName in _preloadTables)
            {
                var tableOp = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
                await tableOp.ToUniTask();

                if (tableOp.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    UnityEngine.Debug.Log($"[LocalizationManager] Preloaded table: {tableName}");
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[LocalizationManager] Failed to preload table: {tableName}");
                }
            }
        }

        /// <summary>
        /// Đổi ngôn ngữ runtime. Gọi từ SettingManager khi người dùng chọn ngôn ngữ mới.
        /// </summary>
        public void SetLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode))
            {
                return;
            }

            if (CurrentLangCode == langCode)
            {
                return;
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(langCode);

            if (locale != null)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[LocalizationManager] Không tìm thấy locale '{langCode}'");
            }
        }

        /// <summary>
        /// Lấy text đã dịch từ String Table.
        /// Dùng khi cần lấy text một lần (không auto-update).
        /// tableName mặc định là "UI" nếu để trống.
        /// </summary>
        public string GetText(string key, string tableName = TABLE_NAME)
        {
            if (string.IsNullOrEmpty(key))
            {
                return key;
            }

            var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, key);
            return op.IsDone ? op.Result : key;
        }
    }
}