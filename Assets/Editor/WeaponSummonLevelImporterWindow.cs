#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Immortal_Switch.Scripts.SummonSystem.WeaponSummon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Immortal_Switch.Editor.GameData
{
    public sealed class WeaponSummonLevelImporterWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Game Data/Weapon Summon Level Importer";

        // Google Sheet CSV export URL:
        // https://docs.google.com/spreadsheets/d/{SHEET_ID}/export?format=csv&gid={GID}
        private const string GoogleSheetCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=985585951&single=true&output=csv";

        private const string ConfigAssetPath =
            "Assets/Immortal Switch/Addressable/SummonData/WeaponSummonConfig.asset";

        private bool replaceExistingData = true;

        private string downloadedCsvText;
        private string message;
        private UnityWebRequest downloadRequest;
        private bool isDownloading;

        [MenuItem(MenuPath)]
        private static void Open()
        {
            var window = GetWindow<WeaponSummonLevelImporterWindow>();
            window.titleContent = new GUIContent("Weapon Summon Importer");
            window.minSize = new Vector2(680f, 540f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Weapon Summon Level Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tải CSV từ Google Sheet và ghi trực tiếp vào config. " +
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
            message = "Đang tải Weapon Summon CSV từ Google Sheet...";

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
                WeaponSummonConfigSO config =
                    AssetDatabase.LoadAssetAtPath<WeaponSummonConfigSO>(ConfigAssetPath);

                if (config == null)
                {
                    throw new InvalidOperationException(
                        $"Không tìm thấy WeaponSummonConfigSO tại đường dẫn: {ConfigAssetPath}");
                }

                List<Row> rows = ParseRows(csvText);
                if (rows.Count == 0)
                    throw new InvalidOperationException("Không có dòng dữ liệu hợp lệ.");

                Undo.RecordObject(config, "Import Weapon Summon Levels");

                var serialized = new SerializedObject(config);
                serialized.Update();

                SerializedProperty levels = FindPropertyCandidate(
                    serialized,
                    "SummonLevels", "summonLevels");

                SerializedProperty rewards = FindPropertyCandidate(
                    serialized,
                    "LevelRewards", "levelRewards");

                if (levels == null || !levels.isArray)
                    throw new InvalidOperationException("Không tìm thấy array SummonLevels trong target config.");

                if (rewards == null || !rewards.isArray)
                    throw new InvalidOperationException("Không tìm thấy array LevelRewards trong target config.");

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

                message = $"Imported {levelCount} weapon summon levels and {rewardCount} rewards.";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                message = $"Import failed: {ex.Message}";
            }
        }

        private static List<Row> ParseRows(string input)
        {
            string[] lines = (input ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');

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
                "summonlevel",
                "totalrollrequired",
                "gradedrate",
                "gradecrate",
                "gradebrate",
                "gradearate",
                "gradesrate",
                "gradessrate",
                "star1rate",
                "star2rate",
                "star3rate",
                "star4rate",
                "star5rate",
                "itemid",
                "itemquantity"
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
                    GradeDRate = ParseFloat(Cell(cells, map, "gradedrate"), "GradeDRate", lineNumber),
                    GradeCRate = ParseFloat(Cell(cells, map, "gradecrate"), "GradeCRate", lineNumber),
                    GradeBRate = ParseFloat(Cell(cells, map, "gradebrate"), "GradeBRate", lineNumber),
                    GradeARate = ParseFloat(Cell(cells, map, "gradearate"), "GradeARate", lineNumber),
                    GradeSRate = ParseFloat(Cell(cells, map, "gradesrate"), "GradeSRate", lineNumber),
                    GradeSSRate = ParseFloat(Cell(cells, map, "gradessrate"), "GradeSSRate", lineNumber),
                    Star1Rate = ParseFloat(Cell(cells, map, "star1rate"), "Star1Rate", lineNumber),
                    Star2Rate = ParseFloat(Cell(cells, map, "star2rate"), "Star2Rate", lineNumber),
                    Star3Rate = ParseFloat(Cell(cells, map, "star3rate"), "Star3Rate", lineNumber),
                    Star4Rate = ParseFloat(Cell(cells, map, "star4rate"), "Star4Rate", lineNumber),
                    Star5Rate = ParseFloat(Cell(cells, map, "star5rate"), "Star5Rate", lineNumber),
                    ItemId = ParseOptionalInt(Cell(cells, map, "itemid"), "ItemId", lineNumber),
                    ItemQuantity = ParseOptionalInt(Cell(cells, map, "itemquantity"), "ItemQuantity", lineNumber)
                };

                if (!usedLevels.Add(row.SummonLevel))
                {
                    throw new FormatException(
                        $"SummonLevel {row.SummonLevel} bị trùng ở dòng {lineNumber}.");
                }

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

            ValidateRate(row.GradeDRate, "GradeDRate", line);
            ValidateRate(row.GradeCRate, "GradeCRate", line);
            ValidateRate(row.GradeBRate, "GradeBRate", line);
            ValidateRate(row.GradeARate, "GradeARate", line);
            ValidateRate(row.GradeSRate, "GradeSRate", line);
            ValidateRate(row.GradeSSRate, "GradeSSRate", line);
            ValidateRate(row.Star1Rate, "Star1Rate", line);
            ValidateRate(row.Star2Rate, "Star2Rate", line);
            ValidateRate(row.Star3Rate, "Star3Rate", line);
            ValidateRate(row.Star4Rate, "Star4Rate", line);
            ValidateRate(row.Star5Rate, "Star5Rate", line);

            float gradeTotal = row.GradeDRate + row.GradeCRate + row.GradeBRate +
                               row.GradeARate + row.GradeSRate + row.GradeSSRate;
            float starTotal = row.Star1Rate + row.Star2Rate + row.Star3Rate +
                              row.Star4Rate + row.Star5Rate;

            ValidateTotal(gradeTotal, "tổng GradeRate", line);
            ValidateTotal(starTotal, "tổng StarRate", line);

            if (row.ItemId < 0 || row.ItemQuantity < 0)
                throw new FormatException($"Dòng {line}: ItemId/ItemQuantity không được âm.");
        }

        private static void ValidateTotal(float total, string name, int line)
        {
            // Cho phép sai số 0.1 do dữ liệu làm tròn, ví dụ 100.1.
            if (Mathf.Abs(total - 100f) > 0.11f)
            {
                throw new FormatException(
                    $"Dòng {line}: {name} phải bằng 100, hiện tại là " +
                    total.ToString("0.####", CultureInfo.InvariantCulture) + ".");
            }
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

            SetIntCandidate(item, row.SummonLevel, "SummonLevel", "summonLevel", "Level", "level");
            SetIntCandidate(item, row.TotalRollRequired, "TotalRollRequired", "totalRollRequired");

            SetFloatCandidate(item, row.GradeDRate, "GradeDRate", "gradeDRate");
            SetFloatCandidate(item, row.GradeCRate, "GradeCRate", "gradeCRate");
            SetFloatCandidate(item, row.GradeBRate, "GradeBRate", "gradeBRate");
            SetFloatCandidate(item, row.GradeARate, "GradeARate", "gradeARate");
            SetFloatCandidate(item, row.GradeSRate, "GradeSRate", "gradeSRate");
            SetFloatCandidate(item, row.GradeSSRate, "GradeSSRate", "gradeSSRate");
            SetFloatCandidate(item, row.Star1Rate, "Star1Rate", "star1Rate");
            SetFloatCandidate(item, row.Star2Rate, "Star2Rate", "star2Rate");
            SetFloatCandidate(item, row.Star3Rate, "Star3Rate", "star3Rate");
            SetFloatCandidate(item, row.Star4Rate, "Star4Rate", "star4Rate");
            SetFloatCandidate(item, row.Star5Rate, "Star5Rate", "star5Rate");

            SetIntCandidate(item, row.ItemId, "ItemId", "itemId");
            SetIntCandidate(item, row.ItemQuantity, "ItemQuantity", "itemQuantity");
        }

        private static void AddReward(SerializedProperty list, Row row)
        {
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            SerializedProperty entry = list.GetArrayElementAtIndex(index);

            SetIntCandidate(entry, row.SummonLevel,
                "SummonLevel", "summonLevel", "Level", "level");

            SerializedProperty items = FindRelativeCandidate(
                entry,
                "RewardItems", "rewardItems", "Rewards", "rewards");

            if (items == null || !items.isArray)
                throw new InvalidOperationException("Không tìm thấy array RewardItems trong LevelRewards.");

            items.ClearArray();
            items.InsertArrayElementAtIndex(0);

            SerializedProperty reward = items.GetArrayElementAtIndex(0);
            SetIntCandidate(reward, row.ItemId,
                "ItemId", "itemId", "ItemID", "itemID");
            SetIntCandidate(reward, row.ItemQuantity,
                "ItemQuantity", "itemQuantity", "Quantity", "quantity", "Amount", "amount");
        }

        private static HashSet<int> ReadExistingLevels(SerializedProperty list)
        {
            var result = new HashSet<int>();

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty item = list.GetArrayElementAtIndex(i);
                SerializedProperty level = FindRelativeCandidate(
                    item,
                    "SummonLevel", "summonLevel", "Level", "level");

                if (level != null && level.propertyType == SerializedPropertyType.Integer)
                    result.Add(level.intValue);
            }

            return result;
        }

        private static SerializedProperty FindPropertyCandidate(
            SerializedObject serialized,
            params string[] names)
        {
            foreach (string name in names)
            {
                SerializedProperty property = serialized.FindProperty(name);
                if (property != null)
                    return property;
            }

            return null;
        }

        private static SerializedProperty FindRelativeCandidate(
            SerializedProperty parent,
            params string[] names)
        {
            foreach (string name in names)
            {
                SerializedProperty property = parent.FindPropertyRelative(name);
                if (property != null)
                    return property;
            }

            return null;
        }

        private static void SetIntCandidate(
            SerializedProperty parent,
            int value,
            params string[] names)
        {
            SerializedProperty property = FindRelativeCandidate(parent, names);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy int field: {string.Join(", ", names)}.");
            }

            property.intValue = value;
        }

        private static void SetFloatCandidate(
            SerializedProperty parent,
            float value,
            params string[] names)
        {
            SerializedProperty property = FindRelativeCandidate(parent, names);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy float field: {string.Join(", ", names)}.");
            }

            property.floatValue = value;
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
            {
                return result;
            }

            throw new FormatException($"Dòng {line}: {name}='{value}' không hợp lệ.");
        }

        private static int ParseOptionalInt(string value, string name, int line)
        {
            return string.IsNullOrWhiteSpace(value)
                ? 0
                : ParseInt(value, name, line);
        }

        private static float ParseFloat(string value, string name, int line)
        {
            string normalized = (value ?? string.Empty).Trim().Replace(',', '.');

            if (float.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result))
            {
                return result;
            }

            throw new FormatException($"Dòng {line}: {name}='{value}' không hợp lệ.");
        }

        [Serializable]
        private sealed class Row
        {
            public int SummonLevel;
            public int TotalRollRequired;
            public float GradeDRate;
            public float GradeCRate;
            public float GradeBRate;
            public float GradeARate;
            public float GradeSRate;
            public float GradeSSRate;
            public float Star1Rate;
            public float Star2Rate;
            public float Star3Rate;
            public float Star4Rate;
            public float Star5Rate;
            public int ItemId;
            public int ItemQuantity;
        }
    }
}
#endif