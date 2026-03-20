using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Immortal_Switch.Scripts;
using UnityEditor;
using UnityEngine;

public class HeroCsvToSoTool : EditorWindow
{
    private TextAsset csvFile;
    private DefaultAsset outputFolder;

    [MenuItem("Tools/CSV/Hero CSV To ScriptableObject")]
    public static void ShowWindow()
    {
        GetWindow<HeroCsvToSoTool>("Hero CSV Parser");
    }

    private void OnGUI()
    {
        GUILayout.Label("Hero CSV -> HeroDataSO", EditorStyles.boldLabel);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Parse CSV And Create SO"))
        {
            CreateHeroSOs();
        }
    }

    private void CreateHeroSOs()
    {
        if (csvFile == null)
        {
            EditorUtility.DisplayDialog("Error", "Vui lòng chọn file CSV.", "OK");
            return;
        }

        if (outputFolder == null)
        {
            EditorUtility.DisplayDialog("Error", "Vui lòng chọn folder output.", "OK");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(outputFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Output Folder không hợp lệ.", "OK");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            EditorUtility.DisplayDialog("Error", "CSV không có dữ liệu.", "OK");
            return;
        }

        int createdCount = 0;
        int updatedCount = 0;

        // Bỏ dòng header
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            List<string> columns = ParseCsvLine(line);

            // CSV của bạn có 12 cột
            if (columns.Count < 12)
            {
                Debug.LogWarning($"[HeroCsvToSoTool] Dòng {i + 1} không đủ cột: {line}");
                continue;
            }

            try
            {
                HeroRow row = ConvertRow(columns);

                string assetName = $"{row.Id}_{row.HeroName}.asset";
                string assetPath = Path.Combine(folderPath, assetName).Replace("\\", "/");

                HeroDataSO heroData = AssetDatabase.LoadAssetAtPath<HeroDataSO>(assetPath);

                if (heroData == null)
                {
                    heroData = ScriptableObject.CreateInstance<HeroDataSO>();
                    FillHeroData(heroData, row);
                    AssetDatabase.CreateAsset(heroData, assetPath);
                    createdCount++;
                }
                else
                {
                    FillHeroData(heroData, row);
                    EditorUtility.SetDirty(heroData);
                    updatedCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HeroCsvToSoTool] Lỗi parse dòng {i + 1}: {line}\n{ex}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Done",
            $"Parse hoàn tất.\nCreated: {createdCount}\nUpdated: {updatedCount}",
            "OK"
        );
    }

    private static void FillHeroData(HeroDataSO heroData, HeroRow row)
    {
        heroData.Id = row.Id;
        heroData.Name = row.HeroName;
        heroData.HeroClass = row.HeroClass;
        heroData.Rarity = row.Rarity;
        heroData.Element = row.Element;
        heroData.Health = row.HitPoint;
        heroData.Attack = row.Attack;
        heroData.Defense = row.Defense;
        heroData.CritChance = row.CritChance;
        heroData.CritDamage = row.CritDamage;
        heroData.AttackSpeed = row.AttackSpeed;
        heroData.AttackRange = row.AttackRange;
    }

    private static HeroRow ConvertRow(List<string> cols)
    {
        HeroRow row = new HeroRow
        {
            Id = ParseInt(cols[0]),
            HeroName = cols[1],
            HeroClass = ParseEnum<HeroClass>(cols[2]),
            Rarity = ParseEnum<Rarity>(cols[3]),
            Element = ParseEnum<Element>(cols[4]),
            HitPoint = ParseFloat(cols[5]),
            Attack = ParseFloat(cols[6]),
            Defense = ParseFloat(cols[7]),
            CritChance = ParseFloat(cols[8]),
            CritDamage = ParseFloat(cols[9]),
            AttackSpeed = ParseFloat(cols[10]),
            AttackRange = ParseFloat(cols[11])
        };

        return row;
    }

    private static int ParseInt(string value)
    {
        value = value.Trim();
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static float ParseFloat(string value)
    {
        value = value.Trim();

        // xử lý trường hợp như 01.05
        // và kiểu số dùng dấu phẩy thập phân như 0,05 hoặc 1,5
        value = value.Replace(",", ".");

        return float.Parse(value, CultureInfo.InvariantCulture);
    }

    private static T ParseEnum<T>(string value) where T : struct
    {
        value = value.Trim();

        if (Enum.TryParse(value, true, out T result))
            return result;

        throw new Exception($"Không parse được enum {typeof(T).Name} từ value: {value}");
    }

    /// <summary>
    /// Parser đơn giản cho 1 dòng CSV.
    /// Hỗ trợ field có dấu nháy kép.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == '\t' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else if (c == ',' && !inQuotes)
            {
                // CSV của bạn nhìn giống tab-separated hơn là comma-separated
                // nên chỗ này chỉ dùng nếu file thực sự ngăn bằng dấu phẩy.
                current += c;
            }
            else
            {
                current += c;
            }
        }

        result.Add(current.Trim());
        return result;
    }

    private class HeroRow
    {
        public int Id;
        public string HeroName;
        public HeroClass HeroClass;
        public Rarity Rarity;
        public Element Element;
        public float HitPoint;
        public float Attack;
        public float Defense;
        public float CritChance;
        public float CritDamage;
        public float AttackSpeed;
        public float AttackRange;
    }
}