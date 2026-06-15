#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Battle;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Hero;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class HeroDataCsvImporterWindow : EditorWindow
{
    /*
     * Link Google Sheet dạng CSV:
     *
     * https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/export?format=csv&gid=SHEET_GID
     */
    private const string GoogleSheetCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=368363386&single=true&output=csv";

    /*
     * Folder lưu HeroDataSO.
     * Tool tự tạo folder nếu chưa tồn tại.
     */
    private const string OutputFolder =
        "Assets/Immortal Switch/Addressable/Hero/Data";

    private const string AssetNamePrefix = "HeroData_";

    private bool isImporting;
    private Vector2 logScrollPosition;

    private readonly List<string> importLogs = new();

    [MenuItem("Tools/Game Data/Hero Data Importer")]
    private static void OpenWindow()
    {
        HeroDataCsvImporterWindow window =
            GetWindow<HeroDataCsvImporterWindow>();

        window.titleContent = new GUIContent("Hero Data Importer");
        window.minSize = new Vector2(680f, 470f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10f);

        EditorGUILayout.LabelField(
            "Hero Data Google Sheet Importer",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(5f);

        DrawConfiguration();

        EditorGUILayout.Space(10f);

        using (new EditorGUI.DisabledScope(isImporting))
        {
            string buttonText = isImporting
                ? "Importing..."
                : "Download And Import Hero Data";

            if (GUILayout.Button(buttonText, GUILayout.Height(38f)))
            {
                DownloadAndImport();
            }
        }

        EditorGUILayout.Space(10f);

        DrawLogs();
    }

    private void DrawConfiguration()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField(
            "Google Sheet CSV URL",
            EditorStyles.boldLabel);

        EditorGUILayout.SelectableLabel(
            GoogleSheetCsvUrl,
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));

        EditorGUILayout.Space(5f);

        EditorGUILayout.LabelField(
            "Output Folder",
            EditorStyles.boldLabel);

        EditorGUILayout.SelectableLabel(
            OutputFolder,
            EditorStyles.textField,
            GUILayout.Height(EditorGUIUtility.singleLineHeight));

        EditorGUILayout.Space(5f);

        EditorGUILayout.HelpBox(
            "Tool sử dụng hero_Id làm định danh. " +
            "Asset đã tồn tại sẽ được cập nhật. " +
            "PortraitIcon, ShardIcon và IsAvailableInSummon sẽ được giữ nguyên.",
            MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void DrawLogs()
    {
        EditorGUILayout.LabelField(
            "Import Logs",
            EditorStyles.boldLabel);

        logScrollPosition = EditorGUILayout.BeginScrollView(
            logScrollPosition,
            EditorStyles.helpBox);

        if (importLogs.Count == 0)
        {
            EditorGUILayout.LabelField("Chưa import dữ liệu.");
        }
        else
        {
            for (int i = 0; i < importLogs.Count; i++)
            {
                EditorGUILayout.LabelField(
                    importLogs[i],
                    EditorStyles.wordWrappedLabel);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private async void DownloadAndImport()
    {
        if (isImporting)
            return;

        if (!ValidateSettings())
            return;

        isImporting = true;
        importLogs.Clear();

        AddLog("Đang tải Hero CSV từ Google Sheet...");

        try
        {
            string csvContent =
                await DownloadCsvAsync(GoogleSheetCsvUrl);

            if (string.IsNullOrWhiteSpace(csvContent))
            {
                throw new InvalidOperationException(
                    "Google Sheet trả về dữ liệu rỗng.");
            }

            AddLog(
                $"Tải CSV thành công: {csvContent.Length} ký tự.");

            ImportCsv(csvContent);
        }
        catch (Exception exception)
        {
            AddLog($"Import failed: {exception.Message}");
            Debug.LogException(exception);
        }
        finally
        {
            isImporting = false;
            Repaint();
        }
    }

    private bool ValidateSettings()
    {
        if (GoogleSheetCsvUrl.Contains("YOUR_SPREADSHEET_ID"))
        {
            EditorUtility.DisplayDialog(
                "Hero Data Importer",
                "Bạn chưa cập nhật GoogleSheetCsvUrl.",
                "OK");

            return false;
        }

        if (!GoogleSheetCsvUrl.StartsWith(
                "https://docs.google.com/spreadsheets/",
                StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Hero Data Importer",
                "GoogleSheetCsvUrl không hợp lệ.",
                "OK");

            return false;
        }

        if (!OutputFolder.StartsWith(
                "Assets/",
                StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog(
                "Hero Data Importer",
                "OutputFolder phải bắt đầu bằng Assets/.",
                "OK");

            return false;
        }

        return true;
    }

    private static async Task<string> DownloadCsvAsync(string url)
    {
        using UnityWebRequest request = UnityWebRequest.Get(url);

        request.timeout = 30;

        UnityWebRequestAsyncOperation operation =
            request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Delay(50);
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            throw new InvalidOperationException(
                $"Không thể tải Google Sheet.\n" +
                $"HTTP Code: {request.responseCode}\n" +
                $"Error: {request.error}");
        }

        return request.downloadHandler.text;
    }

    private void ImportCsv(string csvContent)
    {
        List<List<string>> rows =
            CsvParser.Parse(csvContent);

        if (rows.Count < 2)
        {
            throw new InvalidOperationException(
                "CSV không có dữ liệu hoặc chỉ có header.");
        }

        Dictionary<string, int> headers =
            BuildHeaderDictionary(rows[0]);

        ValidateRequiredHeaders(headers);
        EnsureFolderExists(OutputFolder);

        int createdCount = 0;
        int updatedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        HashSet<int> importedHeroIds = new();

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                List<string> row = rows[rowIndex];

                if (IsEmptyRow(row))
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    HeroCsvRow heroCsvRow = ParseRow(
                        row,
                        rowIndex + 1,
                        headers);

                    if (!importedHeroIds.Add(heroCsvRow.Id))
                    {
                        throw new InvalidOperationException(
                            $"hero_Id {heroCsvRow.Id} bị trùng trong CSV.");
                    }

                    bool wasCreated =
                        CreateOrUpdateAsset(heroCsvRow);

                    if (wasCreated)
                    {
                        createdCount++;

                        AddLog(
                            $"Created: {heroCsvRow.Id} - " +
                            heroCsvRow.Name);
                    }
                    else
                    {
                        updatedCount++;

                        AddLog(
                            $"Updated: {heroCsvRow.Id} - " +
                            heroCsvRow.Name);
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;

                    AddLog(
                        $"Row {rowIndex + 1} failed: " +
                        exception.Message);

                    Debug.LogError(
                        $"[Hero Data Importer] " +
                        $"Row {rowIndex + 1} failed:\n{exception}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        AddLog("--------------------------------");
        AddLog(
            $"Completed. Created: {createdCount}, " +
            $"Updated: {updatedCount}, " +
            $"Skipped: {skippedCount}, " +
            $"Failed: {failedCount}");

        EditorUtility.DisplayDialog(
            "Hero Data Importer",
            $"Import hoàn tất.\n\n" +
            $"Created: {createdCount}\n" +
            $"Updated: {updatedCount}\n" +
            $"Skipped: {skippedCount}\n" +
            $"Failed: {failedCount}",
            "OK");
    }

    private static HeroCsvRow ParseRow(
        IReadOnlyList<string> row,
        int csvRowNumber,
        IReadOnlyDictionary<string, int> headers)
    {
        string idText =
            GetCell(row, headers, "hero_Id");

        string heroName =
            GetCell(row, headers, "hero_Name");

        string heroAddressKey =
            GetCell(row, headers, "key");

        string spineAddressKey =
            GetCell(row, headers, "key_spine");

        string heroIconKey =
            GetCell(row, headers, "key_icon");

        string heroClassText =
            GetCell(row, headers, "class");

        string rarityText =
            GetCell(row, headers, "rarity");

        string elementText =
            GetCell(row, headers, "element");

        if (!int.TryParse(
                idText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int heroId))
        {
            throw new FormatException(
                $"hero_Id '{idText}' không hợp lệ.");
        }

        if (heroId <= 0)
        {
            throw new FormatException(
                $"hero_Id phải lớn hơn 0. Giá trị: {heroId}");
        }

        ValidateRequiredString(
            heroName,
            "hero_Name",
            csvRowNumber);

        ValidateRequiredString(
            heroAddressKey,
            "key",
            csvRowNumber);

        ValidateRequiredString(
            spineAddressKey,
            "key_spine",
            csvRowNumber);

        ValidateRequiredString(
            heroIconKey,
            "key_icon",
            csvRowNumber);

        if (!Enum.TryParse(
                heroClassText,
                true,
                out HeroClass heroClass))
        {
            throw new FormatException(
                $"HeroClass '{heroClassText}' không tồn tại.");
        }

        if (!Enum.TryParse(
                rarityText,
                true,
                out SummonRarity summonRarity))
        {
            throw new FormatException(
                $"SummonRarity '{rarityText}' không tồn tại.");
        }

        if (!Enum.TryParse(
                elementText,
                true,
                out Element element))
        {
            throw new FormatException(
                $"Element '{elementText}' không tồn tại.");
        }

        HeroCsvRow result = new()
        {
            Id = heroId,
            Name = heroName.Trim(),

            HeroAddressKey = heroAddressKey.Trim(),
            SpineAddressKey = spineAddressKey.Trim(),
            HeroIconKey = heroIconKey.Trim(),

            HeroClass = heroClass,
            SummonRarity = summonRarity,
            Element = element,

            Health = ParseFloat(
                GetCell(row, headers, "health"),
                "health"),

            Attack = ParseFloat(
                GetCell(row, headers, "attack"),
                "attack"),

            Defense = ParseFloat(
                GetCell(row, headers, "defense"),
                "defense"),

            CritChance = ParseFloat(
                GetCell(row, headers, "crit_chance"),
                "crit_chance"),

            CritDamage = ParseFloat(
                GetCell(row, headers, "crit_damage"),
                "crit_damage"),

            AttackSpeed = ParseFloat(
                GetCell(row, headers, "attack_speed"),
                "attack_speed"),

            AttackRange = ParseFloat(
                GetCell(row, headers, "attack_range"),
                "attack_range"),

            Accuracy = ParseFloat(
                GetCell(row, headers, "accuracy"),
                "accuracy"),

            SummonWeight = ParseInt(
                GetCell(row, headers, "summon_weight"),
                "summon_weight")
        };

        ValidateHeroValues(result);

        return result;
    }

    private static void ValidateRequiredString(
        string value,
        string columnName,
        int rowNumber)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return;

        throw new FormatException(
            $"{columnName} bị trống tại dòng {rowNumber}.");
    }

    private static void ValidateHeroValues(HeroCsvRow row)
    {
        if (row.Health <= 0f)
        {
            throw new FormatException(
                $"health phải lớn hơn 0. Hero ID: {row.Id}");
        }

        if (row.Attack < 0f)
        {
            throw new FormatException(
                $"attack không được nhỏ hơn 0. Hero ID: {row.Id}");
        }

        if (row.Defense < 0f)
        {
            throw new FormatException(
                $"defense không được nhỏ hơn 0. Hero ID: {row.Id}");
        }

        if (row.CritChance < 0f)
        {
            throw new FormatException(
                $"crit_chance không được nhỏ hơn 0. Hero ID: {row.Id}");
        }

        if (row.CritDamage < 0f)
        {
            throw new FormatException(
                $"crit_damage không được nhỏ hơn 0. Hero ID: {row.Id}");
        }

        if (row.AttackSpeed <= 0f)
        {
            throw new FormatException(
                $"attack_speed phải lớn hơn 0. Hero ID: {row.Id}");
        }

        if (row.AttackRange < 0f)
        {
            throw new FormatException(
                $"attack_range không được nhỏ hơn 0. Hero ID: {row.Id}");
        }

        if (row.Accuracy < 0f)
        {
            throw new FormatException(
                $"accuracy không được nhỏ hơn 0. Hero ID: {row.Id}");
        }

        if (row.SummonWeight < 1)
        {
            throw new FormatException(
                $"summon_weight phải lớn hơn hoặc bằng 1. Hero ID: {row.Id}");
        }
    }

    private static float ParseFloat(
        string value,
        string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException(
                $"{columnName} bị trống.");
        }

        string normalizedValue =
            value.Trim().Replace(',', '.');

        if (float.TryParse(
                normalizedValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result))
        {
            return result;
        }

        throw new FormatException(
            $"{columnName} có giá trị '{value}' không hợp lệ.");
    }

    private static int ParseInt(
        string value,
        string columnName)
    {
        if (int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int result))
        {
            return result;
        }

        throw new FormatException(
            $"{columnName} có giá trị '{value}' không hợp lệ.");
    }

    private static bool CreateOrUpdateAsset(HeroCsvRow row)
    {
        string assetName =
            $"{AssetNamePrefix}{row.Id}_{row.Name}.asset";

        string assetPath =
            $"{OutputFolder}/{assetName}";

        HeroDataSO heroData =
            AssetDatabase.LoadAssetAtPath<HeroDataSO>(assetPath);

        bool wasCreated = heroData == null;

        if (wasCreated)
        {
            heroData = CreateInstance<HeroDataSO>();
        }
        else
        {
            Undo.RecordObject(
                heroData,
                $"Update Hero Data {row.Id}");
        }

        heroData.Id = row.Id;
        heroData.Name = row.Name;

        heroData.Health = row.Health;
        heroData.Attack = row.Attack;
        heroData.Defense = row.Defense;
        heroData.CritChance = row.CritChance;
        heroData.CritDamage = row.CritDamage;
        heroData.AttackSpeed = row.AttackSpeed;
        heroData.AttackRange = row.AttackRange;
        heroData.Accuracy = row.Accuracy;

        heroData.HeroClass = row.HeroClass;
        heroData.Element = row.Element;

        heroData.SummonRarity = row.SummonRarity;
        heroData.SummonWeight = row.SummonWeight;

        heroData.HeroAddressKey = row.HeroAddressKey;
        heroData.SpineAddressKey = row.SpineAddressKey;
        heroData.HeroIconKey = row.HeroIconKey;

        /*
         * Không cập nhật các field này vì CSV không có:
         *
         * heroData.PortraitIcon
         * heroData.ShardIcon
         * heroData.IsAvailableInSummon
         *
         * Asset cũ sẽ giữ nguyên giá trị đã gán.
         * Asset mới sẽ sử dụng default của HeroDataSO.
         */

        if (wasCreated)
        {
            AssetDatabase.CreateAsset(heroData, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(heroData);
        }

        return wasCreated;
    }

    private static Dictionary<string, int> BuildHeaderDictionary(
        IReadOnlyList<string> headerRow)
    {
        Dictionary<string, int> headers =
            new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headerRow.Count; i++)
        {
            string header =
                NormalizeHeader(headerRow[i]);

            if (string.IsNullOrWhiteSpace(header))
                continue;

            if (!headers.TryAdd(header, i))
            {
                throw new InvalidOperationException(
                    $"Header '{header}' bị trùng trong CSV.");
            }
        }

        return headers;
    }

    private static string NormalizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return string.Empty;

        return header
            .Trim()
            .TrimStart('\uFEFF');
    }

    private static void ValidateRequiredHeaders(
        IReadOnlyDictionary<string, int> headers)
    {
        string[] requiredHeaders =
        {
            "hero_Id",
            "hero_Name",
            "key",
            "key_spine",
            "key_icon",
            "class",
            "rarity",
            "element",
            "health",
            "attack",
            "defense",
            "crit_chance",
            "crit_damage",
            "attack_speed",
            "attack_range",
            "accuracy",
            "summon_weight"
        };

        for (int i = 0; i < requiredHeaders.Length; i++)
        {
            string requiredHeader = requiredHeaders[i];

            if (!headers.ContainsKey(requiredHeader))
            {
                throw new InvalidOperationException(
                    $"CSV thiếu cột bắt buộc: {requiredHeader}");
            }
        }
    }

    private static string GetCell(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> headers,
        string header)
    {
        if (!headers.TryGetValue(header, out int index))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy header '{header}'.");
        }

        if (index < 0 || index >= row.Count)
            return string.Empty;

        return row[index]?.Trim() ?? string.Empty;
    }

    private static bool IsEmptyRow(
        IReadOnlyList<string> row)
    {
        if (row == null || row.Count == 0)
            return true;

        for (int i = 0; i < row.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(row[i]))
                return false;
        }

        return true;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string normalizedPath =
            folderPath.Replace("\\", "/");

        string[] folderParts =
            normalizedPath.Split('/');

        if (folderParts.Length == 0 ||
            folderParts[0] != "Assets")
        {
            throw new InvalidOperationException(
                $"Folder không hợp lệ: {folderPath}");
        }

        string currentPath = "Assets";

        for (int i = 1; i < folderParts.Length; i++)
        {
            string folderName = folderParts[i];

            if (string.IsNullOrWhiteSpace(folderName))
                continue;

            string nextPath =
                $"{currentPath}/{folderName}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    folderName);
            }

            currentPath = nextPath;
        }
    }

    private void AddLog(string message)
    {
        importLogs.Add(message);
        Repaint();
    }

    private sealed class HeroCsvRow
    {
        public int Id;
        public string Name;

        public string HeroAddressKey;
        public string SpineAddressKey;
        public string HeroIconKey;

        public HeroClass HeroClass;
        public SummonRarity SummonRarity;
        public Element Element;

        public float Health;
        public float Attack;
        public float Defense;
        public float CritChance;
        public float CritDamage;
        public float AttackSpeed;
        public float AttackRange;
        public float Accuracy;

        public int SummonWeight;
    }

    private static class CsvParser
    {
        public static List<List<string>> Parse(string content)
        {
            List<List<string>> rows = new();
            List<string> currentRow = new();
            StringBuilder currentCell = new();

            bool isInsideQuotes = false;

            for (int i = 0; i < content.Length; i++)
            {
                char character = content[i];

                if (character == '"')
                {
                    bool isEscapedQuote =
                        isInsideQuotes &&
                        i + 1 < content.Length &&
                        content[i + 1] == '"';

                    if (isEscapedQuote)
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        isInsideQuotes = !isInsideQuotes;
                    }

                    continue;
                }

                if (character == ',' && !isInsideQuotes)
                {
                    AddCell(currentRow, currentCell);
                    continue;
                }

                bool isNewLine =
                    character == '\r' ||
                    character == '\n';

                if (isNewLine && !isInsideQuotes)
                {
                    if (character == '\r' &&
                        i + 1 < content.Length &&
                        content[i + 1] == '\n')
                    {
                        i++;
                    }

                    AddCell(currentRow, currentCell);
                    AddRow(rows, currentRow);

                    currentRow = new List<string>();
                    continue;
                }

                currentCell.Append(character);
            }

            AddCell(currentRow, currentCell);

            if (!IsCompletelyEmpty(currentRow))
            {
                AddRow(rows, currentRow);
            }

            return rows;
        }

        private static void AddCell(
            ICollection<string> row,
            StringBuilder cellBuilder)
        {
            row.Add(cellBuilder.ToString());
            cellBuilder.Clear();
        }

        private static void AddRow(
            ICollection<List<string>> rows,
            List<string> row)
        {
            rows.Add(row);
        }

        private static bool IsCompletelyEmpty(
            IReadOnlyList<string> row)
        {
            for (int i = 0; i < row.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(row[i]))
                    return false;
            }

            return true;
        }
    }
}

#endif