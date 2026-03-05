using System;
using System.Globalization;
using System.IO;
using System.Text;
using Immortal_Switch.Scripts;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class HeroDataCsvImporterWindow : EditorWindow
    {
        [Header("Input")] [SerializeField] private TextAsset csvFile;

        [Header("Output Folder (must be under Assets/)")] [SerializeField]
        private string outputFolder = "Assets/Immortal Switch/GameData/Heroes";

        [Header("Options")] [SerializeField] private bool overwriteExisting = true;
        [SerializeField] private bool logEachHero = false;

        [MenuItem("Tools/Immortal Switch/Import HeroData (CSV)")]
        public static void Open()
        {
            var w = GetWindow<HeroDataCsvImporterWindow>("HeroData CSV Importer");
            w.minSize = new Vector2(520, 260);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("HeroData CSV Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

            EditorGUILayout.Space(6);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
            logEachHero = EditorGUILayout.Toggle("Log Each Hero", logEachHero);

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(csvFile == null))
            {
                if (GUILayout.Button("Import / Update HeroDataSO", GUILayout.Height(36)))
                {
                    try
                    {
                        Import();
                        EditorUtility.DisplayDialog("Import done", "HeroDataSO imported/updated successfully.", "OK");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        EditorUtility.DisplayDialog("Import failed", e.Message, "OK");
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "CSV needs headers like:\n" +
                "hero_Id,class,rarity,Hp,ATK,Def,Critchange,Critdamage,ATKSPD,(optional)AttackRange\n\n" +
                "Note: tool handles decimal comma (0,05) and decimal dot (0.05).",
                MessageType.Info);
        }

        private void Import()
        {
            if (csvFile == null) throw new InvalidOperationException("CSV file is null.");
            if (!outputFolder.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Output folder must start with 'Assets/'.");

            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                CreateFoldersRecursively(outputFolder);
            }

            var lines = ReadAllLines(csvFile.text);
            if (lines.Length < 2) throw new InvalidOperationException("CSV has no data rows.");

            // Parse header
            // Parse header
            var header = SplitCsvLine(lines[0]);
            int colName = FindCol(header, "heroID", "hero_Id", "HeroId", "Name");
            int colClass  = FindCol(header, "class", "HeroClass");
            int colRarity = FindCol(header, "rarity", "Rarity");
            int colHp     = FindCol(header, "Hp", "HP", "HitPoint");
            int colAtk    = FindCol(header, "ATK", "Attack");
            int colDef    = FindCol(header, "Def", "DEF", "Defense");
            int colCritChance = FindCol(header, "Critchange", "CritChance", "Critchance");
            int colCritDmg    = FindCol(header, "Critdamage", "CritDamage");
            int colAtkSpd     = FindCol(header, "ATKSPD", "AttackSpeed", "AtkSpd");
            int colRange      = FindColOptional(header, "AttackRange", "Range", "ATKRANGE");

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var row = SplitCsvLine(lines[i]);

                string heroName = GetCell(row, colName).Trim();
                if (string.IsNullOrEmpty(heroName)) continue;

                // dùng heroName làm key + tên asset
                string assetPath = $"{outputFolder}/{MakeSafeFileName(heroName)}.asset";

                HeroDataSO so = AssetDatabase.LoadAssetAtPath<HeroDataSO>(assetPath);
                bool isNew = so == null;
                if (isNew) so = ScriptableObject.CreateInstance<HeroDataSO>();

                so.Name = heroName;

                so.HeroClass = ParseEnum<HeroClass>(GetCell(row, colClass), "HeroClass", heroName);
                so.Rarity    = ParseEnum<Rarity>(GetCell(row, colRarity), "Rarity", heroName);

                so.HitPoint    = ParseFloat(GetCell(row, colHp));
                so.Attack      = ParseFloat(GetCell(row, colAtk));
                so.Defense     = ParseFloat(GetCell(row, colDef));
                so.CritChance  = ParseFloat(GetCell(row, colCritChance));
                so.CritDamage  = ParseFloat(GetCell(row, colCritDmg));
                so.AttackSpeed = ParseFloat(GetCell(row, colAtkSpd));
                so.AttackRange = colRange >= 0 ? ParseFloat(GetCell(row, colRange)) : 1f;

                if (isNew) AssetDatabase.CreateAsset(so, assetPath);
                else EditorUtility.SetDirty(so);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //Debug.Log($"HeroData Import finished. Created={created}, Updated={updated}, Skipped={skipped}");
        }

        // ---------- Helpers ----------

        private static void CreateFoldersRecursively(string assetsPath)
        {
            // assetsPath like: Assets/A/B/C
            var parts = assetsPath.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets") return;

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string[] ReadAllLines(string text)
        {
            // Normalize line endings
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            return text.Split('\n');
        }

        private static int FindCol(string[] header, params string[] names)
        {
            int idx = FindColOptional(header, names);
            if (idx < 0)
                throw new InvalidOperationException($"Missing required column: {string.Join(" / ", names)}");
            return idx;
        }

        private static int FindColOptional(string[] header, params string[] names)
        {
            for (int i = 0; i < header.Length; i++)
            {
                var h = header[i].Trim().Trim('"');
                foreach (var n in names)
                {
                    if (string.Equals(h, n, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return -1;
        }

        private static string GetCell(string[] row, int index)
        {
            if (index < 0) return "";
            if (row == null) return "";
            if (index >= row.Length) return "";
            return row[index]?.Trim().Trim('"') ?? "";
        }

        private static T ParseEnum<T>(string raw, string fieldName, string heroId) where T : struct
        {
            raw = (raw ?? "").Trim().Trim('"');

            // allow things like "Warrior " or "warrior"
            if (Enum.TryParse<T>(raw, true, out var result))
                return result;

            throw new InvalidOperationException($"Invalid {fieldName} '{raw}' for hero '{heroId}'.");
        }

        private static float ParseFloat(string raw)
        {
            raw = (raw ?? "").Trim().Trim('"');
            if (string.IsNullOrEmpty(raw)) return 0f;

            // Handle decimal comma: "0,05" -> "0.05"
            raw = raw.Replace(" ", "");
            raw = raw.Replace(',', '.');

            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;

            return 0f;
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name.Trim();
        }

        // Minimal CSV splitter that handles quoted commas
        private static string[] SplitCsvLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];

                if (ch == '"')
                {
                    // double quotes inside quoted string -> escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                sb.Append(ch);
            }

            result.Add(sb.ToString());
            return result.ToArray();
        }
    }
}