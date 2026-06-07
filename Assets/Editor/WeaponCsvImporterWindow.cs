#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Battle;
using Immortal_Switch.Scripts.Core;
using Immortal_Switch.Scripts.Equipment.Core;
using Immortal_Switch.Scripts.Equipment.Definitions;
using Immortal_Switch.Scripts.Hero;
using Immortal_Switch.Scripts.StatSystem;
using UnityEditor;
using UnityEngine;

namespace Immortal_Switch.Scripts.Equipment.Editor
{
    public class WeaponCsvImporterWindow : EditorWindow
    {
        private TextAsset standardWeaponCsv;
        private TextAsset exclusiveWeaponCsv;
        private TextAsset weaponStatCsv;
        private TextAsset weaponLevelConfigCsv;
        private TextAsset weaponLimitBreakConfigCsv;

        private string databaseAssetPath = "Assets/Data/Equipment/WeaponDatabase.asset";
        private string standardWeaponFolder = "Assets/Data/Equipment/StandardWeapons";
        private string exclusiveWeaponFolder = "Assets/Data/Equipment/ExclusiveWeapons";
        private string levelConfigFolder = "Assets/Data/Equipment/LevelConfigs";
        private string limitBreakConfigFolder = "Assets/Data/Equipment/LimitBreakConfigs";

        private const string PrefKeyStandardCsv = "WeaponCsvImporter.StandardCsv";
        private const string PrefKeyExclusiveCsv = "WeaponCsvImporter.ExclusiveCsv";
        private const string PrefKeyWeaponStatCsv = "WeaponCsvImporter.WeaponStatCsv";
        private const string PrefKeyWeaponLevelConfigCsv = "WeaponCsvImporter.LevelConfigCsv";
        private const string PrefKeyWeaponLimitBreakConfigCsv = "WeaponCsvImporter.LimitBreakConfigCsv";

        private const string PrefKeyDatabaseAssetPath = "WeaponCsvImporter.DatabaseAssetPath";
        private const string PrefKeyStandardFolder = "WeaponCsvImporter.StandardFolder";
        private const string PrefKeyExclusiveFolder = "WeaponCsvImporter.ExclusiveFolder";
        private const string PrefKeyLevelConfigFolder = "WeaponCsvImporter.LevelConfigFolder";
        private const string PrefKeyLimitBreakConfigFolder = "WeaponCsvImporter.LimitBreakConfigFolder";

        private void OnEnable()
        {
            LoadPrefs();
        }

        private void LoadPrefs()
        {
            standardWeaponCsv = LoadAssetFromPrefs<TextAsset>(PrefKeyStandardCsv);
            exclusiveWeaponCsv = LoadAssetFromPrefs<TextAsset>(PrefKeyExclusiveCsv);
            weaponStatCsv = LoadAssetFromPrefs<TextAsset>(PrefKeyWeaponStatCsv);
            weaponLevelConfigCsv = LoadAssetFromPrefs<TextAsset>(PrefKeyWeaponLevelConfigCsv);
            weaponLimitBreakConfigCsv = LoadAssetFromPrefs<TextAsset>(PrefKeyWeaponLimitBreakConfigCsv);

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
            SaveAssetToPrefs(PrefKeyStandardCsv, standardWeaponCsv);
            SaveAssetToPrefs(PrefKeyExclusiveCsv, exclusiveWeaponCsv);
            SaveAssetToPrefs(PrefKeyWeaponStatCsv, weaponStatCsv);
            SaveAssetToPrefs(PrefKeyWeaponLevelConfigCsv, weaponLevelConfigCsv);
            SaveAssetToPrefs(PrefKeyWeaponLimitBreakConfigCsv, weaponLimitBreakConfigCsv);

            EditorPrefs.SetString(PrefKeyDatabaseAssetPath, databaseAssetPath ?? string.Empty);
            EditorPrefs.SetString(PrefKeyStandardFolder, standardWeaponFolder ?? string.Empty);
            EditorPrefs.SetString(PrefKeyExclusiveFolder, exclusiveWeaponFolder ?? string.Empty);
            EditorPrefs.SetString(PrefKeyLevelConfigFolder, levelConfigFolder ?? string.Empty);
            EditorPrefs.SetString(PrefKeyLimitBreakConfigFolder, limitBreakConfigFolder ?? string.Empty);
        }

        private static void SaveAssetToPrefs(string key, UnityEngine.Object asset)
        {
            string path = asset != null ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            EditorPrefs.SetString(key, path);
        }

        private static T LoadAssetFromPrefs<T>(string key) where T : UnityEngine.Object
        {
            string path = EditorPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        [MenuItem("Tools/Equipment/Weapon CSV Importer")]
        public static void Open()
        {
            GetWindow<WeaponCsvImporterWindow>("Weapon CSV Importer");
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("CSV Sources", EditorStyles.boldLabel);

            standardWeaponCsv =
                (TextAsset)EditorGUILayout.ObjectField("StandardWeapon CSV", standardWeaponCsv, typeof(TextAsset),
                    false);
            exclusiveWeaponCsv = (TextAsset)EditorGUILayout.ObjectField("ExclusiveWeapon CSV", exclusiveWeaponCsv,
                typeof(TextAsset), false);
            weaponStatCsv =
                (TextAsset)EditorGUILayout.ObjectField("WeaponStat CSV", weaponStatCsv, typeof(TextAsset), false);
            weaponLevelConfigCsv = (TextAsset)EditorGUILayout.ObjectField("WeaponLevelConfig CSV", weaponLevelConfigCsv,
                typeof(TextAsset), false);
            weaponLimitBreakConfigCsv = (TextAsset)EditorGUILayout.ObjectField("WeaponLimitBreakConfig CSV",
                weaponLimitBreakConfigCsv, typeof(TextAsset), false);

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

            using (new EditorGUI.DisabledScope(
                       standardWeaponCsv == null ||
                       exclusiveWeaponCsv == null ||
                       weaponStatCsv == null ||
                       weaponLevelConfigCsv == null ||
                       weaponLimitBreakConfigCsv == null))
            {
                if (GUILayout.Button("Import / Update Weapon Data", GUILayout.Height(32)))
                {
                    try
                    {
                        SavePrefs();
                        ImportAll();
                        EditorUtility.DisplayDialog("Success", "Import weapon CSV completed.", "OK");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        EditorUtility.DisplayDialog("Import Failed", ex.Message, "OK");
                    }
                }
            }
        }

        private void ImportAll()
        {
            EnsureFolder(standardWeaponFolder);
            EnsureFolder(exclusiveWeaponFolder);
            EnsureFolder(levelConfigFolder);
            EnsureFolder(limitBreakConfigFolder);
            EnsureFolder(Path.GetDirectoryName(databaseAssetPath)?.Replace("\\", "/"));

            var standardRows = CsvUtility.ReadRows(AssetDatabase.GetAssetPath(standardWeaponCsv));
            var exclusiveRows = CsvUtility.ReadRows(AssetDatabase.GetAssetPath(exclusiveWeaponCsv));
            var statRows = CsvUtility.ReadRows(AssetDatabase.GetAssetPath(weaponStatCsv));
            var levelRows = CsvUtility.ReadRows(AssetDatabase.GetAssetPath(weaponLevelConfigCsv));
            var limitBreakRows = CsvUtility.ReadRows(AssetDatabase.GetAssetPath(weaponLimitBreakConfigCsv));

            var statMap = BuildStatMap(statRows);
            var levelConfigMap = BuildLevelConfigs(levelRows);
            var limitBreakConfigMap = BuildLimitBreakConfigs(limitBreakRows);

            var standardAssets = ImportStandardWeapons(standardRows, statMap, levelConfigMap, limitBreakConfigMap);
            var exclusiveAssets = ImportExclusiveWeapons(exclusiveRows, statMap, levelConfigMap, limitBreakConfigMap);

            BuildDatabase(
                standardAssets,
                exclusiveAssets,
                levelConfigMap.Values.ToList(),
                limitBreakConfigMap.Values.ToList()
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
                string iconName = GetOptional(row, "Icon");
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
                asset.Icon = FindSpriteByName(iconName);
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
                string iconName = GetOptional(row, "Icon");
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
                asset.Icon = FindSpriteByName(iconName);
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

    internal static class CsvUtility
    {
        public static List<Dictionary<string, string>> ReadRows(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new Exception("CSV asset path is null or empty.");

            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
                throw new Exception($"CSV file not found: {fullPath}");

            string[] lines = File.ReadAllLines(fullPath);
            if (lines.Length <= 1)
                return new List<Dictionary<string, string>>();

            var headers = ParseCsvLine(lines[0]).Select(x => x.Trim()).ToList();
            var rows = new List<Dictionary<string, string>>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var values = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int h = 0; h < headers.Count; h++)
                {
                    string value = h < values.Count ? values[h] : string.Empty;
                    row[headers[h]] = value;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null)
            {
                result.Add(string.Empty);
                return result;
            }

            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            result.Add(current.ToString());
            return result;
        }
    }
}
#endif