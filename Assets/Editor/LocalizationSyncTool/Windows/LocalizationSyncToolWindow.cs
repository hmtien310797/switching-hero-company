using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Editor.ExcelConfigTool.Services;
using Editor.LocalizationSyncTool.Models;
using Editor.LocalizationSyncTool.Services;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

namespace Editor.LocalizationSyncTool.Windows
{
    public class LocalizationSyncToolWindow : EditorWindow
    {
        private LocalizationSyncToolSettings _settings;
        private List<LocalizationSyncUrlEntry> _entries = new();
        private CancellationTokenSource _syncCancellation;
        private Vector2 _scrollPosition;
        private bool _isRunning;
        private bool _isCancelling;
        private string _status;

        [MenuItem("Tools/Localization Sync")]
        public static void Open()
        {
            GetWindow<LocalizationSyncToolWindow>("Localization Sync");
        }

        private void OnEnable()
        {
            _settings = LocalizationSyncToolSettingsStore.Load();
            _entries = _settings.entries ?? new List<LocalizationSyncUrlEntry>();
            AddMissingCollectionMappings();
        }

        private void OnDisable()
        {
            CancelSync();
            SaveSettings();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("Localization Sync", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "CSV mặc định: Key,vi,en. Tên cột ngôn ngữ phải trùng locale code " +
                "trong String Table Collection.",
                MessageType.Info
            );

            EditorGUILayout.HelpBox(
                $"Project settings: {LocalizationSyncToolSettingsStore.SETTINGS_PATH}",
                MessageType.None
            );

            EditorGUILayout.Space(6);
            DrawUrlEntries();
            EditorGUILayout.Space(8);
            DrawCollectionField();

            _settings.keyColumnName = EditorGUILayout.TextField(
                "Key Column",
                _settings.keyColumnName
            );

            DrawLocaleMappings();
            DrawFolderPicker();

            _settings.removeMissingEntries = EditorGUILayout.Toggle(
                new GUIContent(
                    "Remove Missing Keys",
                    "Xóa key trong collection nếu key không còn tồn tại trong CSV."
                ),
                _settings.removeMissingEntries
            );

            if (_settings.removeMissingEntries &&
                _entries.Count > 1)
            {
                EditorGUILayout.HelpBox(
                    "Không nên bật Remove Missing Keys khi dùng nhiều CSV vì file import sau có thể xóa key của file trước.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(_isRunning || _entries.All(entry => string.IsNullOrWhiteSpace(entry.url))))
            {
                if (GUILayout.Button("Sync & Import Localization", GUILayout.Height(40)))
                {
                    StartSync();
                }
            }

            if (_isRunning)
            {
                EditorGUILayout.HelpBox(_status ?? "Đang xử lý...", MessageType.Info);

                using (new EditorGUI.DisabledScope(_isCancelling))
                {
                    if (GUILayout.Button("Cancel", GUILayout.Height(28)))
                    {
                        CancelSync();
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }
        }

        private void DrawUrlEntries()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Source URLs", EditorStyles.boldLabel);

            if (GUILayout.Button("+ Add URL", GUILayout.Width(100)))
            {
                _entries.Add(new LocalizationSyncUrlEntry());
                SaveSettings();
            }

            EditorGUILayout.EndHorizontal();

            for (var index = 0; index < _entries.Count; index++)
            {
                var entry = _entries[index];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{index + 1}", GUILayout.Width(28));
                entry.url = EditorGUILayout.TextField(entry.url);

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(
                        string.IsNullOrWhiteSpace(entry.fileName) ? "(auto)" : entry.fileName,
                        GUILayout.Width(240)
                    );
                }

                if (GUILayout.Button("x", GUILayout.Width(22)))
                {
                    _entries.RemoveAt(index);
                    SaveSettings();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCollectionField()
        {
            var collectionNames = LocalizationEditorSettings
                .GetStringTableCollections()
                .Select(collection => collection.TableCollectionName)
                .OrderBy(name => name)
                .ToArray();

            if (collectionNames.Length == 0)
            {
                EditorGUILayout.HelpBox("Project chưa có String Table Collection.", MessageType.Error);
                return;
            }

            var selectedIndex = Array.IndexOf(collectionNames, _settings.tableCollectionName);
            selectedIndex = Mathf.Max(0, selectedIndex);

            selectedIndex = EditorGUILayout.Popup(
                "Table Collection",
                selectedIndex,
                collectionNames
            );

            var selectedCollectionName = collectionNames[selectedIndex];

            if (!string.Equals(
                    _settings.tableCollectionName,
                    selectedCollectionName,
                    StringComparison.Ordinal))
            {
                _settings.tableCollectionName = selectedCollectionName;
                AddMissingCollectionMappings();
            }
        }

        private void DrawLocaleMappings()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Locale Column Mappings", EditorStyles.boldLabel);

            if (GUILayout.Button("+ Add Mapping", GUILayout.Width(110)))
            {
                AddLocaleMapping();
                SaveSettings();
            }

            if (GUILayout.Button("Add From Collection", GUILayout.Width(140)))
            {
                AddMissingCollectionMappings();
                SaveSettings();
            }

            EditorGUILayout.EndHorizontal();

            var availableLocales = LocalizationEditorSettings
                .GetLocales()
                .Where(locale => locale != null)
                .OrderBy(locale => locale.LocaleName)
                .ToList();

            if (availableLocales.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Localization Settings chưa có Locale. Hãy tạo Locale trước khi thêm mapping.",
                    MessageType.Warning
                );
            }

            for (var index = 0; index < _settings.localeMappings.Count; index++)
            {
                var mapping = _settings.localeMappings[index];
                EditorGUILayout.BeginHorizontal();
                DrawLocalePopup(mapping, availableLocales);
                mapping.csvColumnName = EditorGUILayout.TextField(mapping.csvColumnName);

                if (GUILayout.Button("x", GUILayout.Width(22)))
                {
                    _settings.localeMappings.RemoveAt(index);
                    SaveSettings();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox(
                "Bên trái là locale code trong Unity, bên phải là tên cột chính xác trong CSV.",
                MessageType.None
            );
        }

        private void DrawLocalePopup(
            LocalizationSyncLocaleMapping mapping,
            IReadOnlyList<UnityEngine.Localization.Locale> availableLocales
        )
        {
            if (availableLocales.Count == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(mapping.localeCode, GUILayout.Width(180));
                }

                return;
            }

            var selectedIndex = -1;

            for (var index = 0; index < availableLocales.Count; index++)
            {
                if (string.Equals(
                        availableLocales[index].Identifier.Code,
                        mapping.localeCode,
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    selectedIndex = index;
                    break;
                }
            }

            var options = availableLocales
                .Select(locale => $"{locale.LocaleName} ({locale.Identifier.Code})")
                .ToList();

            var hasMissingLocale = selectedIndex < 0 && !string.IsNullOrWhiteSpace(mapping.localeCode);

            if (hasMissingLocale)
            {
                options.Insert(0, $"Missing ({mapping.localeCode})");
                selectedIndex = 0;
            }
            else
            {
                selectedIndex = Mathf.Max(0, selectedIndex);
            }

            var newIndex = EditorGUILayout.Popup(
                selectedIndex,
                options.ToArray(),
                GUILayout.Width(220)
            );

            if (hasMissingLocale)
            {
                if (newIndex == 0)
                {
                    return;
                }

                newIndex--;
            }

            var selectedLocaleCode = availableLocales[newIndex].Identifier.Code;

            if (string.Equals(
                    mapping.localeCode,
                    selectedLocaleCode,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return;
            }

            mapping.localeCode = selectedLocaleCode;

            mapping.csvColumnName = LocalizationSyncImporter.GetDefaultCsvColumnName(
                selectedLocaleCode
            );
        }

        private void AddLocaleMapping()
        {
            _settings.localeMappings ??= new List<LocalizationSyncLocaleMapping>();

            var availableLocale = LocalizationEditorSettings
                .GetLocales()
                .Where(locale => locale != null)
                .FirstOrDefault(locale => _settings.localeMappings.All(mapping =>
                    !string.Equals(
                        mapping.localeCode,
                        locale.Identifier.Code,
                        StringComparison.OrdinalIgnoreCase
                    )
                ));

            var localeCode = availableLocale?.Identifier.Code ?? string.Empty;

            _settings.localeMappings.Add(new LocalizationSyncLocaleMapping
            {
                localeCode = localeCode,
                csvColumnName = LocalizationSyncImporter.GetDefaultCsvColumnName(localeCode),
            });
        }

        private void AddMissingCollectionMappings()
        {
            _settings.localeMappings ??= new List<LocalizationSyncLocaleMapping>();

            var collection = LocalizationEditorSettings.GetStringTableCollection(
                _settings.tableCollectionName
            );

            if (collection == null)
            {
                return;
            }

            var localeCodes = collection.StringTables
                .Where(table => table != null)
                .Select(table => table.LocaleIdentifier.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var localeCode in localeCodes)
            {
                if (_settings.localeMappings.Any(mapping =>
                        string.Equals(
                            mapping.localeCode,
                            localeCode,
                            StringComparison.OrdinalIgnoreCase
                        )))
                {
                    continue;
                }

                _settings.localeMappings.Add(new LocalizationSyncLocaleMapping
                {
                    localeCode = localeCode,
                    csvColumnName = LocalizationSyncImporter.GetDefaultCsvColumnName(localeCode),
                });
            }
        }

        private void DrawFolderPicker()
        {
            EditorGUILayout.BeginHorizontal();
            _settings.csvFolder = EditorGUILayout.TextField("CSV Folder", _settings.csvFolder);

            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                var selected = EditorUtility.OpenFolderPanel(
                    "CSV Folder",
                    Application.dataPath,
                    string.Empty
                );

                if (!string.IsNullOrWhiteSpace(selected))
                {
                    _settings.csvFolder = selected.StartsWith(
                        Application.dataPath,
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "Assets" + selected[Application.dataPath.Length..]
                        : selected;

                    _settings.csvFolder = _settings.csvFolder.Replace("\\", "/");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void StartSync()
        {
            var requests = _entries
                .Select((entry, index) => new DownloadRequest
                {
                    SourceIndex = index,
                    Url = entry.url,
                })
                .Where(request => !string.IsNullOrWhiteSpace(request.Url))
                .ToList();

            if (requests.Count == 0)
            {
                return;
            }

            SaveSettings();
            _syncCancellation?.Dispose();
            _syncCancellation = new CancellationTokenSource();
            _isRunning = true;
            _isCancelling = false;
            _status = "Đang download CSV...";
            SyncAsync(requests, _syncCancellation.Token);
        }

        private async void SyncAsync(
            IReadOnlyList<DownloadRequest> requests,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var results = await GoogleSheetDownloader.DownloadAllAsync(
                    requests,
                    _settings.csvFolder,
                    true,
                    cancellationToken
                );

                cancellationToken.ThrowIfCancellationRequested();
                var failures = results.Where(result => !result.IsSuccess).ToList();

                if (failures.Count > 0)
                {
                    var details = string.Join(
                        "\n",
                        failures.Select(failure =>
                            $"URL #{failure.SourceIndex + 1}: {failure.ErrorMessage}"
                        )
                    );

                    throw new InvalidOperationException(details);
                }

                foreach (var result in results)
                {
                    if (result.SourceIndex >= 0 &&
                        result.SourceIndex < _entries.Count)
                    {
                        _entries[result.SourceIndex].fileName = result.FileName;
                    }
                }

                SaveSettings();
                _status = "Đang import vào Unity Localization...";
                Repaint();

                var importResult = LocalizationSyncImporter.Import(
                    results.Select(result => result.FilePath).ToList(),
                    _settings.tableCollectionName,
                    _settings.keyColumnName,
                    _settings.localeMappings,
                    _settings.removeMissingEntries
                );

                FinishSync();
                var locales = string.Join(", ", importResult.LocaleCodes);
                var duplicateSummary = BuildDuplicateSummary(importResult.DuplicateKeys);

                Debug.Log(
                    $"[LocalizationSync] Imported {importResult.ImportedFileCount} source files, " +
                    $"{importResult.KeyCount} keys, locales: {locales}. " +
                    $"Generated {importResult.GeneratedConstantCount} constants."
                );

                if (importResult.DuplicateKeys.Count > 0)
                {
                    Debug.LogWarning(
                        $"[LocalizationSync] Duplicate keys ({importResult.DuplicateKeys.Count}):\n" +
                        string.Join(
                            "\n",
                            importResult.DuplicateKeys.Select(item => $"{item.Key} (x{item.Count})")
                        )
                    );
                }

                EditorUtility.DisplayDialog(
                    "Localization Sync",
                    $"Import thành công.\n\nFiles: {importResult.ImportedFileCount}\n" +
                    $"Keys: {importResult.KeyCount}\n" +
                    $"Constants: {importResult.GeneratedConstantCount}\n" +
                    $"Locales: {locales}\n\n" +
                    duplicateSummary,
                    "OK"
                );
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[LocalizationSync] Đã hủy sync.");
                FinishSync();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LocalizationSync] Sync thất bại:\n{exception}");
                FinishSync();

                EditorUtility.DisplayDialog(
                    "Localization Sync",
                    "Sync thất bại. Kiểm tra Console để xem chi tiết.",
                    "OK"
                );
            }
        }

        private void CancelSync()
        {
            if (!_isRunning || _isCancelling)
            {
                return;
            }

            _isCancelling = true;
            _status = "Đang hủy...";
            _syncCancellation?.Cancel();
            Repaint();
        }

        private static string BuildDuplicateSummary(
            IReadOnlyList<LocalizationDuplicateKey> duplicateKeys
        )
        {
            if (duplicateKeys == null ||
                duplicateKeys.Count == 0)
            {
                return "Duplicate keys: Không có";
            }

            const int maxDisplayedKeys = 30;

            var displayedKeys = duplicateKeys
                .Take(maxDisplayedKeys)
                .Select(item => $"- {item.Key} (x{item.Count})")
                .ToList();

            var result = $"Duplicate keys ({duplicateKeys.Count}):\n" +
                         string.Join("\n", displayedKeys);

            if (duplicateKeys.Count > maxDisplayedKeys)
            {
                result += $"\n... và {duplicateKeys.Count - maxDisplayedKeys} key khác. Xem Console để biết đầy đủ.";
            }

            return result;
        }

        private void FinishSync()
        {
            _syncCancellation?.Dispose();
            _syncCancellation = null;
            _isRunning = false;
            _isCancelling = false;
            _status = null;
            Repaint();
        }

        private void SaveSettings()
        {
            if (_settings == null)
            {
                return;
            }

            _settings.entries = _entries;
            LocalizationSyncToolSettingsStore.Save(_settings);
        }
    }
}