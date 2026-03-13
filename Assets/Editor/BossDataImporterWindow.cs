using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Skill;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class BossDataImporterWindow : EditorWindow
    {
        private TextAsset bossStatFile;
        private TextAsset bossSkillFile;
        private string outputFolder = "Assets/Immortal Switch/Addressable/BossData";
        private bool overwriteIfExists = true;

        [MenuItem("Tools/CSV/Boss Data Importer")]
        public static void ShowWindow()
        {
            GetWindow<BossDataImporterWindow>("Boss Data Importer");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Boss Data Importer", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Import boss stat + boss skill từ 2 file raw text/csv/tab-separated vào BossDataSO.",
                MessageType.Info);

            GUILayout.Space(8);

            bossStatFile = (TextAsset)EditorGUILayout.ObjectField("Boss Stat File", bossStatFile, typeof(TextAsset), false);
            bossSkillFile = (TextAsset)EditorGUILayout.ObjectField("Boss Skill File", bossSkillFile, typeof(TextAsset), false);

            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            overwriteIfExists = EditorGUILayout.Toggle("Overwrite If Exists", overwriteIfExists);

            GUILayout.Space(12);

            if (GUILayout.Button("Import Boss Data", GUILayout.Height(34)))
            {
                ImportBossData();
            }
        }

        private void ImportBossData()
        {
            if (bossStatFile == null)
            {
                Debug.LogError("Chưa gán Boss Stat File.");
                return;
            }

            if (bossSkillFile == null)
            {
                Debug.LogError("Chưa gán Boss Skill File.");
                return;
            }

            EnsureFolderExists(outputFolder);

            Dictionary<int, BossStatRow> statMap = ParseBossStatFile(bossStatFile.text);
            Dictionary<int, BossSkillRow> skillMap = ParseBossSkillFile(bossSkillFile.text);

            int successCount = 0;
            int failCount = 0;

            foreach (var kvp in statMap)
            {
                int bossId = kvp.Key;
                BossStatRow statRow = kvp.Value;

                try
                {
                    BossDataSO asset = GetOrCreateBossAsset(statRow, out string assetPath, out bool createdNew);

                    asset.Id = statRow.Id;
                    asset.Name = statRow.Name;
                    asset.Element = statRow.Element;
                    asset.BaseHP = statRow.BaseHP;
                    asset.BaseAtk = statRow.BaseAtk;
                    asset.BaseDef = statRow.BaseDef;
                    asset.AtkSpeed = statRow.AtkSpeed;
                    asset.AttackRange = statRow.Range;
                    asset.MoveSpeed = statRow.MoveSpeed;

                    EditorUtility.SetDirty(asset);

                    Debug.Log($"{(createdNew ? "Created" : "Updated")}: {assetPath}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Import lỗi Boss ID {bossId}\n{ex}");
                    failCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Import hoàn tất. Success: {successCount}, Failed: {failCount}");
        }

        private BossDataSO GetOrCreateBossAsset(BossStatRow row, out string assetPath, out bool createdNew)
        {
            string safeName = MakeSafeFileName($"Boss_{row.Id}_{row.Name}");
            assetPath = $"{outputFolder}/{safeName}.asset";

            BossDataSO existing = AssetDatabase.LoadAssetAtPath<BossDataSO>(assetPath);
            if (existing != null)
            {
                createdNew = false;

                if (!overwriteIfExists)
                    throw new Exception($"Asset đã tồn tại và overwrite đang tắt: {assetPath}");

                return existing;
            }

            BossDataSO asset = CreateInstance<BossDataSO>();
            AssetDatabase.CreateAsset(asset, assetPath);
            createdNew = true;
            return asset;
        }

        private Dictionary<int, BossStatRow> ParseBossStatFile(string raw)
        {
            var result = new Dictionary<int, BossStatRow>();
            List<string> lines = GetValidLines(raw);

            if (lines.Count <= 1)
                throw new Exception("Boss stat file không đủ dữ liệu.");

            for (int i = 1; i < lines.Count; i++)
            {
                string line = lines[i];
                try
                {
                    string[] cols = SplitLine(line);

                    if (cols.Length < 9)
                        throw new Exception($"Không đủ cột boss stat. Line: {line}");

                    BossStatRow row = new BossStatRow
                    {
                        Id = ParseInt(cols[0]),
                        Name = cols[1].Trim(),
                        Element = ParseElement(cols[2]),
                        BaseHP = ParseFloat(cols[3]),
                        BaseAtk = ParseFloat(cols[4]),
                        BaseDef = ParseFloat(cols[5]),
                        AtkSpeed = ParseFloat(cols[6]),
                        Range = ParseFloat(cols[7]),
                        MoveSpeed = ParseFloat(cols[8]),
                    };

                    result[row.Id] = row;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Parse boss stat lỗi ở line {i + 1}: {line}\n{ex}");
                }
            }

            return result;
        }

        private Dictionary<int, BossSkillRow> ParseBossSkillFile(string raw)
        {
            var result = new Dictionary<int, BossSkillRow>();
            List<string> lines = GetValidLines(raw);

            if (lines.Count <= 1)
                throw new Exception("Boss skill file không đủ dữ liệu.");

            for (int i = 1; i < lines.Count; i++)
            {
                string line = lines[i];
                try
                {
                    string[] cols = SplitLine(line);

                    if (cols.Length < 6)
                        throw new Exception($"Không đủ cột boss skill. Line: {line}");

                    BossSkillRow row = new BossSkillRow
                    {
                        SkillId = ParseInt(cols[0]),
                        BossId = ParseInt(cols[1]),
                        PassiveSkill = cols[2].Trim(),
                        PassiveSkillDescription = cols[3].Trim(),
                        ActiveSkill = cols[4].Trim(),
                        ActiveSkillDescription = cols[5].Trim(),
                    };

                    result[row.BossId] = row;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Parse boss skill lỗi ở line {i + 1}: {line}\n{ex}");
                }
            }

            return result;
        }

        private static List<string> GetValidLines(string raw)
        {
            string[] split = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> result = new List<string>();

            foreach (string line in split)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    result.Add(line.Trim());
            }

            return result;
        }

        private static string[] SplitLine(string line)
        {
            if (line.Contains("\t"))
                return line.Split('\t');

            return SplitCsv(line);
        }

        private static string[] SplitCsv(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }

            values.Add(current.Trim());
            return values.ToArray();
        }

        private static int ParseInt(string value)
        {
            value = value.Trim().Replace("\"", "");
            return int.Parse(value, CultureInfo.InvariantCulture);
        }

        private static float ParseFloat(string value)
        {
            value = value.Trim().Replace("\"", "");
            value = value.Replace(',', '.');
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static Element ParseElement(string value)
        {
            string v = value.Trim().ToLowerInvariant();

            return v switch
            {
                "fire" => Element.Fire,
                "water" => Element.Water,
                "earth" => Element.Earth,
                _ => throw new Exception($"Element không hợp lệ: {value}")
            };
        }

        private static string MakeSafeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            fileName = fileName.Replace(" ", "");
            return fileName;
        }

        private static void EnsureFolderExists(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];

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

        private class BossStatRow
        {
            public int Id;
            public string Name;
            public Element Element;
            public float BaseHP;
            public float BaseAtk;
            public float BaseDef;
            public float AtkSpeed;
            public float Range;
            public float MoveSpeed;
        }

        private class BossSkillRow
        {
            public int SkillId;
            public int BossId;
            public string PassiveSkill;
            public string PassiveSkillDescription;
            public string ActiveSkill;
            public string ActiveSkillDescription;
        }
    }
}