#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Battle;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Level.Stage;
using UnityEditor;
using UnityEngine;

public class StageConfigCsvImporterWindow : EditorWindow
{
    [Header("CSV TextAssets")] [SerializeField]
    private TextAsset chaptersCsv;

    [SerializeField] private TextAsset enemyPatternRulesCsv;
    [SerializeField] private TextAsset enemyPatternsCsv;
    [SerializeField] private TextAsset bossPatternRulesCsv;
    [SerializeField] private TextAsset rewardRulesCsv;
    [SerializeField] private TextAsset stageScalingRulesCsv;

    [Header("Import Source")] [SerializeField]
    private bool useCsvUrls = true;

    private static string chaptersCsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1136361234&single=true&output=csv";
    private static string enemyPatternRulesCsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=58914796&single=true&output=csv";
    private static string enemyPatternsCsvUrl ="https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=538218169&single=true&output=csv";
    private static string bossPatternRulesCsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1880814643&single=true&output=csv";
    private static string rewardRulesCsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=861091833&single=true&output=csv";
    private static string stageScalingRulesCsvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=844833132&single=true&output=csv";

    [Header("Output Folder")] [SerializeField]
    private string outputFolder = "Assets/_Project/Generated/StageConfig";

    [MenuItem("Tools/Stage Config/CSV Importer")]
    private static void Open()
    {
        GetWindow<StageConfigCsvImporterWindow>("Stage Config Importer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Stage Config CSV Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        useCsvUrls = EditorGUILayout.Toggle("Use CSV URLs", useCsvUrls);

        EditorGUILayout.Space(8);

        if (useCsvUrls)
        {
            EditorGUILayout.LabelField("Google Sheet Published CSV URLs", EditorStyles.boldLabel);

            chaptersCsvUrl = EditorGUILayout.TextField("Chapters CSV URL", chaptersCsvUrl);
            enemyPatternRulesCsvUrl = EditorGUILayout.TextField("EnemyPatternRules CSV URL", enemyPatternRulesCsvUrl);
            enemyPatternsCsvUrl = EditorGUILayout.TextField("EnemyPatterns CSV URL", enemyPatternsCsvUrl);
            bossPatternRulesCsvUrl = EditorGUILayout.TextField("BossPatternRules CSV URL", bossPatternRulesCsvUrl);
            rewardRulesCsvUrl = EditorGUILayout.TextField("RewardRules CSV URL", rewardRulesCsvUrl);
            stageScalingRulesCsvUrl = EditorGUILayout.TextField("StageScalingRules CSV URL", stageScalingRulesCsvUrl);
        }
        else
        {
            EditorGUILayout.LabelField("Local CSV TextAssets", EditorStyles.boldLabel);

            chaptersCsv = (TextAsset)EditorGUILayout.ObjectField("Chapters CSV", chaptersCsv, typeof(TextAsset), false);
            enemyPatternRulesCsv = (TextAsset)EditorGUILayout.ObjectField("EnemyPatternRules CSV", enemyPatternRulesCsv,
                typeof(TextAsset), false);
            enemyPatternsCsv =
                (TextAsset)EditorGUILayout.ObjectField("EnemyPatterns CSV", enemyPatternsCsv, typeof(TextAsset), false);
            bossPatternRulesCsv = (TextAsset)EditorGUILayout.ObjectField("BossPatternRules CSV", bossPatternRulesCsv,
                typeof(TextAsset), false);
            rewardRulesCsv =
                (TextAsset)EditorGUILayout.ObjectField("RewardRules CSV", rewardRulesCsv, typeof(TextAsset), false);
            stageScalingRulesCsv = (TextAsset)EditorGUILayout.ObjectField("StageScalingRules CSV", stageScalingRulesCsv,
                typeof(TextAsset), false);
        }

        EditorGUILayout.Space(8);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        EditorGUILayout.Space(12);

        bool canImport = useCsvUrls
            ? !string.IsNullOrWhiteSpace(chaptersCsvUrl)
              && !string.IsNullOrWhiteSpace(enemyPatternRulesCsvUrl)
              && !string.IsNullOrWhiteSpace(enemyPatternsCsvUrl)
              && !string.IsNullOrWhiteSpace(bossPatternRulesCsvUrl)
            : chaptersCsv != null
              && enemyPatternRulesCsv != null
              && enemyPatternsCsv != null
              && bossPatternRulesCsv != null;

        using (new EditorGUI.DisabledScope(!canImport))
        {
            if (GUILayout.Button("Import Stage Config", GUILayout.Height(36)))
            {
                ImportAll();
            }
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Required: Chapters, EnemyPatternRules, EnemyPatterns, BossPatternRules.\n" +
            "Optional: RewardRules, StageScalingRules.\n\n" +
            "Google Sheet phải Publish to web dạng CSV cho từng tab.\n" +
            "Các cột list dùng dấu |, ví dụ: 3003|3006.",
            MessageType.Info
        );
    }
    
    private string GetCsvText(TextAsset localAsset, string url, string label, bool required)
    {
        if (useCsvUrls)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                if (required)
                    throw new Exception($"Missing required CSV URL: {label}");

                return string.Empty;
            }

            return DownloadCsv(url, label);
        }

        if (localAsset == null)
        {
            if (required)
                throw new Exception($"Missing required CSV TextAsset: {label}");

            return string.Empty;
        }

        return localAsset.text;
    }

    private static string DownloadCsv(string url, string label)
    {
        try
        {
            using WebClient client = new WebClient();
            client.Encoding = Encoding.UTF8;

            string text = client.DownloadString(url);

            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Downloaded CSV is empty.");

            Debug.Log($"[StageConfigImporter] Downloaded CSV: {label}");
            return text;
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to download CSV: {label}\nURL: {url}\nError: {e.Message}");
        }
    }

    private void ImportAll()
    {
        EnsureFolder(outputFolder);

        try
        {
            string chaptersText = GetCsvText(chaptersCsv, chaptersCsvUrl, "Chapters", true);
            string enemyPatternRulesText = GetCsvText(enemyPatternRulesCsv, enemyPatternRulesCsvUrl, "EnemyPatternRules", true);
            string enemyPatternsText = GetCsvText(enemyPatternsCsv, enemyPatternsCsvUrl, "EnemyPatterns", true);
            string bossPatternRulesText = GetCsvText(bossPatternRulesCsv, bossPatternRulesCsvUrl, "BossPatternRules", true);

            string rewardRulesText = GetCsvText(rewardRulesCsv, rewardRulesCsvUrl, "RewardRules", false);
            string stageScalingRulesText = GetCsvText(stageScalingRulesCsv, stageScalingRulesCsvUrl, "StageScalingRules", false);

            ChapterConfigSO chapterConfig = ImportChapters(chaptersText);
            EnemyPatternRuleSO enemyPatternRule = ImportEnemyPatternConfig(enemyPatternRulesText, enemyPatternsText);
            BossPatternRuleSO bossPatternRule = ImportBossPatternRules(bossPatternRulesText);

            RewardRuleSO rewardRule = null;
            if (!string.IsNullOrWhiteSpace(rewardRulesText))
                rewardRule = ImportRewardRules(rewardRulesText);

            StageScalingRuleSO scalingRule = null;
            if (!string.IsNullOrWhiteSpace(stageScalingRulesText))
                scalingRule = ImportStageScalingRules(stageScalingRulesText);

            EditorUtility.SetDirty(chapterConfig);
            EditorUtility.SetDirty(enemyPatternRule);
            EditorUtility.SetDirty(bossPatternRule);

            if (rewardRule != null)
                EditorUtility.SetDirty(rewardRule);

            if (scalingRule != null)
                EditorUtility.SetDirty(scalingRule);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[StageConfigImporter] Import completed.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[StageConfigImporter] Import failed:\n{e}");
        }
    }

    // =========================================================
    // Import Chapters
    // =========================================================

    private ChapterConfigSO ImportChapters(string csvText)
    {
        CsvTable table = CsvTable.Parse(csvText);

        ChapterConfigSO asset = LoadOrCreateAsset<ChapterConfigSO>("ChapterConfig.asset");

        List<ChapterConfig> chapters = new List<ChapterConfig>();

        foreach (CsvRow row in table.Rows)
        {
            if (row.IsEmpty)
                continue;

            ChapterConfig chapter = new ChapterConfig
            {
                ChapterId = row.GetInt("ChapterId"),
                ChapterName = row.GetString("ChapterName"),
                StageCount = Mathf.Max(1, row.GetInt("StageCount")),
                ChapterElement = row.GetEnum<Element>("ChapterElement"),
                RewardRuleId = row.GetString("RewardRuleId"),
                EnemyPatternRuleId = row.GetString("EnemyPatternRuleId"),
                BossPatternRuleId = row.GetString("BossPatternRuleId"),
                ElementRuleId = row.GetString("ElementRuleId"),
                AfkRewardMultiplier = row.GetFloat("AfkRewardMultiplier", 1f)
            };

            chapters.Add(chapter);
        }

        asset.Chapters = chapters.ToArray();

        Debug.Log($"[StageConfigImporter] Imported Chapters: {asset.Chapters.Length}");
        return asset;
    }

    // =========================================================
    // Import Enemy Pattern Rules + Enemy Patterns
    // =========================================================

    private EnemyPatternRuleSO ImportEnemyPatternConfig(string rulesCsvText, string patternsCsvText)
    {
        CsvTable rulesTable = CsvTable.Parse(rulesCsvText);
        CsvTable patternsTable = CsvTable.Parse(patternsCsvText);
        EnemyPatternRuleSO asset = LoadOrCreateAsset<EnemyPatternRuleSO>("EnemyPatternRule.asset");

        List<EnemyPatternRule> rules = new List<EnemyPatternRule>();
        List<EnemyPatternData> patterns = new List<EnemyPatternData>();

        foreach (CsvRow row in rulesTable.Rows)
        {
            if (row.IsEmpty)
                continue;

            EnemyPatternRule rule = new EnemyPatternRule
            {
                RuleId = row.GetStringAny("EnemyPatternRuleId", "RuleId"),
                RequiredElement = row.GetEnum<Element>("RequiredElement"),
                PickMode = ParsePickMode(row.GetString("PickMode", "Loop")),
                PatternLoopIds = SplitPipe(row.GetString("PatternLoopCsv"))
            };

            rules.Add(rule);
        }

        foreach (CsvRow row in patternsTable.Rows)
        {
            if (row.IsEmpty)
                continue;

            EnemyPatternData pattern = new EnemyPatternData
            {
                PatternId = row.GetString("PatternId"),
                RequiredElement = row.GetEnum<Element>("RequiredElement")
            };

            ReadEnemyIdsAndRates(row, out int[] enemyIds, out float[] rates);
            pattern.EnemyIds = enemyIds;
            pattern.Rates = rates;

            patterns.Add(pattern);
        }

        asset.Rules = rules.ToArray();
        asset.Patterns = patterns.ToArray();

        ValidateEnemyPatternReferences(asset);

        Debug.Log(
            $"[StageConfigImporter] Imported EnemyPatternRules: {asset.Rules.Length}, Patterns: {asset.Patterns.Length}");
        return asset;
    }

    private PatternPickMode ParsePickMode(string value)
    {
        if (Enum.TryParse(value, true, out PatternPickMode result))
            return result;

        return PatternPickMode.Loop;
    }

    private void ReadEnemyIdsAndRates(CsvRow row, out int[] enemyIds, out float[] rates)
    {
        // Support dạng mới:
        // EnemyIdsCsv = 1001|1002|1003
        // RatesCsv = 50|30|20
        if (row.Has("EnemyIdsCsv"))
        {
            enemyIds = SplitPipeInts(row.GetString("EnemyIdsCsv"));
            rates = row.Has("RatesCsv")
                ? SplitPipeFloats(row.GetString("RatesCsv"))
                : CreateEqualRates(enemyIds.Length);

            if (rates.Length != enemyIds.Length)
            {
                Debug.LogError(
                    $"[StageConfigImporter] EnemyIdsCsv/RatesCsv length mismatch. PatternId={row.GetString("PatternId")}");
                rates = CreateEqualRates(enemyIds.Length);
            }

            return;
        }

        // Support dạng cột:
        // EnemyId1 Rate1 EnemyId2 Rate2 ...
        List<int> ids = new List<int>();
        List<float> rateList = new List<float>();

        for (int i = 1; i <= 8; i++)
        {
            string enemyColumn = $"EnemyId{i}";
            string rateColumn = $"Rate{i}";

            if (!row.Has(enemyColumn))
                continue;

            string enemyValue = row.GetString(enemyColumn);
            if (string.IsNullOrWhiteSpace(enemyValue))
                continue;

            if (!int.TryParse(enemyValue, out int enemyId))
            {
                Debug.LogError(
                    $"[StageConfigImporter] Invalid enemy id. PatternId={row.GetString("PatternId")}, Column={enemyColumn}, Value={enemyValue}");
                continue;
            }

            float rate = row.Has(rateColumn)
                ? row.GetFloat(rateColumn, 0f)
                : 0f;

            ids.Add(enemyId);
            rateList.Add(rate);
        }

        enemyIds = ids.ToArray();
        rates = rateList.Count == enemyIds.Length
            ? rateList.ToArray()
            : CreateEqualRates(enemyIds.Length);

        if (enemyIds.Length == 0)
            Debug.LogError($"[StageConfigImporter] Pattern has no enemy ids. PatternId={row.GetString("PatternId")}");
    }

    private void ValidateEnemyPatternReferences(EnemyPatternRuleSO asset)
    {
        if (asset == null || asset.Rules == null || asset.Patterns == null)
            return;

        HashSet<string> patternIds = new HashSet<string>(asset.Patterns.Select(x => x.PatternId));

        foreach (EnemyPatternRule rule in asset.Rules)
        {
            if (rule.PatternLoopIds == null)
                continue;

            foreach (string patternId in rule.PatternLoopIds)
            {
                if (!patternIds.Contains(patternId))
                {
                    Debug.LogError(
                        $"[StageConfigImporter] EnemyPatternRule references missing PatternId. Rule={rule.RuleId}, MissingPattern={patternId}");
                }
            }
        }
    }

    // =========================================================
    // Import Boss Pattern Rules
    // =========================================================

    private BossPatternRuleSO ImportBossPatternRules(string csvText)
    {
        CsvTable table = CsvTable.Parse(csvText);

        BossPatternRuleSO asset = LoadOrCreateAsset<BossPatternRuleSO>("BossPatternRule.asset");

        List<BossPatternRule> rules = new List<BossPatternRule>();

        foreach (CsvRow row in table.Rows)
        {
            if (row.IsEmpty)
                continue;

            BossPatternRule rule = new BossPatternRule
            {
                RuleId = row.GetStringAny("BossPatternRuleId", "RuleId"),
                RequiredElement = row.GetEnum<Element>("RequiredElement"),
                StagesPerBoss = Mathf.Max(1, row.GetInt("StagesPerBoss", 1)),
                BossLoopIds = SplitPipeInts(row.GetString("BossLoopCsv"))
            };

            if (rule.BossLoopIds == null || rule.BossLoopIds.Length == 0)
            {
                Debug.LogError($"[StageConfigImporter] Boss rule has no BossLoopCsv. Rule={rule.RuleId}");
            }

            rules.Add(rule);
        }

        asset.Rules = rules.ToArray();

        Debug.Log($"[StageConfigImporter] Imported BossPatternRules: {asset.Rules.Length}");
        return asset;
    }

    // =========================================================
    // Import Reward Rules
    // =========================================================

    private RewardRuleSO ImportRewardRules(string csv)
    {
        CsvTable table = CsvTable.Parse(csv);

        RewardRuleSO asset = LoadOrCreateAsset<RewardRuleSO>("RewardRule.asset");

        List<RewardRule> rules = new List<RewardRule>();

        foreach (CsvRow row in table.Rows)
        {
            if (row.IsEmpty)
                continue;

            RewardRule rule = new RewardRule
            {
                RewardRuleId = row.GetString("RewardRuleId"),
                BaseRewards = ReadRewardEntries(row, "BaseReward"),
                ClearRewards = ReadRewardEntries(row, "ClearReward"),
                Note = row.GetString("Note", string.Empty)
            };

            rules.Add(rule);
        }

        asset.Rules = rules.ToArray();

        Debug.Log($"[StageConfigImporter] Imported RewardRules: {asset.Rules.Length}");
        return asset;
    }

    private RewardFormulaEntry[] ReadRewardEntries(CsvRow row, string prefix)
    {
        List<RewardFormulaEntry> entries = new List<RewardFormulaEntry>();

        for (int i = 1; i <= 8; i++)
        {
            string typeColumn = $"{prefix}{i}Type";
            string formulaColumn = $"{prefix}{i}Formula";

            if (!row.Has(typeColumn))
                continue;

            string type = row.GetString(typeColumn);
            if (string.IsNullOrWhiteSpace(type))
                continue;

            string formula = row.Has(formulaColumn)
                ? row.GetString(formulaColumn)
                : string.Empty;

            entries.Add(new RewardFormulaEntry
            {
                ResourceType = type,
                Formula = formula
            });
        }

        return entries.ToArray();
    }

    // =========================================================
    // Import Stage Scaling Rules
    // =========================================================

    private StageScalingRuleSO ImportStageScalingRules(string csv)
    {
        CsvTable table = CsvTable.Parse(csv);

        StageScalingRuleSO asset = LoadOrCreateAsset<StageScalingRuleSO>("StageScalingRule.asset");

        List<StageScalingRule> rules = new List<StageScalingRule>();

        foreach (CsvRow row in table.Rows)
        {
            if (row.IsEmpty)
                continue;

            StageScalingRule rule = new StageScalingRule
            {
                ScalingRuleId = row.GetString("ScalingRuleId"),
                EnemyHpMultiplierFormula = row.GetString("EnemyHpMultiplierFormula"),
                EnemyAtkMultiplierFormula = row.GetString("EnemyAtkMultiplierFormula"),
                EnemyDefMultiplierFormula = row.GetString("EnemyDefMultiplierFormula"),
                BossHpMultiplierFormula = row.GetString("BossHpMultiplierFormula"),
                BossAtkMultiplierFormula = row.GetString("BossAtkMultiplierFormula"),
                BossDefMultiplierFormula = row.GetString("BossDefMultiplierFormula"),
                Note = row.GetString("Note", string.Empty)
            };

            rules.Add(rule);
        }

        asset.Rules = rules.ToArray();

        Debug.Log($"[StageConfigImporter] Imported StageScalingRules: {asset.Rules.Length}");
        return asset;
    }

    // =========================================================
    // Asset Helpers
    // =========================================================

    private T LoadOrCreateAsset<T>(string fileName) where T : ScriptableObject
    {
        EnsureFolder(outputFolder);

        string path = $"{outputFolder}/{fileName}";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (asset != null)
            return asset;

        asset = CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string folderPath)
    {
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

    // =========================================================
    // Pipe Helpers
    // =========================================================

    private static string[] SplitPipe(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        return value
            .Split('|')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    private static int[] SplitPipeInts(string value)
    {
        string[] parts = SplitPipe(value);
        List<int> result = new List<int>();

        foreach (string part in parts)
        {
            if (int.TryParse(part, out int id))
            {
                result.Add(id);
            }
            else
            {
                Debug.LogError($"[StageConfigImporter] Invalid int in pipe list: {part}");
            }
        }

        return result.ToArray();
    }

    private static float[] SplitPipeFloats(string value)
    {
        string[] parts = SplitPipe(value);
        List<float> result = new List<float>();

        foreach (string part in parts)
        {
            if (float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
            {
                result.Add(number);
            }
            else
            {
                Debug.LogError($"[StageConfigImporter] Invalid float in pipe list: {part}");
            }
        }

        return result.ToArray();
    }

    private static float[] CreateEqualRates(int count)
    {
        if (count <= 0)
            return Array.Empty<float>();

        float value = 1f / count;
        float[] rates = new float[count];

        for (int i = 0; i < count; i++)
            rates[i] = value;

        return rates;
    }

    // =========================================================
    // CSV Parser
    // =========================================================

    private class CsvTable
    {
        public readonly List<CsvRow> Rows = new List<CsvRow>();

        public static CsvTable Parse(string csv)
        {
            CsvTable table = new CsvTable();

            List<List<string>> rawRows = ParseRaw(csv);
            if (rawRows.Count == 0)
                return table;

            List<string> headers = rawRows[0]
                .Select(NormalizeHeader)
                .ToList();

            for (int i = 1; i < rawRows.Count; i++)
            {
                List<string> rawRow = rawRows[i];

                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int h = 0; h < headers.Count; h++)
                {
                    string header = headers[h];
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    string value = h < rawRow.Count ? rawRow[h] : string.Empty;
                    values[header] = value.Trim();
                }

                table.Rows.Add(new CsvRow(values));
            }

            return table;
        }

        private static string NormalizeHeader(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace("\uFEFF", string.Empty);
        }

        private static List<List<string>> ParseRaw(string csv)
        {
            List<List<string>> rows = new List<List<string>>();
            List<string> currentRow = new List<string>();
            System.Text.StringBuilder currentCell = new System.Text.StringBuilder();

            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        currentCell.Append('"');
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
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                    continue;
                }

                if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                        i++;

                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();

                    bool hasAnyValue = currentRow.Any(x => !string.IsNullOrWhiteSpace(x));
                    if (hasAnyValue)
                        rows.Add(currentRow);

                    currentRow = new List<string>();
                    continue;
                }

                currentCell.Append(c);
            }

            currentRow.Add(currentCell.ToString());

            if (currentRow.Any(x => !string.IsNullOrWhiteSpace(x)))
                rows.Add(currentRow);

            return rows;
        }
    }

    private class CsvRow
    {
        private readonly Dictionary<string, string> values;

        public bool IsEmpty => values.Values.All(string.IsNullOrWhiteSpace);

        public CsvRow(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public bool Has(string key)
        {
            return values.ContainsKey(key);
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (!values.TryGetValue(key, out string value))
                return defaultValue;

            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        public string GetStringAny(params string[] keys)
        {
            foreach (string key in keys)
            {
                string value = GetString(key, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            string value = GetString(key, string.Empty);

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;

            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            string value = GetString(key, string.Empty);

            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;

            return defaultValue;
        }

        public T GetEnum<T>(string key) where T : struct, Enum
        {
            string value = GetString(key, string.Empty);

            if (Enum.TryParse(value, true, out T result))
                return result;

            Debug.LogError($"[StageConfigImporter] Cannot parse enum {typeof(T).Name}. Column={key}, Value={value}");
            return default;
        }
    }
}
#endif