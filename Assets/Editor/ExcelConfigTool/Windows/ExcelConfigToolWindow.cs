using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Editor.ExcelConfigTool.Models;
using Editor.ExcelConfigTool.Services;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelConfigTool.Windows
{
    public class ExcelConfigToolWindow : EditorWindow
    {
        private ExcelConfigToolService _service;
        private ExcelConfigToolSettings _settings;
        private List<UrlEntry> _entries = new();
        private string _inputFolder;
        private string _outputScriptFolder;
        private string _outputAssetFolder;
        private Vector2 _scrollPos;
        private bool _isRunning;
        private bool _isCancelling;
        private CancellationTokenSource _syncCancellation;

        private enum SyncStep
        {
            None,
            Downloading,
            GenerateScripts,
            GenerateAssets,
            Done,
        }

        private SyncStep _syncStep;

        [MenuItem("Tools/Excel Config Tool")]
        public static void Open()
        {
            GetWindow<ExcelConfigToolWindow>("Excel Config Tool");
        }

        private void OnEnable()
        {
            _service = new ExcelConfigToolService();
            LoadSettings();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            _syncCancellation?.Cancel();
            _syncCancellation?.Dispose();
            _syncCancellation = null;
            SaveSettings();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void LoadSettings()
        {
            _settings = ExcelConfigToolSettingsStore.Load();
            _entries = _settings.entries ?? new List<UrlEntry>();
            _inputFolder = _settings.inputFolder;
            _outputScriptFolder = _settings.outputScriptFolder;
            _outputAssetFolder = _settings.outputAssetFolder;
        }

        private void SaveSettings()
        {
            if (_settings == null)
            {
                return;
            }

            _settings.entries = _entries;
            _settings.inputFolder = _inputFolder;
            _settings.outputScriptFolder = _outputScriptFolder;
            _settings.outputAssetFolder = _outputAssetFolder;
            ExcelConfigToolSettingsStore.Save(_settings);
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("Excel Config Tool", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                $"Project settings: {ExcelConfigToolSettingsStore.SETTINGS_PATH}",
                MessageType.None
            );

            EditorGUILayout.Space(4);

            DrawEntryList();
            EditorGUILayout.Space(8);

            DrawFolderPicker("Input CSV Folder", ref _inputFolder);
            DrawFolderPicker("Output Script Folder", ref _outputScriptFolder);
            DrawFolderPicker("Output Asset Folder", ref _outputAssetFolder);

            EditorGUILayout.Space(12);

            GUI.enabled = !_isRunning && _entries.Any(e => !string.IsNullOrWhiteSpace(e.url));

            if (GUILayout.Button("Sync All (Download -> Scripts -> Assets)", GUILayout.Height(40)))
            {
                SyncAll();
            }

            GUI.enabled = true;

            if (_isRunning)
            {
                EditorGUILayout.HelpBox(GetStatusText(), MessageType.Info);

                using (new EditorGUI.DisabledScope(_isCancelling))
                {
                    if (GUILayout.Button("Cancel Sync", GUILayout.Height(28)))
                    {
                        CancelSync();
                    }
                }
            }
            else if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox("Add at least one Google Sheets CSV URL.", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
            }
        }

        private string GetStatusText()
        {
            if (_isCancelling)
            {
                return "Cancelling sync...";
            }

            return _syncStep switch
            {
                SyncStep.Downloading => "Downloading CSV files from Google Sheets...",
                SyncStep.GenerateScripts => "Generating scripts...",
                SyncStep.GenerateAssets => "Generating ScriptableObject assets...",
                _ => "Processing...",
            };
        }

        private void SyncAll()
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
                EditorUtility.DisplayDialog("Excel Config Tool", "No valid URL was configured.", "OK");
                return;
            }

            SaveSettings();
            _syncCancellation?.Dispose();
            _syncCancellation = new CancellationTokenSource();
            _isCancelling = false;
            _isRunning = true;
            _syncStep = SyncStep.Downloading;
            Repaint();

            var forceOverwrite = EditorUtility.DisplayDialog(
                "Overwrite?",
                "Overwrite existing CSV files?",
                "Yes, overwrite",
                "No, keep existing"
            );

            DownloadAsync(requests, forceOverwrite);
        }

        private async void DownloadAsync(
            IReadOnlyList<DownloadRequest> requests,
            bool forceOverwrite
        )
        {
            try
            {
                var results = await GoogleSheetDownloader.DownloadAllAsync(
                    requests,
                    _inputFolder,
                    forceOverwrite,
                    _syncCancellation.Token
                );

                foreach (var result in results.Where(result => result.IsSuccess))
                {
                    if (result.SourceIndex >= 0 &&
                        result.SourceIndex < _entries.Count)
                    {
                        _entries[result.SourceIndex].fileName = result.FileName;
                    }
                }

                SaveSettings();

                var failures = results.Where(result => !result.IsSuccess).ToList();

                if (failures.Count > 0)
                {
                    StopSync();

                    var details = string.Join(
                        "\n",
                        failures.Select(result =>
                            $"Entry #{result.SourceIndex + 1}: {result.ErrorMessage}"
                        )
                    );

                    Debug.LogError(
                        "[ExcelConfigTool] Sync stopped because one or more downloads failed:\n" +
                        details
                    );

                    EditorUtility.DisplayDialog(
                        "Download failed",
                        $"Sync was stopped to avoid generating from stale CSV files.\n\n{details}",
                        "OK"
                    );

                    return;
                }

                _syncStep = SyncStep.GenerateScripts;
            }
            catch (OperationCanceledException) when (
                _syncCancellation == null ||
                _syncCancellation.IsCancellationRequested
            )
            {
                Debug.LogWarning("[ExcelConfigTool] Sync cancelled by user.");
                StopSync();
            }
            catch (Exception e)
            {
                StopSync();
                Debug.LogError($"[ExcelConfigTool] Download batch failed:\n{e}");

                EditorUtility.DisplayDialog(
                    "Download failed",
                    "The download batch failed. Check the Console for details.",
                    "OK"
                );
            }
        }

        private void OnEditorUpdate()
        {
            if (!_isRunning)
            {
                return;
            }

            if (_syncCancellation?.IsCancellationRequested == true)
            {
                Debug.LogWarning("[ExcelConfigTool] Sync cancelled by user.");
                StopSync();
                return;
            }

            try
            {
                switch (_syncStep)
                {
                    case SyncStep.GenerateScripts:
                    {
                        var changedScriptCount = _service.GenerateScripts(
                            _inputFolder,
                            _outputScriptFolder,
                            true
                        );

                        if (changedScriptCount > 0)
                        {
                            ExcelConfigSyncCoordinator.ScheduleAssetsAfterScriptReload(
                                _inputFolder,
                                _outputAssetFolder
                            );

                            CompleteSync();

                            // Set the pending session state before requesting a refresh,
                            // so DidReloadScripts cannot race ahead of the coordinator.
                            AssetDatabase.Refresh();
                        }
                        else
                        {
                            _syncStep = SyncStep.GenerateAssets;
                        }

                        break;
                    }

                    case SyncStep.GenerateAssets:
                        _service.GenerateOrUpdateAssets(
                            _inputFolder,
                            _outputAssetFolder,
                            true
                        );

                        CompleteSync();
                        Repaint();
                        EditorUtility.DisplayDialog("Excel Config Tool", "Sync completed.", "OK");
                        break;
                }
            }
            catch (Exception e)
            {
                StopSync();
                Debug.LogError($"[ExcelConfigTool] Sync failed:\n{e}");

                EditorUtility.DisplayDialog(
                    "Excel Config Tool",
                    "Sync failed. Check the Console for details.",
                    "OK"
                );
            }

            Repaint();
        }

        private void StopSync()
        {
            _syncStep = SyncStep.None;
            _isRunning = false;
            _isCancelling = false;
            _syncCancellation?.Dispose();
            _syncCancellation = null;
            Repaint();
        }

        private void CompleteSync()
        {
            _syncStep = SyncStep.Done;
            _isRunning = false;
            _isCancelling = false;
            _syncCancellation?.Dispose();
            _syncCancellation = null;
        }

        private void CancelSync()
        {
            if (!_isRunning || _isCancelling)
            {
                return;
            }

            _isCancelling = true;
            _syncCancellation?.Cancel();
            Repaint();
        }

        private void DrawEntryList()
        {
            GUILayout.Label("URL Entries", EditorStyles.boldLabel);

            if (GUILayout.Button("+ Add URL", GUILayout.Width(100)))
            {
                _entries.Add(new UrlEntry());
                SaveSettings();
            }

            EditorGUILayout.Space(4);

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(24));
                entry.url = EditorGUILayout.TextField(entry.url);

                var displayName = string.IsNullOrEmpty(entry.fileName)
                    ? "(auto)"
                    : entry.fileName;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(displayName, GUILayout.Width(400));
                }

                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    _entries.RemoveAt(i);
                    SaveSettings();
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawFolderPicker(string label, ref string folder)
        {
            EditorGUILayout.BeginHorizontal();
            folder = EditorGUILayout.TextField(label, folder);

            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                var selected = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);

                if (!string.IsNullOrWhiteSpace(selected))
                {
                    folder = selected.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase)
                        ? "Assets" + selected[Application.dataPath.Length..]
                        : selected;

                    folder = folder.Replace("\\", "/");
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}