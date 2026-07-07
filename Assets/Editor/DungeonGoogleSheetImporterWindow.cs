#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Battle.Dungeon.Editor
{
    public sealed class DungeonGoogleSheetImporterWindow : EditorWindow
    {
        // Điền URL CSV thật trực tiếp tại đây.
        private const string DungeonDefinitionCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=257803413&single=true&output=csv";

        private const string DungeonStageFormulaCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1851007320&single=true&output=csv";

        private const string DungeonDamageThresholdCsvUrl =
            "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1499794538&single=true&output=csv";

        private static readonly HttpClient HttpClient = new();

        [SerializeField] private DungeonDatabaseSO targetDatabase;
        private bool isImporting;

        [MenuItem("Tools/Game Data/Dungeun Data Importer")]
        private static void OpenWindow()
        {
            DungeonGoogleSheetImporterWindow window =
                GetWindow<DungeonGoogleSheetImporterWindow>();

            window.titleContent = new GUIContent("Dungeon Importer");
            window.minSize = new Vector2(460f, 210f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Dungeon Google Sheet Importer",
                EditorStyles.boldLabel
            );

            EditorGUILayout.HelpBox(
                "Import và override Definition, Stage Formula và Damage Threshold " +
                "trong DungeonDatabaseSO.",
                MessageType.Info
            );

            targetDatabase =
                (DungeonDatabaseSO)EditorGUILayout.ObjectField(
                    "Target Database",
                    targetDatabase,
                    typeof(DungeonDatabaseSO),
                    false
                );

            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(
                       isImporting || targetDatabase == null))
            {
                if (GUILayout.Button(
                        "Import All Dungeon Data",
                        GUILayout.Height(38f)))
                {
                    ImportAllAsync();
                }
            }

            if (isImporting)
            {
                EditorGUILayout.HelpBox(
                    "Đang tải và import dữ liệu...",
                    MessageType.Info
                );
            }
        }

        private async void ImportAllAsync()
        {
            if (targetDatabase == null)
            {
                return;
            }

            isImporting = true;
            Repaint();

            try
            {
                string definitionCsv =
                    await DownloadCsvAsync(DungeonDefinitionCsvUrl);

                string stageFormulaCsv =
                    await DownloadCsvAsync(DungeonStageFormulaCsvUrl);

                string damageThresholdCsv =
                    await DownloadCsvAsync(DungeonDamageThresholdCsvUrl);

                List<DungeonDefinitionData> definitions =
                    ParseDefinitions(definitionCsv);

                List<DungeonStageFormulaRow> stageFormulaRows =
                    ParseStageFormulaRows(stageFormulaCsv);

                List<DungeonDamageThresholdRow> damageThresholdRows =
                    ParseDamageThresholdRows(damageThresholdCsv);

                ValidateImportedData(
                    definitions,
                    stageFormulaRows,
                    damageThresholdRows
                );

                Undo.RecordObject(
                    targetDatabase,
                    "Import Dungeon Google Sheet Data"
                );

                targetDatabase.EditorReplaceData(
                    definitions,
                    stageFormulaRows,
                    damageThresholdRows
                );

                EditorUtility.SetDirty(targetDatabase);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Dungeon Importer",
                    "Import thành công.\n\n" +
                    $"Definitions: {definitions.Count}\n" +
                    $"Stage formulas: {stageFormulaRows.Count}\n" +
                    $"Damage thresholds: {damageThresholdRows.Count}",
                    "OK"
                );
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "Dungeon Importer Error",
                    exception.Message,
                    "OK"
                );
            }
            finally
            {
                isImporting = false;
                Repaint();
            }
        }

        private static async Task<string> DownloadCsvAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url) ||
                url.Contains("YOUR_SHEET_ID"))
            {
                throw new InvalidOperationException(
                    $"Google Sheet URL chưa được cấu hình:\n{url}"
                );
            }

            using HttpResponseMessage response =
                await HttpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private static List<DungeonDefinitionData> ParseDefinitions(
            string csv)
        {
            List<Dictionary<string, string>> rows = CsvUtility.Parse(csv);
            List<DungeonDefinitionData> result = new(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                DungeonDefinitionData data = new();

                SetPrivateField(data, "dungeonId", ReadInt(row, "dungeon_id"));
                SetPrivateField(data, "dungeonKey", ReadString(row, "dungeon_key"));
                SetPrivateField(data, "uiNameVi", ReadString(row, "ui_name_vi"));
                SetPrivateField(data, "uiNameEn", ReadString(row, "ui_name_en"));
                SetPrivateField(data, "mode", ParseMode(ReadString(row, "mode")));
                SetPrivateField(data, "stageCount", ReadInt(row, "stage_count"));
                SetPrivateField(data, "entryCostKey", ReadString(row, "entry_cost_key"));
                SetPrivateField(data, "entryCostAmount", ReadInt(row, "entry_cost_amount", 1));
                SetPrivateField(data, "stageTableKey", ReadString(row, "stage_table_key"));
                SetPrivateField(data, "defaultTimeLimitSec", ReadInt(row, "default_time_limit_sec", 60));
                SetPrivateField(data, "enemyId", ReadInt(row, "enemy_id"));
                SetPrivateField(data, "bossId", ReadInt(row, "boss_id"));
                SetPrivateField(data, "mapName", ReadString(row, "map_name"));

                result.Add(data);
            }

            return result;
        }

        private static List<DungeonStageFormulaRow> ParseStageFormulaRows(
            string csv)
        {
            List<Dictionary<string, string>> rows = CsvUtility.Parse(csv);
            List<DungeonStageFormulaRow> result = new(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                DungeonStageFormulaRow data = new();

                SetPrivateField(data, "tableKey", ReadString(row, "table_key"));
                SetPrivateField(data, "stage", ReadInt(row, "stage", 1));
                SetPrivateField(data, "timeLimitOverrideSec", ReadInt(row, "time_limit_override_sec"));
                SetPrivateField(data, "recommendedPower", ParseFormula(row, "recommended_power"));
                SetPrivateField(data, "enemyHp", ParseFormula(row, "enemy_hp"));
                SetPrivateField(data, "enemyAtk", ParseFormula(row, "enemy_atk"));
                SetPrivateField(data, "enemyDef", ParseFormula(row, "enemy_def"));
                SetPrivateField(data, "reward1ItemId", ReadString(row, "reward1_item_id"));
                SetPrivateField(data, "reward1", ParseFormula(row, "reward1"));
                SetPrivateField(data, "reward2ItemId", ReadString(row, "reward2_item_id"));
                SetPrivateField(data, "reward2", ParseFormula(row, "reward2"));
                SetPrivateField(data, "reward3ItemId", ReadString(row, "reward3_item_id"));
                SetPrivateField(data, "reward3", ParseFormula(row, "reward3"));
                SetPrivateField(data, "enemyCount", ParseFormula(row, "enemy_count"));
                SetPrivateField(data, "enemyPerBatch", Mathf.Max(1, ReadInt(row, "enemy_per_batch", 1)));
                SetPrivateField(data, "delayBetweenBatchesSec", Mathf.Max(0f, ReadFloat(row, "delay_between_batches_sec")));

                result.Add(data);
            }

            return result;
        }

        private static List<DungeonDamageThresholdRow>
            ParseDamageThresholdRows(string csv)
        {
            List<Dictionary<string, string>> rows = CsvUtility.Parse(csv);
            List<DungeonDamageThresholdRow> result = new(rows.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                DungeonDamageThresholdRow data = new();

                SetPrivateField(data, "tableKey", ReadString(row, "table_key"));
                SetPrivateField(data, "stage", ReadInt(row, "stage", 1));
                SetPrivateField(data, "requiredDamage", ParseFormula(row, "required_damage"));
                SetPrivateField(
                    data,
                    "rewardMultiplierPercent",
                    Mathf.Max(
                        0f,
                        ReadFloat(row, "reward_multiplier_percent", 100f)
                    )
                );

                result.Add(data);
            }

            return result;
        }

        private static void ValidateImportedData(
            List<DungeonDefinitionData> definitions,
            List<DungeonStageFormulaRow> formulaRows,
            List<DungeonDamageThresholdRow> thresholdRows)
        {
            HashSet<int> dungeonIds = new();

            for (int i = 0; i < definitions.Count; i++)
            {
                DungeonDefinitionData definition = definitions[i];

                if (definition.DungeonId <= 0)
                {
                    throw new InvalidOperationException(
                        $"Dungeon definition dòng {i + 2}: dungeon_id phải lớn hơn 0."
                    );
                }

                if (!dungeonIds.Add(definition.DungeonId))
                {
                    throw new InvalidOperationException(
                        $"Dungeon definition dòng {i + 2}: trùng dungeon_id={definition.DungeonId}."
                    );
                }

                if (string.IsNullOrWhiteSpace(definition.MapName))
                {
                    throw new InvalidOperationException(
                        $"Dungeon '{definition.DungeonKey}' yêu cầu map_name."
                    );
                }

                if (!HasStageOneFormula(formulaRows, definition.StageTableKey))
                {
                    throw new InvalidOperationException(
                        $"Dungeon '{definition.DungeonKey}' không có " +
                        $"Stage Formula tại stage 1."
                    );
                }

                if (definition.Mode == DungeonModeType.DamageChallenge &&
                    !HasStageOneThreshold(thresholdRows, definition.StageTableKey))
                {
                    throw new InvalidOperationException(
                        $"Damage Challenge '{definition.DungeonKey}' không có " +
                        $"required damage formula tại stage 1."
                    );
                }

                bool requiresEnemy =
                    definition.Mode == DungeonModeType.KillAllEnemies ||
                    definition.Mode == DungeonModeType.DefendObjective;

                if (requiresEnemy && definition.EnemyId <= 0)
                {
                    throw new InvalidOperationException(
                        $"Dungeon '{definition.DungeonKey}' yêu cầu enemy_id > 0."
                    );
                }

                if (definition.Mode == DungeonModeType.BossChallenge &&
                    definition.EnemyId <= 0)
                {
                    throw new InvalidOperationException(
                        $"Boss Challenge '{definition.DungeonKey}' yêu cầu boss_id > 0."
                    );
                }
            }
        }

        private static bool HasStageOneFormula(
            List<DungeonStageFormulaRow> rows,
            string tableKey)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                DungeonStageFormulaRow row = rows[i];
                if (row != null &&
                    row.Stage == 1 &&
                    string.Equals(
                        row.TableKey,
                        tableKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasStageOneThreshold(
            List<DungeonDamageThresholdRow> rows,
            string tableKey)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                DungeonDamageThresholdRow row = rows[i];
                if (row != null &&
                    row.Stage == 1 &&
                    string.Equals(
                        row.TableKey,
                        tableKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }


        private static DungeonFormulaData ParseFormula(
            Dictionary<string, string> row,
            string prefix)
        {
            DungeonFormulaData formulaData = new DungeonFormulaData();

            // DungeonFormulaData là struct: box một lần, set trên cùng boxed instance,
            // sau đó unbox để không bị mất toàn bộ giá trị.
            object boxedData = formulaData;

            SetPrivateField(
                boxedData,
                "formula",
                ParseFormulaType(ReadString(row, $"{prefix}_formula"))
            );

            SetPrivateField(
                boxedData,
                "baseValue",
                ReadDouble(row, $"{prefix}_base")
            );

            SetPrivateField(
                boxedData,
                "coefficient",
                ReadDouble(row, $"{prefix}_coefficient")
            );

            SetPrivateField(
                boxedData,
                "exponent",
                ReadDouble(row, $"{prefix}_exponent")
            );

            SetPrivateField(
                boxedData,
                "stepInterval",
                ReadInt(row, $"{prefix}_step_interval")
            );

            SetPrivateField(
                boxedData,
                "stepValue",
                ReadDouble(row, $"{prefix}_step_value")
            );

            SetPrivateField(
                boxedData,
                "roundMode",
                ParseRoundMode(ReadString(row, $"{prefix}_round"))
            );

            return (DungeonFormulaData)boxedData;
        }

        private static DungeonModeType ParseMode(string value)
        {
            switch (Normalize(value))
            {
                case "CLEAR":
                case "KILL_ALL_ENEMIES":
                    return DungeonModeType.KillAllEnemies;

                case "DEFENSE":
                case "DEFEND_OBJECTIVE":
                    return DungeonModeType.DefendObjective;

                case "DPS":
                case "DAMAGE_CHALLENGE":
                    return DungeonModeType.DamageChallenge;

                case "BOSS":
                case "BOSS_CHALLENGE":
                    return DungeonModeType.BossChallenge;

                default:
                    throw new InvalidOperationException(
                        $"Dungeon mode không hợp lệ: '{value}'"
                    );
            }
        }

        private static DungeonFormulaType ParseFormulaType(string value)
        {
            switch (Normalize(value))
            {
                case "":
                case "FLAT":
                    return DungeonFormulaType.Flat;
                case "LINEAR":
                    return DungeonFormulaType.Linear;
                case "POWER":
                    return DungeonFormulaType.Power;
                case "EXPONENTIAL":
                    return DungeonFormulaType.Exponential;
                case "STEP":
                    return DungeonFormulaType.Step;
                default:
                    throw new InvalidOperationException(
                        $"Formula không hợp lệ: '{value}'"
                    );
            }
        }

        private static DungeonRoundMode ParseRoundMode(string value)
        {
            switch (Normalize(value))
            {
                case "":
                case "NONE":
                    return DungeonRoundMode.None;
                case "ROUND":
                    return DungeonRoundMode.Round;
                case "FLOOR":
                    return DungeonRoundMode.Floor;
                case "CEIL":
                case "CEILING":
                    return DungeonRoundMode.Ceil;
                default:
                    throw new InvalidOperationException(
                        $"Round mode không hợp lệ: '{value}'"
                    );
            }
        }

        private static string Normalize(string value)
        {
            return value?
                       .Trim()
                       .Replace(" ", "_")
                       .Replace("-", "_")
                       .ToUpperInvariant()
                   ?? string.Empty;
        }

        private static string ReadString(
            Dictionary<string, string> row,
            string key,
            string defaultValue = "")
        {
            return row.TryGetValue(key, out string value)
                ? value.Trim()
                : defaultValue;
        }

        private static int ReadInt(
            Dictionary<string, string> row,
            string key,
            int defaultValue = 0)
        {
            string value = ReadString(row, key);

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int result))
            {
                return result;
            }

            throw new FormatException(
                $"Không thể parse int: column='{key}', value='{value}'"
            );
        }

        private static float ReadFloat(
            Dictionary<string, string> row,
            string key,
            float defaultValue = 0f)
        {
            string value = ReadString(row, key);

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (float.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float result))
            {
                return result;
            }

            string normalized = value.Replace(',', '.');
            if (float.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw new FormatException(
                $"Không thể parse float: column='{key}', value='{value}'"
            );
        }

        private static double ReadDouble(
            Dictionary<string, string> row,
            string key,
            double defaultValue = 0d)
        {
            string value = ReadString(row, key);

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (double.TryParse(
                    value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double result))
            {
                return result;
            }

            string normalized = value.Replace(',', '.');
            if (double.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw new FormatException(
                $"Không thể parse double: column='{key}', value='{value}'"
            );
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (field == null)
            {
                throw new MissingFieldException(
                    target.GetType().FullName,
                    fieldName
                );
            }

            field.SetValue(target, value);
        }

        private static class CsvUtility
        {
            public static List<Dictionary<string, string>> Parse(
                string csv)
            {
                List<List<string>> rawRows = ParseRows(csv);

                if (rawRows.Count == 0)
                {
                    return new List<Dictionary<string, string>>();
                }

                List<string> headers = rawRows[0];
                List<Dictionary<string, string>> result =
                    new(rawRows.Count - 1);

                for (int rowIndex = 1;
                     rowIndex < rawRows.Count;
                     rowIndex++)
                {
                    List<string> rawRow = rawRows[rowIndex];

                    if (IsEmptyRow(rawRow))
                    {
                        continue;
                    }

                    Dictionary<string, string> row =
                        new(StringComparer.OrdinalIgnoreCase);

                    for (int columnIndex = 0;
                         columnIndex < headers.Count;
                         columnIndex++)
                    {
                        string header = headers[columnIndex]
                            .Trim()
                            .TrimStart('\uFEFF');

                        string value = columnIndex < rawRow.Count
                            ? rawRow[columnIndex]
                            : string.Empty;

                        row[header] = value;
                    }

                    result.Add(row);
                }

                return result;
            }

            private static List<List<string>> ParseRows(string csv)
            {
                List<List<string>> rows = new();
                List<string> currentRow = new();
                StringBuilder field = new();
                bool insideQuotes = false;

                for (int i = 0; i < csv.Length; i++)
                {
                    char character = csv[i];

                    if (character == '"')
                    {
                        if (insideQuotes &&
                            i + 1 < csv.Length &&
                            csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            insideQuotes = !insideQuotes;
                        }

                        continue;
                    }

                    if (!insideQuotes && character == ',')
                    {
                        currentRow.Add(field.ToString());
                        field.Clear();
                        continue;
                    }

                    if (!insideQuotes &&
                        (character == '\n' || character == '\r'))
                    {
                        if (character == '\r' &&
                            i + 1 < csv.Length &&
                            csv[i + 1] == '\n')
                        {
                            i++;
                        }

                        currentRow.Add(field.ToString());
                        field.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                        continue;
                    }

                    field.Append(character);
                }

                if (field.Length > 0 || currentRow.Count > 0)
                {
                    currentRow.Add(field.ToString());
                    rows.Add(currentRow);
                }

                return rows;
            }

            private static bool IsEmptyRow(List<string> row)
            {
                for (int i = 0; i < row.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(row[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}

#endif
