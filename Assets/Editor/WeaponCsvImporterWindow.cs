#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Battle;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Immortal_Switch.Scripts.Equipment.Editor
{
    public class WeaponCsvImporterWindow : EditorWindow
    {
        // Mỗi URL trỏ tới một sheet/tab khác nhau bằng gid.
        private const string StandardWeaponCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1775448913&single=true&output=csv";

        private const string ExclusiveWeaponCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=286312787&single=true&output=csv";

        private const string WeaponStatCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1553024302&single=true&output=csv";

        private const string WeaponLevelConfigCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1298802847&single=true&output=csv";

        private const string WeaponLimitBreakConfigCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=368806139&single=true&output=csv";

        private string databaseAssetPath = "Assets/Data/Equipment/WeaponDatabase.asset";
        private string standardWeaponFolder = "Assets/Data/Equipment/StandardWeapons";
        private string exclusiveWeaponFolder = "Assets/Data/Equipment/ExclusiveWeapons";
        private string levelConfigFolder = "Assets/Data/Equipment/LevelConfigs";
        private string limitBreakConfigFolder = "Assets/Data/Equipment/LimitBreakConfigs";

        private const string PrefKeyDatabaseAssetPath = "WeaponCsvImporter.DatabaseAssetPath";
        private const string PrefKeyStandardFolder = "WeaponCsvImporter.StandardFolder";
        private const string PrefKeyExclusiveFolder = "WeaponCsvImporter.ExclusiveFolder";
        private const string PrefKeyLevelConfigFolder = "WeaponCsvImporter.LevelConfigFolder";
        private const string PrefKeyLimitBreakConfigFolder = "WeaponCsvImporter.LimitBreakConfigFolder";

        private bool isImporting;
        private string importStatus;

        private void OnEnable()
        {
            LoadPrefs();
        }

        private void LoadPrefs()
        {
            databaseAssetPath = EditorPrefs.GetString(
                PrefKeyDatabaseAssetPath,
                "Assets/Data/Equipment/WeaponDatabase.asset"
            );

            standardWeaponFolder = EditorPrefs.GetString(
                PrefKeyStandardFolder,
                "Assets/Data/Equipment/StandardWeapons"
            );

            exclusiveWeaponFolder = EditorPrefs.GetString(
                PrefKeyExclusiveFolder,
                "Assets/Data/Equipment/ExclusiveWeapons"
            );

            levelConfigFolder = EditorPrefs.GetString(
                PrefKeyLevelConfigFolder,
                "Assets/Data/Equipment/LevelConfigs"
            );

            limitBreakConfigFolder = EditorPrefs.GetString(
                PrefKeyLimitBreakConfigFolder,
                "Assets/Data/Equipment/LimitBreakConfigs"
            );
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefKeyDatabaseAssetPath, databaseAssetPath ?? string.Empty);
            EditorPrefs.SetString(PrefKeyStandardFolder, standardWeaponFolder ?? string.Empty);
            EditorPrefs.SetString(PrefKeyExclusiveFolder, exclusiveWeaponFolder ?? string.Empty);
            EditorPrefs.SetString(PrefKeyLevelConfigFolder, levelConfigFolder ?? string.Empty);
            EditorPrefs.SetString(PrefKeyLimitBreakConfigFolder, limitBreakConfigFolder ?? string.Empty);
        }

        [MenuItem("Tools/Equipment/Weapon CSV Importer")]
        public static void Open()
        {
            GetWindow<WeaponCsvImporterWindow>("Weapon CSV Importer");
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Google Sheet Sources", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Standard Weapon", StandardWeaponCsvUrl);
                EditorGUILayout.TextField("Exclusive Weapon", ExclusiveWeaponCsvUrl);
                EditorGUILayout.TextField("Weapon Stat", WeaponStatCsvUrl);
                EditorGUILayout.TextField("Level Config", WeaponLevelConfigCsvUrl);
                EditorGUILayout.TextField("Limit Break Config", WeaponLimitBreakConfigCsvUrl);
            }

            EditorGUILayout.HelpBox(
                "Google Sheet phải được chia sẻ quyền Viewer. " +
                "Mỗi URL phải dùng export?format=csv&gid=...",
                MessageType.Info
            );

            EditorGUILayout.Space();
            GUILayout.Label("Output", EditorStyles.boldLabel);

            databaseAssetPath = EditorGUILayout.TextField("Database Asset", databaseAssetPath);
            standardWeaponFolder = EditorGUILayout.TextField("Standard Folder", standardWeaponFolder);
            exclusiveWeaponFolder = EditorGUILayout.TextField("Exclusive Folder", exclusiveWeaponFolder);
            levelConfigFolder = EditorGUILayout.TextField("Level Config Folder", levelConfigFolder);
            limitBreakConfigFolder = EditorGUILayout.TextField("Limit Break Config Folder", limitBreakConfigFolder);

            if (EditorGUI.EndChangeCheck())
            {
                SavePrefs();
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrWhiteSpace(importStatus))
            {
                EditorGUILayout.HelpBox(
                    importStatus,
                    isImporting ? MessageType.Info : MessageType.None
                );
            }

            using (new EditorGUI.DisabledScope(isImporting))
            {
                string buttonLabel = isImporting
                    ? "Downloading Google Sheet..."
                    : "Import / Update Weapon Data";

                if (GUILayout.Button(buttonLabel, GUILayout.Height(32)))
                {
                    ImportAllAsync();
                }
            }
        }

        private async void ImportAllAsync()
        {
            if (isImporting)
                return;

            isImporting = true;
            importStatus = "Đang tải dữ liệu từ Google Sheet...";
            Repaint();

            try
            {
                SavePrefs();

                EnsureFolder(standardWeaponFolder);
                EnsureFolder(exclusiveWeaponFolder);
                EnsureFolder(levelConfigFolder);
                EnsureFolder(limitBreakConfigFolder);
                EnsureFolder(Path.GetDirectoryName(databaseAssetPath)?.Replace("\\", "/"));

                importStatus = "Đang tải Standard Weapon...";
                Repaint();

                string standardWeaponCsvText = await DownloadCsvAsync(
                    StandardWeaponCsvUrl,
                    "Standard Weapon"
                );

                importStatus = "Đang tải Exclusive Weapon...";
                Repaint();

                string exclusiveWeaponCsvText = await DownloadCsvAsync(
                    ExclusiveWeaponCsvUrl,
                    "Exclusive Weapon"
                );

                importStatus = "Đang tải Weapon Stat...";
                Repaint();

                string weaponStatCsvText = await DownloadCsvAsync(
                    WeaponStatCsvUrl,
                    "Weapon Stat"
                );

                importStatus = "Đang tải Weapon Level Config...";
                Repaint();

                string weaponLevelConfigCsvText = await DownloadCsvAsync(
                    WeaponLevelConfigCsvUrl,
                    "Weapon Level Config"
                );

                importStatus = "Đang tải Weapon Limit Break Config...";
                Repaint();

                string weaponLimitBreakConfigCsvText = await DownloadCsvAsync(
                    WeaponLimitBreakConfigCsvUrl,
                    "Weapon Limit Break Config"
                );

                importStatus = "Đang đọc và tạo Weapon Data...";
                Repaint();

                var standardRows = CsvUtility.ReadRowsFromText(standardWeaponCsvText);
                var exclusiveRows = CsvUtility.ReadRowsFromText(exclusiveWeaponCsvText);
                var statRows = CsvUtility.ReadRowsFromText(weaponStatCsvText);
                var levelRows = CsvUtility.ReadRowsFromText(weaponLevelConfigCsvText);
                var limitBreakRows = CsvUtility.ReadRowsFromText(weaponLimitBreakConfigCsvText);

                var statMap = BuildStatMap(statRows);
                var levelConfigMap = BuildLevelConfigs(levelRows);
                var limitBreakConfigMap = BuildLimitBreakConfigs(limitBreakRows);

                var standardAssets = ImportStandardWeapons(
                    standardRows,
                    statMap,
                    levelConfigMap,
                    limitBreakConfigMap
                );

                var exclusiveAssets = ImportExclusiveWeapons(
                    exclusiveRows,
                    statMap,
                    levelConfigMap,
                    limitBreakConfigMap
                );

                BuildDatabase(
                    standardAssets,
                    exclusiveAssets,
                    levelConfigMap.Values.ToList(),
                    limitBreakConfigMap.Values.ToList()
                );

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                importStatus = "Import Google Sheet hoàn tất.";

                EditorUtility.DisplayDialog(
                    "Success",
                    "Import weapon data từ Google Sheet hoàn tất.",
                    "OK"
                );
            }
            catch (Exception ex)
            {
                importStatus = $"Import thất bại: {ex.Message}";

                Debug.LogException(ex);

                EditorUtility.DisplayDialog(
                    "Import Failed",
                    ex.Message,
                    "OK"
                );
            }
            finally
            {
                isImporting = false;
                Repaint();
            }
        }

        private static async Task<string> DownloadCsvAsync(
            string url,
            string sheetName)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception($"Google Sheet URL của '{sheetName}' đang trống.");

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Google Sheet URL không hợp lệ của '{sheetName}': {url}");

            using var request = UnityWebRequest.Get(url);

            request.timeout = 30;

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

#if UNITY_2020_2_OR_NEWER
            bool hasError = request.result != UnityWebRequest.Result.Success;
#else
    bool hasError = request.isNetworkError || request.isHttpError;
#endif

            if (hasError)
            {
                throw new Exception(
                    $"Không thể tải Google Sheet '{sheetName}'.\n" +
                    $"URL: {url}\n" +
                    $"HTTP Code: {request.responseCode}\n" +
                    $"Error: {request.error}"
                );
            }

            string csvText = request.downloadHandler.text;

            if (string.IsNullOrWhiteSpace(csvText))
                throw new Exception($"Google Sheet '{sheetName}' trả về dữ liệu trống.");

            // Trường hợp URL trả về trang đăng nhập hoặc HTML thay vì CSV.
            string trimmedText = csvText.TrimStart();

            if (trimmedText.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmedText.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception(
                    $"Google Sheet '{sheetName}' trả về HTML thay vì CSV. " +
                    "Hãy kiểm tra quyền chia sẻ hoặc URL export CSV."
                );
            }

            return csvText;
        }

        private Dictionary<string, List<WeaponStatBlock>> BuildStatMap(List<Dictionary<string, string>> rows)
        {
            var map = new Dictionary<string, List<WeaponStatBlock>>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                string statGroupId = GetRequired(row, "StatGroupId");
                var statType = ParseEnum<StatType>(GetRequired(row, "StatType"), "StatType");
                var operation = ParseEnum<ModifierOp>(GetRequired(row, "Operation"), "Operation");
                float value = ParseFloat(GetRequired(row, "Value"), "Value");

                if (!map.TryGetValue(statGroupId, out var list))
                {
                    list = new List<WeaponStatBlock>();
                    map.Add(statGroupId, list);
                }

                list.Add(new WeaponStatBlock
                {
                    StatType = statType,
                    Operation = operation,
                    Value = value
                });
            }

            return map;
        }

        private Dictionary<string, WeaponLevelConfigSO> BuildLevelConfigs(List<Dictionary<string, string>> rows)
        {
            var map = new Dictionary<string, WeaponLevelConfigSO>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                string configId = GetRequired(row, "LevelConfigId");
                float baseCost = ParseFloat(GetRequired(row, "BaseCost"), "BaseCost");
                float exponent = ParseFloat(GetRequired(row, "Exponent"), "Exponent");
                float multiplier = ParseFloat(GetRequired(row, "Multiplier"), "Multiplier");
                int minCost = ParseInt(GetRequired(row, "MinCost"), "MinCost");

                string assetPath = $"{levelConfigFolder}/LV_{SanitizeFileName(configId)}.asset";
                var asset = LoadOrCreateAsset<WeaponLevelConfigSO>(assetPath);

                asset.ConfigId = configId;
                asset.BaseCost = baseCost;
                asset.Exponent = exponent;
                asset.Multiplier = multiplier;
                asset.MinCost = minCost;

                EditorUtility.SetDirty(asset);
                map[configId] = asset;
            }

            return map;
        }

        private Dictionary<string, WeaponLimitBreakConfigSO> BuildLimitBreakConfigs(
            List<Dictionary<string, string>> rows)
        {
            var map = new Dictionary<string, WeaponLimitBreakConfigSO>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                string configId = GetRequired(row, "LimitBreakConfigId");
                int stage = ParseInt(GetRequired(row, "Stage"), "Stage");
                int requiredLevel = ParseInt(GetRequired(row, "RequiredLevel"), "RequiredLevel");
                int cost = ParseInt(GetRequired(row, "BreakThroughStoneCost"), "BreakThroughStoneCost");
                float successRate = ParseFloat(GetRequired(row, "SuccessRate"), "SuccessRate");

                if (!map.TryGetValue(configId, out var asset))
                {
                    string assetPath = $"{limitBreakConfigFolder}/LB_{SanitizeFileName(configId)}.asset";
                    asset = LoadOrCreateAsset<WeaponLimitBreakConfigSO>(assetPath);
                    asset.ConfigId = configId;
                    asset.LevelPerStage = 25;
                    asset.Entries = new List<WeaponLimitBreakEntry>();
                    map.Add(configId, asset);
                }

                asset.Entries.RemoveAll(x => x.Stage == stage);
                asset.Entries.Add(new WeaponLimitBreakEntry
                {
                    Stage = stage,
                    RequiredLevel = requiredLevel,
                    BreakThroughStoneCost = cost,
                    SuccessRate = successRate
                });

                asset.Entries = asset.Entries.OrderBy(x => x.Stage).ToList();
                EditorUtility.SetDirty(asset);
            }

            return map;
        }

        private List<StandardWeaponDefinitionSO> ImportStandardWeapons(
            List<Dictionary<string, string>> rows,
            Dictionary<string, List<WeaponStatBlock>> statMap,
            Dictionary<string, WeaponLevelConfigSO> levelConfigMap,
            Dictionary<string, WeaponLimitBreakConfigSO> limitBreakConfigMap)
        {
            var result = new List<StandardWeaponDefinitionSO>();

            foreach (var row in rows)
            {
                int weaponId = ParseInt(GetRequired(row, "WeaponId"), "WeaponId");
                string weaponName = GetRequired(row, "Name");
                HeroClass heroClass = ParseEnum<HeroClass>(GetRequired(row, "Class"), "Class");
                WeaponTier tier = ParseEnum<WeaponTier>(GetRequired(row, "Tier"), "Tier");
                int star = ParseInt(GetRequired(row, "Star"), "Star");
                string statGroupId = GetRequired(row, "StatGroupId");

                WeaponFuseMode fuseMode = ParseEnum<WeaponFuseMode>(GetRequired(row, "FuseMode"), "FuseMode");
                int nextWeaponId = ParseInt(GetRequired(row, "NextWeaponId"), "NextWeaponId");
                int fuseShardRequired = ParseInt(GetRequired(row, "FuseShardRequired"), "FuseShardRequired");
                HeroClass exclusivePoolClass =
                    ParseEnum<HeroClass>(GetRequired(row, "ExclusivePoolClass"), "ExclusivePoolClass");
                int exclusiveClassStoneCost =
                    ParseInt(GetRequired(row, "ExclusiveClassStoneCost"), "ExclusiveClassStoneCost");

                string levelConfigId = GetRequired(row, "LevelConfigId");
                string limitBreakConfigId = GetRequired(row, "LimitBreakConfigId");

                string assetPath = $"{standardWeaponFolder}/STD_{weaponId}_{SanitizeFileName(weaponName)}.asset";
                var asset = LoadOrCreateAsset<StandardWeaponDefinitionSO>(assetPath);

                asset.WeaponId = weaponId;
                asset.WeaponName = weaponName;
                asset.WeaponClass = heroClass;
                asset.Tier = tier;
                asset.Star = star;
                asset.EquipStats = statMap.TryGetValue(statGroupId, out var stats)
                    ? stats.ToArray()
                    : Array.Empty<WeaponStatBlock>();

                asset.FuseMode = fuseMode;
                asset.NextWeaponId = nextWeaponId;
                asset.FuseShardRequired = fuseShardRequired;
                asset.ExclusivePoolClass = exclusivePoolClass;
                BigNumber.TryParse(exclusiveClassStoneCost.ToString(), out asset.ExclusiveClassStoneCost);

                asset.LevelConfig = levelConfigMap.TryGetValue(levelConfigId, out var lv) ? lv : null;
                asset.LimitBreakConfig = limitBreakConfigMap.TryGetValue(limitBreakConfigId, out var lb) ? lb : null;

                EditorUtility.SetDirty(asset);
                result.Add(asset);
            }

            return result;
        }

        private List<ExclusiveWeaponDefinitionSO> ImportExclusiveWeapons(
            List<Dictionary<string, string>> rows,
            Dictionary<string, List<WeaponStatBlock>> statMap,
            Dictionary<string, WeaponLevelConfigSO> levelConfigMap,
            Dictionary<string, WeaponLimitBreakConfigSO> limitBreakConfigMap)
        {
            var result = new List<ExclusiveWeaponDefinitionSO>();

            foreach (var row in rows)
            {
                int exclusiveWeaponId = ParseInt(GetRequired(row, "ExclusiveWeaponId"), "ExclusiveWeaponId");
                string weaponName = GetRequired(row, "Name");
                int heroId = ParseInt(GetRequired(row, "HeroId"), "HeroId");
                HeroClass heroClass = ParseEnum<HeroClass>(GetRequired(row, "Class"), "Class");
                string statGroupId = GetRequired(row, "StatGroupId");
                string levelConfigId = GetRequired(row, "LevelConfigId");
                string limitBreakConfigId = GetRequired(row, "LimitBreakConfigId");

                string assetPath =
                    $"{exclusiveWeaponFolder}/EX_{exclusiveWeaponId}_{SanitizeFileName(weaponName)}.asset";
                var asset = LoadOrCreateAsset<ExclusiveWeaponDefinitionSO>(assetPath);

                asset.ExclusiveWeaponId = exclusiveWeaponId;
                asset.WeaponName = weaponName;
                asset.HeroId = heroId;
                asset.HeroClass = heroClass;
                asset.EquipStats = statMap.TryGetValue(statGroupId, out var stats)
                    ? stats.ToArray()
                    : Array.Empty<WeaponStatBlock>();

                asset.LevelConfig = levelConfigMap.TryGetValue(levelConfigId, out var lv) ? lv : null;
                asset.LimitBreakConfig = limitBreakConfigMap.TryGetValue(limitBreakConfigId, out var lb) ? lb : null;

                EditorUtility.SetDirty(asset);
                result.Add(asset);
            }

            return result;
        }

        private void BuildDatabase(
            List<StandardWeaponDefinitionSO> standardAssets,
            List<ExclusiveWeaponDefinitionSO> exclusiveAssets,
            List<WeaponLevelConfigSO> levelConfigs,
            List<WeaponLimitBreakConfigSO> limitBreakConfigs)
        {
            var database = LoadOrCreateAsset<WeaponDatabaseSO>(databaseAssetPath);

            database.StandardWeapons = standardAssets.OrderBy(x => x.WeaponId).ToList();
            database.ExclusiveWeapons = exclusiveAssets.OrderBy(x => x.ExclusiveWeaponId).ToList();
            database.LevelConfigs = levelConfigs.OrderBy(x => x.ConfigId).ToList();
            database.LimitBreakConfigs = limitBreakConfigs.OrderBy(x => x.ConfigId).ToList();

            EditorUtility.SetDirty(database);
        }

        private static T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                return asset;

            asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

            folderPath = folderPath.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string GetRequired(Dictionary<string, string> row, string key)
        {
            if (!row.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                throw new Exception($"Missing required column '{key}' or empty value.");

            return value.Trim();
        }

        private static string GetOptional(Dictionary<string, string> row, string key)
        {
            return row.TryGetValue(key, out var value) ? value?.Trim() ?? string.Empty : string.Empty;
        }

        private static int ParseInt(string raw, string fieldName)
        {
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new Exception($"Invalid int for field '{fieldName}': {raw}");
            return value;
        }

        private static float ParseFloat(string raw, string fieldName)
        {
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                throw new Exception($"Invalid float for field '{fieldName}': {raw}");
            return value;
        }

        private static TEnum ParseEnum<TEnum>(string raw, string fieldName) where TEnum : struct
        {
            if (!Enum.TryParse(raw, true, out TEnum value))
                throw new Exception($"Invalid enum '{fieldName}': {raw}");
            return value;
        }

        private static string SanitizeFileName(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }

            return input.Replace(" ", "_");
        }

        private static Sprite FindSpriteByName(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;

            spriteName = spriteName.Trim();

            // Tìm các asset có khả năng chứa sprite
            string[] guids = AssetDatabase.FindAssets($"{spriteName}");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                // Load tất cả sub-assets ở path này
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is Sprite sprite &&
                        string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase))
                    {
                        return sprite;
                    }
                }
            }

            // Fallback: tìm riêng type Sprite
            string[] spriteGuids = AssetDatabase.FindAssets($"{spriteName} t:Sprite");
            for (int i = 0; i < spriteGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(spriteGuids[i]);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);

                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is Sprite sprite &&
                        string.Equals(sprite.name, spriteName, StringComparison.OrdinalIgnoreCase))
                    {
                        return sprite;
                    }
                }
            }

            Debug.LogWarning($"[WeaponCsvImporter] Sprite not found: {spriteName}");
            return null;
        }
    }

    public static class CsvUtility
    {
        public static List<Dictionary<string, string>> ReadRowsFromText(string csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText))
                throw new Exception("CSV content is null or empty.");

            csvText = csvText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');

            List<string> lines = SplitCsvLines(csvText);

            if (lines.Count == 0)
                return new List<Dictionary<string, string>>();

            string headerLine = lines[0].TrimStart('\uFEFF');

            List<string> headers = ParseCsvLine(headerLine);

            for (int i = 0; i < headers.Count; i++)
            {
                headers[i] = headers[i].Trim();
            }

            var rows = new List<Dictionary<string, string>>();

            for (int lineIndex = 1; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                List<string> values = ParseCsvLine(line);

                var row = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

                for (int columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    string header = headers[columnIndex];

                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    string value = columnIndex < values.Count
                        ? values[columnIndex]
                        : string.Empty;

                    row[header] = value.Trim();
                }

                rows.Add(row);
            }

            return rows;
        }

        public static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();

            if (line == null)
            {
                values.Add(string.Empty);
                return values;
            }

            var currentValue = new StringBuilder();

            bool isInsideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char currentCharacter = line[i];

                if (currentCharacter == '"')
                {
                    bool isEscapedQuote =
                        isInsideQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"';

                    if (isEscapedQuote)
                    {
                        currentValue.Append('"');
                        i++;
                        continue;
                    }

                    isInsideQuotes = !isInsideQuotes;
                    continue;
                }

                if (currentCharacter == ',' && !isInsideQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                    continue;
                }

                currentValue.Append(currentCharacter);
            }

            values.Add(currentValue.ToString());

            return values;
        }

        private static List<string> SplitCsvLines(string csvText)
        {
            var lines = new List<string>();
            var currentLine = new StringBuilder();

            bool isInsideQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char currentCharacter = csvText[i];

                if (currentCharacter == '"')
                {
                    bool isEscapedQuote =
                        isInsideQuotes &&
                        i + 1 < csvText.Length &&
                        csvText[i + 1] == '"';

                    if (isEscapedQuote)
                    {
                        currentLine.Append(currentCharacter);
                        currentLine.Append(csvText[i + 1]);
                        i++;
                        continue;
                    }

                    isInsideQuotes = !isInsideQuotes;
                    currentLine.Append(currentCharacter);
                    continue;
                }

                if (currentCharacter == '\n' && !isInsideQuotes)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    continue;
                }

                currentLine.Append(currentCharacter);
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            return lines;
        }
    }
}
#endif