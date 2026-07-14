using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Editor.ExcelConfigTool.Services;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelConfigTool.Windows
{
    [Serializable]
    public class UrlEntry
    {
        public string url;
        public string fileName;
    }

    public class ExcelConfigToolWindow : EditorWindow
    {
        private ExcelConfigToolService _service;
        private List<UrlEntry> _entries = new();
        private string _inputFolder = "Assets/Immortal Switch/GameConfigs/Excel";
        private string _outputScriptFolder = "Assets/Immortal Switch/GameConfigs/Generated/Scripts";
        private string _outputAssetFolder = "Assets/Immortal Switch/GameConfigs/Generated/Assets";
        private Vector2 _scrollPos;
        private bool _isRunning;

        // Sync state machine
        private enum SyncStep
        {
            None,
            Downloading,
            GenerateScripts,
            WaitCompile,
            GenerateAssets,
            Done
        }

        private SyncStep _syncStep;

        private const string PREFS_KEY = "ExcelConfigTool_Entries";

        [MenuItem("Tools/Excel Config Tool")]
        public static void Open()
        {
            GetWindow<ExcelConfigToolWindow>("Excel Config Tool");
        }

        private void OnEnable()
        {
            _service = new ExcelConfigToolService();
            LoadEntries();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SaveEntries();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void LoadEntries()
        {
            var raw = EditorPrefs.GetString(PREFS_KEY, "");

            _entries = string.IsNullOrEmpty(raw)
                ? new List<UrlEntry>()
                : JsonConvert.DeserializeObject<List<UrlEntry>>(raw) ?? new List<UrlEntry>();
        }

        private void SaveEntries()
        {
            var valid = _entries.Where(e => !string.IsNullOrWhiteSpace(e.url)).ToList();
            EditorPrefs.SetString(PREFS_KEY, JsonConvert.SerializeObject(valid));
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("Excel Config Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawEntryList();
            EditorGUILayout.Space(8);

            DrawFolderPicker("Input CSV Folder", ref _inputFolder);
            DrawFolderPicker("Output Script Folder", ref _outputScriptFolder);
            DrawFolderPicker("Output Asset Folder", ref _outputAssetFolder);

            EditorGUILayout.Space(12);

            GUI.enabled = !_isRunning && _entries.Count > 0;

            if (GUILayout.Button("Sync All (Download → Scripts → Assets)", GUILayout.Height(40)))
            {
                SyncAll();
            }

            GUI.enabled = true;

            if (_isRunning)
            {
                EditorGUILayout.HelpBox(GetStatusText(), MessageType.Info);
            }
            else if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox("Thêm URL Google Sheets vào danh sách.", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private string GetStatusText()
        {
            return _syncStep switch
            {
                SyncStep.Downloading => "Đang tải CSV từ Google Sheets...",
                SyncStep.GenerateScripts => "Đang generate scripts...",
                SyncStep.WaitCompile => "Đang chờ Unity compile...",
                SyncStep.GenerateAssets => "Đang generate assets...",
                _ => "Đang xử lý..."
            };
        }

        // ── Sync state machine (chạy trên main thread qua EditorApplication.update) ──

        private void SyncAll()
        {
            _isRunning = true;
            _syncStep = SyncStep.Downloading;
            Repaint();

            var force = EditorUtility.DisplayDialog(
                "Overwrite?",
                "Ghi đè file CSV đã tồn tại?",
                "Yes, overwrite",
                "No, skip existing"
            );

            DownloadAsync(force);
        }

        private async void DownloadAsync(bool force)
        {
            var urls = _entries.Select(e => e.url).ToList();
            var results = await GoogleSheetDownloader.DownloadAllAsync(urls, _inputFolder, force);

            // Cập nhật filename sau khi download (chạy trên ThreadPool, chỉ ghi dữ liệu)
            for (int i = 0; i < results.Count && i < _entries.Count; i++)
            {
                _entries[i].fileName = results[i].FileName;
            }

            SaveEntries();

            // Đánh dấu để main thread xử lý tiếp
            _syncStep = SyncStep.GenerateScripts;
        }

        private void OnEditorUpdate()
        {
            if (!_isRunning)
            {
                return;
            }

            switch (_syncStep)
            {
                case SyncStep.GenerateScripts:
                    _syncStep = SyncStep.WaitCompile;
                    _service.GenerateScripts(_inputFolder, _outputScriptFolder);
                    break;

                case SyncStep.WaitCompile:
                    if (EditorApplication.isCompiling ||
                        EditorApplication.isUpdating)
                    {
                        return;
                    }

                    _syncStep = SyncStep.GenerateAssets;
                    break;

                case SyncStep.GenerateAssets:
                    _syncStep = SyncStep.Done;
                    _service.GenerateOrUpdateAssets(_inputFolder, _outputAssetFolder);

                    _isRunning = false;
                    Repaint();
                    EditorUtility.DisplayDialog("Done", "Sync hoàn tất!", "OK");
                    break;
            }

            if (_syncStep != SyncStep.WaitCompile)
            {
                Repaint();
            }
        }

        // ── UI ────────────────────────────────────────────────────────────────

        private void DrawEntryList()
        {
            GUILayout.Label("URL Entries", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Add URL", GUILayout.Width(100)))
            {
                _entries.Add(new UrlEntry());
                SaveEntries();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(20));

                var newUrl = EditorGUILayout.TextField(entry.url);

                if (newUrl != entry.url)
                {
                    entry.url = newUrl;
                    SaveEntries();
                }

                var displayName = string.IsNullOrEmpty(entry.fileName) ? "(auto)" : entry.fileName;
                GUI.enabled = false;
                EditorGUILayout.TextField(displayName, GUILayout.Width(400));
                GUI.enabled = true;

                if (GUILayout.Button("x", GUILayout.Width(20)))
                {
                    _entries.RemoveAt(i);
                    SaveEntries();
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
                var selected = EditorUtility.OpenFolderPanel(label, Application.dataPath, "");

                if (string.IsNullOrWhiteSpace(selected))
                {
                    return;
                }

                folder = selected.StartsWith(Application.dataPath)
                    ? "Assets" + selected[Application.dataPath.Length..]
                    : selected;

                folder = folder.Replace("\\", "/");
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}