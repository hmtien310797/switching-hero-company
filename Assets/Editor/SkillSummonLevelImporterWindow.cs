#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Immortal_Switch.Scripts.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Immortal_Switch.Editor.GameData
{
    public sealed class SkillSummonLevelImporterWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Game Data/Skill Summon Level Importer";

        private const string GoogleSheetCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1208326716&single=true&output=csv";

        // Đường dẫn đầy đủ đến file config .asset trong project.
        private const string ConfigAssetPath =
            "Assets/Immortal Switch/Addressable/SummonData/SkillSummonConfig.asset";

        private bool replaceExistingData = true;

        private string message;
        private string downloadedCsvText;
        private UnityWebRequest downloadRequest;
        private bool isDownloading;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<SkillSummonLevelImporterWindow>();
            window.titleContent = new GUIContent("Skill Summon Importer");
            window.minSize = new Vector2(640f, 500f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Skill Summon Level Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tải CSV từ Google Sheet và ghi trực tiếp vào SkillSummonConfigSO. " +
                "Config được load bằng AssetDatabase.LoadAssetAtPath từ ConfigAssetPath.",
                MessageType.Info);

            EditorGUILayout.LabelField("Config Asset Path", ConfigAssetPath);
            replaceExistingData = EditorGUILayout.Toggle(
                "Replace Existing Data",
                replaceExistingData);

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(isDownloading))
            {
                if (GUILayout.Button(
                        isDownloading
                            ? "Downloading Google Sheet..."
                            : "Download CSV And Apply",
                        GUILayout.Height(36f)))
                {
                    DownloadGoogleSheetAndApply();
                }
            }

            if (!string.IsNullOrWhiteSpace(message))
                EditorGUILayout.HelpBox(message, MessageType.None);
        }

        private void OnDisable()
        {
            EditorApplication.update -= PollDownload;
            DisposeDownloadRequest();
        }

        private void DownloadGoogleSheetAndApply()
        {
            if (isDownloading)
                return;

            DisposeDownloadRequest();

            downloadRequest = UnityWebRequest.Get(GoogleSheetCsvUrl);
            downloadRequest.timeout = 30;
            downloadRequest.SendWebRequest();

            isDownloading = true;
            message = "Đang tải CSV từ Google Sheet...";

            EditorApplication.update -= PollDownload;
            EditorApplication.update += PollDownload;
            Repaint();
        }

        private void PollDownload()
        {
            if (downloadRequest == null || !downloadRequest.isDone)
                return;

            EditorApplication.update -= PollDownload;
            isDownloading = false;

            try
            {
                if (downloadRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException(
                        $"Không tải được Google Sheet: {downloadRequest.error}");
                }

                downloadedCsvText = downloadRequest.downloadHandler.text;

                if (string.IsNullOrWhiteSpace(downloadedCsvText))
                    throw new InvalidOperationException("Google Sheet trả về nội dung rỗng.");

                message = "Đã tải CSV. Đang parse và apply...";
                ParseAndApply(downloadedCsvText);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                message = $"Import failed: {ex.Message}";
            }
            finally
            {
                DisposeDownloadRequest();
                Repaint();
            }
        }

        private void DisposeDownloadRequest()
        {
            downloadRequest?.Dispose();
            downloadRequest = null;
        }

        private void ParseAndApply(string csvText)
        {
            try
            {
                SkillSummonConfigSO config =
                    AssetDatabase.LoadAssetAtPath<SkillSummonConfigSO>(ConfigAssetPath);

                if (config == null)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy SkillSummonConfigSO tại đường dẫn: {ConfigAssetPath}");
                }

                List<Row> rows = ParseRows(csvText);
                if (rows.Count == 0)
                    throw new InvalidOperationException("Không có dòng dữ liệu hợp lệ.");

                Undo.RecordObject(config, "Import Skill Summon Levels");

                var serialized = new SerializedObject(config);
                serialized.Update();

                SerializedProperty levels = serialized.FindProperty("SummonLevels");
                SerializedProperty rewards = serialized.FindProperty("LevelRewards");

                if (levels == null || rewards == null)
                    throw new InvalidOperationException(
                        "Không tìm thấy SummonLevels hoặc LevelRewards.");

                if (replaceExistingData)
                {
                    levels.ClearArray();
                    rewards.ClearArray();
                }

                HashSet<int> existingLevels = ReadExistingLevels(levels);
                HashSet<int> existingRewards = ReadExistingLevels(rewards);

                int levelCount = 0;
                int rewardCount = 0;

                foreach (Row row in rows.OrderBy(x => x.SummonLevel))
                {
                    if (replaceExistingData || !existingLevels.Contains(row.SummonLevel))
                    {
                        AddLevel(levels, row);
                        existingLevels.Add(row.SummonLevel);
                        levelCount++;
                    }

                    if (row.ItemId > 0 &&
                        row.ItemQuantity > 0 &&
                        (replaceExistingData || !existingRewards.Contains(row.SummonLevel)))
                    {
                        AddReward(rewards, row);
                        existingRewards.Add(row.SummonLevel);
                        rewardCount++;
                    }
                }

                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();

                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);

                message = $"Imported {levelCount} levels and {rewardCount} rewards.";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                message = $"Import failed: {ex.Message}";
            }
        }

        private static List<Row> ParseRows(string input)
        {
            string[] lines = input.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int headerIndex = Array.FindIndex(lines, x => !string.IsNullOrWhiteSpace(x));
            if (headerIndex < 0)
                return new List<Row>();

            char delimiter = lines[headerIndex].Contains('\t')
                ? '\t'
                : lines[headerIndex].Contains(';') ? ';' : ',';

            string[] headers = SplitLine(lines[headerIndex], delimiter);
            var map = headers
                .Select((name, index) => new { Name = Normalize(name), Index = index })
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => x.First().Index);

            string[] required =
            {
                "summonlevel", "totalrollrequired",
                "gradebrate", "gradearate", "gradesrate", "gradessrate",
                "itemid", "itemquantity"
            };

            foreach (string column in required)
            {
                if (!map.ContainsKey(column))
                    throw new FormatException($"Thiếu cột: {column}");
            }

            var result = new List<Row>();
            var usedLevels = new HashSet<int>();

            for (int i = headerIndex + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] cells = SplitLine(lines[i], delimiter);
                int lineNumber = i + 1;

                var row = new Row
                {
                    SummonLevel = ParseInt(Cell(cells, map, "summonlevel"), "SummonLevel", lineNumber),
                    TotalRollRequired = ParseInt(Cell(cells, map, "totalrollrequired"), "TotalRollRequired", lineNumber),
                    GradeBRate = ParseFloat(Cell(cells, map, "gradebrate"), "GradeBRate", lineNumber),
                    GradeARate = ParseFloat(Cell(cells, map, "gradearate"), "GradeARate", lineNumber),
                    GradeSRate = ParseFloat(Cell(cells, map, "gradesrate"), "GradeSRate", lineNumber),
                    GradeSSRate = ParseFloat(Cell(cells, map, "gradessrate"), "GradeSSRate", lineNumber),
                    ItemId = ParseOptionalInt(Cell(cells, map, "itemid"), "ItemId", lineNumber),
                    ItemQuantity = ParseOptionalInt(Cell(cells, map, "itemquantity"), "ItemQuantity", lineNumber)
                };

                if (!usedLevels.Add(row.SummonLevel))
                    throw new FormatException($"SummonLevel {row.SummonLevel} bị trùng ở dòng {lineNumber}.");

                Validate(row, lineNumber);
                result.Add(row);
            }

            return result;
        }

        private static void Validate(Row row, int line)
        {
            if (row.SummonLevel < 1)
                throw new FormatException($"Dòng {line}: SummonLevel phải >= 1.");
            if (row.TotalRollRequired < 0)
                throw new FormatException($"Dòng {line}: TotalRollRequired không được âm.");

            ValidateRate(row.GradeBRate, "GradeBRate", line);
            ValidateRate(row.GradeARate, "GradeARate", line);
            ValidateRate(row.GradeSRate, "GradeSRate", line);
            ValidateRate(row.GradeSSRate, "GradeSSRate", line);

            if (row.ItemId < 0 || row.ItemQuantity < 0)
                throw new FormatException($"Dòng {line}: ItemId/ItemQuantity không được âm.");
        }

        private static void ValidateRate(float value, string name, int line)
        {
            if (value < 0f || value > 100f)
                throw new FormatException($"Dòng {line}: {name} phải trong khoảng 0-100.");
        }

        private static void AddLevel(SerializedProperty list, Row row)
        {
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            SerializedProperty item = list.GetArrayElementAtIndex(index);

            SetInt(item, "SummonLevel", row.SummonLevel);
            SetInt(item, "TotalRollRequired", row.TotalRollRequired);
            SetFloat(item, "GradeBRate", row.GradeBRate);
            SetFloat(item, "GradeARate", row.GradeARate);
            SetFloat(item, "GradeSRate", row.GradeSRate);
            SetFloat(item, "GradeSSRate", row.GradeSSRate);

            SetInt(item, "ItemId", row.ItemId);
            SetInt(item, "ItemQuantity", row.ItemQuantity);
        }

        private static void AddReward(SerializedProperty list, Row row)
        {
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            SerializedProperty entry = list.GetArrayElementAtIndex(index);

            SetInt(entry, "SummonLevel", row.SummonLevel);

            SerializedProperty items = entry.FindPropertyRelative("RewardItems");
            if (items == null)
                throw new InvalidOperationException("Không tìm thấy RewardItems.");

            items.ClearArray();
            items.InsertArrayElementAtIndex(0);

            SerializedProperty reward = items.GetArrayElementAtIndex(0);
            SetIntCandidate(reward, row.ItemId, "ItemId", "itemId", "ItemID", "itemID");
            SetIntCandidate(
                reward,
                row.ItemQuantity,
                "ItemQuantity", "Quantity", "itemQuantity", "quantity", "Amount", "amount");
        }

        private static HashSet<int> ReadExistingLevels(SerializedProperty list)
        {
            var result = new HashSet<int>();
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty level =
                    list.GetArrayElementAtIndex(i).FindPropertyRelative("SummonLevel");
                if (level != null)
                    result.Add(level.intValue);
            }
            return result;
        }

        private static void SetInt(SerializedProperty parent, string name, int value)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            if (property == null)
                throw new InvalidOperationException($"Không tìm thấy field {name}.");
            property.intValue = value;
        }

        private static void SetFloat(SerializedProperty parent, string name, float value)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            if (property == null)
                throw new InvalidOperationException($"Không tìm thấy field {name}.");
            property.floatValue = value;
        }

        private static void SetIntCandidate(
            SerializedProperty parent,
            int value,
            params string[] names)
        {
            foreach (string name in names)
            {
                SerializedProperty property = parent.FindPropertyRelative(name);
                if (property == null)
                    continue;

                property.intValue = value;
                return;
            }

            throw new InvalidOperationException(
                $"Không tìm thấy reward field: {string.Join(", ", names)}.");
        }

        private static string Cell(
            string[] cells,
            IReadOnlyDictionary<string, int> map,
            string name)
        {
            int index = map[name];
            return index < cells.Length ? cells[index].Trim() : string.Empty;
        }

        private static string Normalize(string value)
        {
            return new string((value ?? string.Empty)
                .Trim()
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static string[] SplitLine(string line, char delimiter)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            bool quoted = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                    continue;
                }

                if (c == delimiter && !quoted)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            result.Add(current.ToString().Trim());
            return result.ToArray();
        }

        private static int ParseInt(string value, string name, int line)
        {
            if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int result))
                return result;

            throw new FormatException($"Dòng {line}: {name}='{value}' không hợp lệ.");
        }

        private static int ParseOptionalInt(string value, string name, int line)
        {
            return string.IsNullOrWhiteSpace(value) ? 0 : ParseInt(value, name, line);
        }

        private static float ParseFloat(string value, string name, int line)
        {
            string normalized = (value ?? string.Empty).Trim().Replace(',', '.');

            if (float.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result))
                return result;

            throw new FormatException($"Dòng {line}: {name}='{value}' không hợp lệ.");
        }

        [Serializable]
        private sealed class Row
        {
            public int SummonLevel;
            public int TotalRollRequired;
            public float GradeBRate;
            public float GradeARate;
            public float GradeSRate;
            public float GradeSSRate;
            public int ItemId;
            public int ItemQuantity;
        }
    }
}
#endif