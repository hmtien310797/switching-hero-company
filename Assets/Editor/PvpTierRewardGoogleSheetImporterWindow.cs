#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Immortal_Switch.Scripts.Pvp.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Editor.ExcelConfigTool.Services;

namespace Immortal_Switch.Scripts.Pvp.Editor
{
    /// <summary>
    /// Importer bảng tier/rank reward PvP từ Google Sheet (published CSV) vào
    /// <see cref="PvpTierRewardDatabaseSO"/> tại <c>Assets/Immortal Switch/Addressable/Pvp/Data/PvpTierRewardDatabase.asset</c>
    /// (bind qua DatabaseManager [DatabaseBinding] — nhớ thêm asset vào label addressable "game_database").
    /// Clone pattern của HeroProgressionGoogleSheetImporterWindow.
    /// <para>Columns: tier_id, tier_name, tier_level, tier_required_point, tier_reward.</para>
    /// </summary>
    public class PvpTierRewardGoogleSheetImporterWindow : EditorWindow, IGameDataSyncStep
    {
        private const string GoogleSheetUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=63311490&single=true&output=csv";

        private const string OutputAssetPath = "Assets/Immortal Switch/Addressable/Pvp/Data/PvpTierRewardDatabase.asset";

        private bool isImporting;
        private Vector2 scrollPosition;
        private string lastResult = string.Empty;

        // ---- Batch API (GameDataSyncCoordinator) ----
        public bool IsRunning => isImporting;
        public bool LastImportFailed { get; private set; }

        public void RunForBatch()
        {
            LastImportFailed = false;
            ImportAsync();
        }
        // ---------------------------------------------

        [MenuItem("Tools/Game Data/PvP Tier Reward Importer")]
        public static void OpenWindow()
        {
            var window = GetWindow<PvpTierRewardGoogleSheetImporterWindow>("PvP Tier Reward Importer");
            window.minSize = new Vector2(620f, 430f);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Google Sheet Source", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField(GoogleSheetUrl);

            EditorGUILayout.Space(10f);
            EditorGUILayout.HelpBox(
                "Fetch bảng PvP tier/rank reward từ Google Sheet, parse CSV, ghi vào\n" +
                OutputAssetPath + "\n" +
                "(25 rows: bronze/silver/gold/platinum/diamond × level 1-5).",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(isImporting))
            {
                if (GUILayout.Button("Download And Import", GUILayout.Height(36f)))
                    ImportAsync();
            }

            if (isImporting)
                EditorGUILayout.HelpBox("Downloading and importing...", MessageType.Info);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Expected Columns", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "tier_id, tier_name, tier_level, tier_required_point, tier_reward\n" +
                "(tier_reward = \"itemId:qty;itemId:qty;...\" vd \"3:1000;1:5000\")",
                MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(lastResult, GUILayout.MinHeight(150f));

            EditorGUILayout.EndScrollView();
        }

        private async void ImportAsync()
        {
            isImporting = true;
            lastResult = "Downloading...";
            Repaint();

            try
            {
                string csvText = await DownloadTextAsync(GoogleSheetUrl);
                lastResult = ImportCsv(csvText);

                if (string.IsNullOrEmpty(lastResult) ||
                    lastResult.StartsWith("IMPORT FAILED", StringComparison.Ordinal))
                {
                    LastImportFailed = true;
                }
            }
            catch (Exception exception)
            {
                LastImportFailed = true;
                Debug.LogException(exception);
                lastResult = $"Import failed:\n{exception.Message}";
            }
            finally
            {
                isImporting = false;
                Repaint();
            }
        }

        private string ImportCsv(string csvText)
        {
            List<List<string>> csvRows = CsvUtility.Parse(csvText);
            if (csvRows.Count < 2)
                return "IMPORT FAILED\n\nCSV không có data hoặc chỉ có header.";

            var headers = new HeaderMap(csvRows[0]);
            string[] required = { "tier_id", "tier_name", "tier_level", "tier_required_point", "tier_reward" };
            foreach (string col in required)
            {
                if (!headers.Contains(col))
                    return $"IMPORT FAILED\n\nThiếu column: '{col}'";
            }

            var rows = new List<PvpTierRewardRow>();
            var errors = new List<string>();

            for (int i = 1; i < csvRows.Count; i++)
            {
                int sheetRow = i + 1;
                List<string> row = csvRows[i];
                if (IsEmptyRow(row)) continue;

                try
                {
                    rows.Add(new PvpTierRewardRow
                    {
                        TierId = ParseInt(GetCell(row, headers, "tier_id")),
                        TierName = GetCell(row, headers, "tier_name").Trim(),
                        TierLevel = ParseInt(GetCell(row, headers, "tier_level")),
                        RequiredPoint = ParseInt(GetCell(row, headers, "tier_required_point")),
                        Reward = GetCell(row, headers, "tier_reward").Trim()
                    });
                }
                catch (Exception e)
                {
                    errors.Add($"Row {sheetRow}: {e.Message}");
                }
            }

            if (errors.Count > 0)
                return "IMPORT FAILED\n\n" + string.Join("\n", errors.Select(e => "- " + e));

            if (rows.Count == 0)
                return "IMPORT FAILED\n\nKhông có row tier hợp lệ.";

            // Ghi vào SO (create nếu chưa có, cập nhật nếu có).
            EnsureOutputFolder();
            var db = AssetDatabase.LoadAssetAtPath<PvpTierRewardDatabaseSO>(OutputAssetPath);
            bool created = false;
            if (db == null)
            {
                db = CreateInstance<PvpTierRewardDatabaseSO>();
                AssetDatabase.CreateAsset(db, OutputAssetPath);
                created = true;
            }

            Undo.RecordObject(db, "Import PvP Tier Reward");
            db.Rows.Clear();
            db.Rows.AddRange(rows);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return $"IMPORT SUCCESS\n\n" +
                   (created ? $"Assets created: 1\n" : $"Assets updated: 1\n") +
                   $"Tiers imported: {rows.Count}";
        }

        private static async Task<string> DownloadTextAsync(string url)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.timeout = 30;
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException($"Download failed: {request.responseCode} - {request.error}\nURL: {url}");

                string text = request.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(text))
                    throw new InvalidOperationException("Google Sheet trả về nội dung rỗng.");

                if (text.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                    text.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Google Sheet trả về HTML thay vì CSV. Hãy bật quyền 'Anyone with the link can view'.");

                return text;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string GetCell(List<string> row, HeaderMap headers, string column)
        {
            int idx = headers.GetIndex(column);
            return idx >= 0 && idx < row.Count ? row[idx] : string.Empty;
        }

        private static int ParseInt(string value)
        {
            value = (value ?? string.Empty).Trim().TrimEnd(',');
            if (!int.TryParse(value, out int result))
                throw new FormatException($"Giá trị không phải int: '{value}'");
            return result;
        }

        private static bool IsEmptyRow(List<string> row)
        {
            if (row == null || row.Count == 0) return true;
            for (int i = 0; i < row.Count; i++)
                if (!string.IsNullOrWhiteSpace(row[i])) return false;
            return true;
        }

        /// <summary>Tạo folder cha cho OutputAssetPath nếu chưa tồn tại.</summary>
        private static void EnsureOutputFolder()
        {
            string folder = System.IO.Path.GetDirectoryName(OutputAssetPath).Replace('\\', '/');
            string[] parts = folder.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private sealed class HeaderMap
        {
            private readonly Dictionary<string, int> indices = new();

            public HeaderMap(IReadOnlyList<string> headers)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    string normalized = Normalize(headers[i]);
                    if (!string.IsNullOrEmpty(normalized) && !indices.ContainsKey(normalized))
                        indices.Add(normalized, i);
                }
            }

            public bool Contains(string header) => indices.ContainsKey(Normalize(header));
            public int GetIndex(string header) => indices.TryGetValue(Normalize(header), out int idx) ? idx : -1;

            private static string Normalize(string value)
            {
                if (value == null) return string.Empty;
                var sb = new StringBuilder();
                foreach (char c in value)
                    if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                return sb.ToString();
            }
        }

        private static class CsvUtility
        {
            public static List<List<string>> Parse(string csvText)
            {
                var rows = new List<List<string>>();
                var currentRow = new List<string>();
                var currentField = new StringBuilder();
                bool insideQuotes = false;

                for (int i = 0; i < csvText.Length; i++)
                {
                    char character = csvText[i];

                    if (insideQuotes)
                    {
                        if (character == '"')
                        {
                            bool escaped = i + 1 < csvText.Length && csvText[i + 1] == '"';
                            if (escaped) { currentField.Append('"'); i++; }
                            else insideQuotes = false;
                        }
                        else currentField.Append(character);
                        continue;
                    }

                    switch (character)
                    {
                        case '"': insideQuotes = true; break;
                        case ',':
                            currentRow.Add(currentField.ToString());
                            currentField.Clear();
                            break;
                        case '\r':
                            if (i + 1 < csvText.Length && csvText[i + 1] == '\n') i++;
                            FinishRow(rows, currentRow, currentField);
                            break;
                        case '\n':
                            FinishRow(rows, currentRow, currentField);
                            break;
                        default:
                            currentField.Append(character);
                            break;
                    }
                }

                if (insideQuotes)
                    throw new FormatException("CSV có quoted field chưa đóng.");

                if (currentField.Length > 0 || currentRow.Count > 0)
                    FinishRow(rows, currentRow, currentField);

                if (rows.Count > 0 && rows[0].Count > 0)
                    rows[0][0] = rows[0][0].TrimStart('﻿');

                return rows;
            }

            private static void FinishRow(ICollection<List<string>> rows, List<string> currentRow, StringBuilder currentField)
            {
                currentRow.Add(currentField.ToString());
                currentField.Clear();
                rows.Add(new List<string>(currentRow));
                currentRow.Clear();
            }
        }
    }
}
#endif