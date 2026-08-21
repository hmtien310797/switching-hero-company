using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Skill;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Immortal_Switch.Scripts.Localization
{
    /// <summary>
    /// Quản lý localization dùng Unity Localization.
    /// Lưu ngôn ngữ hiện tại, fire OnLanguageChanged để các component cập nhật.
    /// </summary>
    public class LocalizationManager : Singleton<LocalizationManager>
    {
        [ValueDropdown("@GetAllLangCodes()")]
        [SerializeField]
        private string defaultLangCode = "en";

        private List<string> GetAllLangCodes()
        {
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            return locales == null ? new List<string>() : locales.Select(v => v.Identifier.Code).ToList();
        }

        /// <summary>
        /// Fire khi ngôn ngữ thay đổi runtime. Các component LocalizeText / LocalizeImage
        /// subscribe event này để tự động cập nhật.
        /// </summary>
        public static event Action<string> OnLanguageChanged;

        private bool _isListeningLocaleChanges;
        private int _localeChangeVersion;
        private bool _suppressLocaleNotifications;
        private bool _isLocalizationInitialized;

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
                return locale != null ? locale.Identifier.Code : defaultLangCode;
            }
        }

        /// <summary>
        /// Danh sách các String Table cần preload trước khi vào game.
        /// </summary>
        private readonly List<string> _preloadTables = new()
        {
            TABLE_NAME,
        };

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            SubscribeLocaleChanges();
        }

        public override async UniTask InitializeAsync()
        {
            SubscribeLocaleChanges();

            await LocalizationSettings.InitializationOperation.ToUniTask();

            ApplySavedLocale();
            EnsureSelectedLocale();

            await PreloadTablesAsync();

            _isLocalizationInitialized = true;
        }

        /// <summary>
        /// Giải phóng các String Table đang giữ bundle cũ trước khi Addressables.UpdateCatalogs.
        /// Bắt buộc gọi trước pipeline update catalog khi app đã từng load Localization.
        /// Lần mở app đầu tiên chưa có SelectedLocale thì hàm tự bỏ qua.
        /// </summary>
        public async UniTask PrepareForRemoteCatalogUpdateAsync(
            CancellationToken cancellationToken = default)
        {
            // Lần đầu mở app chưa có Localization cũ để release.
            // Tuyệt đối không đọc SelectedLocale khi chưa initialize.
            if (!_isLocalizationInitialized)
            {
                Debug.Log(
                    "[LocalizationManager] Localization chưa initialize, " +
                    "bỏ qua release table cũ.");

                return;
            }

            var locale = LocalizationSettings.SelectedLocale;

            if (locale == null)
            {
                return;
            }

            _suppressLocaleNotifications = true;

            try
            {
                foreach (var tableName in _preloadTables)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    LocalizationSettings.StringDatabase.ReleaseTable(
                        tableName,
                        locale);
                }

                LocalizationSettings.StringDatabase.ResetState();
                LocalizationSettings.AssetDatabase.ResetState();

                _isLocalizationInitialized = false;

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    cancellationToken);

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    cancellationToken);

                await Resources.UnloadUnusedAssets()
                    .ToUniTask(cancellationToken: cancellationToken);
            }
            catch
            {
                _suppressLocaleNotifications = false;
                throw;
            }
        }

        /// <summary>
        /// Gọi sau khi Addressables catalog/bundle localization đã được cập nhật.
        /// Áp lại locale đã lưu, preload table từ catalog mới và refresh UI.
        /// </summary>
        public async UniTask ReloadRemoteLocalizationAsync(
            CancellationToken cancellationToken = default)
        {
            SubscribeLocaleChanges();

            try
            {
                await LocalizationSettings.InitializationOperation.ToUniTask(
                    cancellationToken: cancellationToken);

                ApplySavedLocale();
                EnsureSelectedLocale();

                await PreloadTablesAsync(cancellationToken);

                _isLocalizationInitialized = true;
            }
            finally
            {
                _suppressLocaleNotifications = false;
            }

            OnLanguageChanged?.Invoke(CurrentLangCode);

            Debug.Log(
                $"[LocalizationManager] Remote localization applied. " +
                $"Locale={CurrentLangCode}");
        }

        private void EnsureSelectedLocale()
        {
            if (LocalizationSettings.SelectedLocale != null)
            {
                return;
            }

            var locales = LocalizationSettings.AvailableLocales.Locales;

            if (locales == null ||
                locales.Count == 0)
            {
                throw new InvalidOperationException(
                    "[LocalizationManager] No runtime Locale is available. " +
                    "Keep Localization-Locales in a Local group with the Locale label, then rebuild Addressables/Player.");
            }

            LocalizationSettings.SelectedLocale = locales[0];
        }

        private void ApplySavedLocale()
        {
            if (SettingManager.Instance == null)
            {
                return;
            }

            var savedLang = SettingManager.Instance.CurrentSetting.LangCode;

            if (string.IsNullOrWhiteSpace(savedLang))
            {
                savedLang = defaultLangCode;
                SettingManager.Instance.SetLangCode(savedLang);
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(savedLang);

            if (locale != null &&
                !locale.Equals(LocalizationSettings.SelectedLocale))
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        protected override void OnDestroy()
        {
            if (_isListeningLocaleChanges)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
                _isListeningLocaleChanges = false;
            }

            base.OnDestroy();
        }

        private void SubscribeLocaleChanges()
        {
            if (_isListeningLocaleChanges)
            {
                return;
            }

            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            _isListeningLocaleChanges = true;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            if (_suppressLocaleNotifications || locale == null)
            {
                return;
            }

            var changeVersion = ++_localeChangeVersion;
            NotifyLanguageChangedAsync(locale, changeVersion).Forget();
        }

        private async UniTaskVoid NotifyLanguageChangedAsync(Locale locale, int changeVersion)
        {
            await PreloadTablesAsync();

            if (changeVersion != _localeChangeVersion)
            {
                return;
            }

            OnLanguageChanged?.Invoke(locale.Identifier.Code);
        }

        private async UniTask PreloadTablesAsync(
            CancellationToken cancellationToken = default)
        {
            foreach (var tableName in _preloadTables)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tableOp = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
                await tableOp.ToUniTask(cancellationToken: cancellationToken);

                if (tableOp.Status == AsyncOperationStatus.Succeeded)
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
        public static void SetLanguage(string langCode)
        {
            if (string.IsNullOrEmpty(langCode))
            {
                return;
            }

            if (Instance == null ||
                Instance.CurrentLangCode == langCode)
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
        public static string GetText(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            var operation = LocalizationSettings.StringDatabase
                .GetTableEntryAsync(TABLE_NAME, key);

            if (!operation.IsDone ||
                operation.Result.Entry == null)
            {
                return key;
            }

            var localizedText = operation.Result.Entry.GetLocalizedString(args);

            return string.IsNullOrEmpty(localizedText)
                ? key
                : localizedText;
        }

        /// <summary>
        /// Lấy raw localized value (template chưa format, giữ nguyên <c>{0}</c>, <c>{1}</c>...)
        /// từ String Table <see cref="TABLE_NAME"/> tại thời điểm runtime.
        ///
        /// Dùng cho caller cần tự format template (ví dụ <see cref="SkillDataSO"/>).
        /// Trả về <c>false</c> nếu:
        /// - key rỗng.
        /// - Localization chưa initialize / table chưa preload (operation chưa done).
        /// - entry không tồn tại.
        /// - giá trị rỗng.
        ///
        /// Không throw, không spam log mỗi frame.
        /// </summary>
        public static bool TryGetRawText(string key, out string localizedValue)
        {
            localizedValue = null;

            if (string.IsNullOrWhiteSpace(key))
                return false;

            var operation = LocalizationSettings.StringDatabase
                .GetTableEntryAsync(TABLE_NAME, key);

            if (!operation.IsDone ||
                operation.Result.Entry == null)
                return false;

            localizedValue = operation.Result.Entry.Value;

            return !string.IsNullOrEmpty(localizedValue);
        }
    }
}