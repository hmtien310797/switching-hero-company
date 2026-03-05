#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Immortal_Switch.Scripts;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class CreepDataSoGeneratorWindow : EditorWindow
    {
        [Header("Input")]
        [SerializeField] private TextAsset csvFile;

        [Header("Output")]
        [SerializeField] private DefaultAsset outputFolder;

        [Header("Options")]
        [SerializeField] private bool overwriteExisting = true;
        [SerializeField] private bool useNameInFileName = true;
        [SerializeField] private bool skipEmptyLines = true;

        [MenuItem("Tools/Immortal Switch/Generate CreepData SO From CSV (v2)")]
        public static void Open()
        {
            var w = GetWindow<CreepDataSoGeneratorWindow>("CreepData SO Generator v2");
            w.minSize = new Vector2(560, 280);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Generate CreepDataSo Assets From CSV", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
            outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);

            EditorGUILayout.Space(6);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
            useNameInFileName = EditorGUILayout.Toggle("Use Name In Filename", useNameInFileName);
            skipEmptyLines = EditorGUILayout.Toggle("Skip Empty Lines", skipEmptyLines);

            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(csvFile == null || outputFolder == null))
            {
                if (GUILayout.Button("Generate ScriptableObjects", GUILayout.Height(36)))
                {
                    try
                    {
                        Generate();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                        EditorUtility.DisplayDialog("Error", ex.Message, "OK");
                    }
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Expected CSV header:\n" +
                "id,name,element,BaseHP,BaseATK,BaseDEF,atkSpeed,range,moveSpeed,notes\n\n" +
                "Notes is ignored.\n" +
                "Element must match your Element enum values (case-insensitive).",
                MessageType.Info);
        }

        private void Generate()
        {
            if (csvFile == null) throw new Exception("CSV File is null.");
            if (outputFolder == null) throw new Exception("Output Folder is null.");

            var folderPath = AssetDatabase.GetAssetPath(outputFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
                throw new Exception("Output Folder is not a valid folder asset.");

            string text = csvFile.text ?? "";
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            if (lines.Length < 2)
                throw new Exception("CSV must contain header + at least 1 data row.");

            var header = SplitCsvLine(lines[0]).ToArray();
            var col = BuildColumnIndex(header);

            // Required columns
            Require(col, "id");
            Require(col, "name");
            Require(col, "element");
            Require(col, "BaseHP");
            Require(col, "BaseATK");
            Require(col, "BaseDEF");
            Require(col, "atkSpeed");
            Require(col, "range");
            Require(col, "moveSpeed");
            // notes optional, ignored

            int created = 0, updated = 0, skipped = 0, errors = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                string raw = lines[i];
                if (skipEmptyLines && string.IsNullOrWhiteSpace(raw)) continue;

                var cols = SplitCsvLine(raw);
                if (cols.Count == 0) continue;

                // tolerate last trailing line
                if (cols.Count < header.Length)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    Debug.LogWarning($"Row {i + 1}: column count mismatch. Skipped.\n{raw}");
                    skipped++;
                    continue;
                }

                try
                {
                    int id = ParseInt(cols[col["id"]], $"Row {i + 1} id");
                    string nameStr = cols[col["name"]].Trim();
                    string elementStr = cols[col["element"]].Trim();

                    float hp = ParseFloat(cols[col["BaseHP"]], $"Row {i + 1} BaseHP");
                    float atk = ParseFloat(cols[col["BaseATK"]], $"Row {i + 1} BaseATK");
                    float def = ParseFloat(cols[col["BaseDEF"]], $"Row {i + 1} BaseDEF");
                    float atkSpeed = ParseFloat(cols[col["atkSpeed"]], $"Row {i + 1} atkSpeed");
                    float range = ParseFloat(cols[col["range"]], $"Row {i + 1} range");
                    float moveSpeed = ParseFloat(cols[col["moveSpeed"]], $"Row {i + 1} moveSpeed");

                    if (!Enum.TryParse(elementStr, true, out Element element))
                        throw new Exception($"Row {i + 1}: cannot parse Element from '{elementStr}'. Check enum.");

                    int nameId = StableStringHashToInt(nameStr);

                    string assetName = BuildAssetName(id, nameStr);
                    string assetPath = Path.Combine(folderPath, assetName + ".asset").Replace("\\", "/");

                    var so = AssetDatabase.LoadAssetAtPath<CreepDataSo>(assetPath);

                    if (so != null && !overwriteExisting)
                    {
                        skipped++;
                        continue;
                    }

                    bool isNew = (so == null);
                    if (isNew)
                    {
                        so = ScriptableObject.CreateInstance<CreepDataSo>();
                        AssetDatabase.CreateAsset(so, assetPath);
                        created++;
                    }
                    else
                    {
                        updated++;
                    }

                    // Assign (notes ignored)
                    so.Id = id;
                    so.Name = nameId;
                    so.Element = element;
                    so.BaseHp = hp;
                    so.BaseAtk = atk;
                    so.BaseDef = def;
                    so.BaseAtkSpeed = atkSpeed;
                    so.BaseRange = range;
                    so.BaseMoveSpeed = moveSpeed;

                    EditorUtility.SetDirty(so);
                }
                catch (Exception rowEx)
                {
                    errors++;
                    Debug.LogError($"CSV Import Error: {rowEx.Message}\nRow content: {raw}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Done",
                $"Created: {created}\nUpdated: {updated}\nSkipped: {skipped}\nErrors: {errors}",
                "OK");
        }

        private string BuildAssetName(int id, string nameStr)
        {
            if (!useNameInFileName || string.IsNullOrWhiteSpace(nameStr))
                return $"CreepData_{id}";

            return $"CreepData_{id}_{SanitizeFileName(nameStr)}";
        }

        private static void Require(Dictionary<string, int> map, string colName)
        {
            if (!map.ContainsKey(colName))
                throw new Exception($"Missing required column '{colName}' in CSV header.");
        }

        private static Dictionary<string, int> BuildColumnIndex(string[] header)
        {
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
            {
                string key = (header[i] ?? "").Trim();
                if (string.IsNullOrEmpty(key)) continue;
                if (!dict.ContainsKey(key)) dict.Add(key, i);
            }
            return dict;
        }

        // CSV splitter supporting quoted fields + escaped quotes ""
        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            bool inQuotes = false;
            var token = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        token.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(token.ToString().Trim());
                    token.Clear();
                }
                else
                {
                    token.Append(c);
                }
            }

            result.Add(token.ToString().Trim());
            return result;
        }

        private static int ParseInt(string s, string context)
        {
            s = (s ?? "").Trim();
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                throw new Exception($"Cannot parse int: {context} = '{s}'");
            return v;
        }

        private static float ParseFloat(string s, string context)
        {
            // support 1,5 and 1.5
            s = (s ?? "").Trim().Replace(",", ".");
            if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                throw new Exception($"Cannot parse float: {context} = '{s}'");
            return v;
        }

        private static string SanitizeFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unnamed";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Replace(" ", "_");
        }

        /// <summary>
        /// Deterministic hash -> int (stable across sessions/builds).
        /// Good for mapping name string to int id without needing a string field.
        /// </summary>
        private static int StableStringHashToInt(string s)
        {
            // FNV-1a 32-bit
            unchecked
            {
                const uint fnvOffset = 2166136261u;
                const uint fnvPrime = 16777619u;

                uint hash = fnvOffset;
                s ??= "";
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= fnvPrime;
                }
                // Keep positive int (optional)
                return (int)(hash & 0x7FFFFFFF);
            }
        }
    }
}
#endif