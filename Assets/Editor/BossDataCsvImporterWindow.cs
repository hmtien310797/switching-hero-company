#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.Boss;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class BossDataCsvImporterWindow : EditorWindow
{
    /*
     * Google Sheet CSV URL:
     *
     * https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/export?format=csv&gid=SHEET_GID
     */
    private const string GoogleSheetCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1261891881&single=true&output=csv";

    /*
     * Folder lưu BossDataSO.
     * Tool tự tạo folder nếu chưa tồn tại.
     */
    private const string OutputFolder =
        "Assets/Immortal Switch/Addressable/Boss/Data";

    private const string AssetNamePrefix = "BossData_";

    private bool isImporting;
    private Vector2 logScrollPosition;

    private readonly List<string> importLogs = new();

    [MenuItem("Tools/Game Data/Boss Data Importer")]
    private static void OpenWindow()
    {
        BossDataCsvImporterWindow window =
            GetWindow<BossDataCsvImporterWindow>();

        window.titleContent = new GUIContent("Boss Data Importer");
        window.minSize = new Vector2(650f, 450f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10f);

        EditorGUILayout.LabelField(
            "Boss Data Google Sheet Importer",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(5f);

        DrawConfiguration();

        EditorGUILayout.Space(10f);

        using (new EditorGUI.DisabledScope(isImporting))
        {
            string buttonText = isImporting
                ? "Importing..."
                : "Download And Import Boss Data";

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
            "Tool sử dụng id làm định danh. " +
            "Asset chưa tồn tại sẽ được tạo mới, asset đã tồn tại sẽ được cập nhật. " +
            "Field Icon hiện tại sẽ được giữ nguyên.",
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

        AddLog("Đang tải CSV từ Google Sheet...");

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
                "Boss Data Importer",
                "Bạn chưa cập nhật GoogleSheetCsvUrl.",
                "OK");

            return false;
        }

        if (!GoogleSheetCsvUrl.StartsWith(
                "https://docs.google.com/spreadsheets/",
                StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Boss Data Importer",
                "GoogleSheetCsvUrl không hợp lệ.",
                "OK");

            return false;
        }

        if (!OutputFolder.StartsWith(
                "Assets/",
                StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog(
                "Boss Data Importer",
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

        HashSet<int> importedIds = new();

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
                    BossCsvRow bossCsvRow = ParseRow(
                        row,
                        rowIndex + 1,
                        headers);

                    if (!importedIds.Add(bossCsvRow.Id))
                    {
                        throw new InvalidOperationException(
                            $"Boss id {bossCsvRow.Id} bị trùng trong CSV.");
                    }

                    bool wasCreated =
                        CreateOrUpdateAsset(bossCsvRow);

                    if (wasCreated)
                    {
                        createdCount++;

                        AddLog(
                            $"Created: {bossCsvRow.Id} - " +
                            bossCsvRow.Name);
                    }
                    else
                    {
                        updatedCount++;

                        AddLog(
                            $"Updated: {bossCsvRow.Id} - " +
                            bossCsvRow.Name);
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;

                    AddLog(
                        $"Row {rowIndex + 1} failed: " +
                        exception.Message);

                    Debug.LogError(
                        $"[Boss Data Importer] " +
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
            "Boss Data Importer",
            $"Import hoàn tất.\n\n" +
            $"Created: {createdCount}\n" +
            $"Updated: {updatedCount}\n" +
            $"Skipped: {skippedCount}\n" +
            $"Failed: {failedCount}",
            "OK");
    }

    private static BossCsvRow ParseRow(
        IReadOnlyList<string> row,
        int csvRowNumber,
        IReadOnlyDictionary<string, int> headers)
    {
        string idText =
            GetCell(row, headers, "id");

        string name =
            GetCell(row, headers, "name");

        string bossAddressKey =
            GetCell(row, headers, "key");

        string iconKey =
            GetCell(row, headers, "icon_key");

        string elementText =
            GetCell(row, headers, "element");

        if (!int.TryParse(
                idText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int id))
        {
            throw new FormatException(
                $"id '{idText}' không hợp lệ.");
        }

        if (id <= 0)
        {
            throw new FormatException(
                $"id phải lớn hơn 0. Giá trị hiện tại: {id}");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new FormatException(
                $"name bị trống tại dòng {csvRowNumber}.");
        }

        if (string.IsNullOrWhiteSpace(bossAddressKey))
        {
            throw new FormatException(
                $"key bị trống tại dòng {csvRowNumber}.");
        }

        if (string.IsNullOrWhiteSpace(iconKey))
        {
            throw new FormatException(
                $"icon_key bị trống tại dòng {csvRowNumber}.");
        }

        if (string.IsNullOrWhiteSpace(elementText))
        {
            throw new FormatException(
                $"element bị trống tại dòng {csvRowNumber}.");
        }

        if (!Enum.TryParse(
                elementText,
                true,
                out Element element))
        {
            throw new FormatException(
                $"Element '{elementText}' không tồn tại " +
                $"trong enum {nameof(Element)}.");
        }

        BossCsvRow result = new()
        {
            Id = id,
            Name = name.Trim(),
            BossAddressKey = bossAddressKey.Trim(),
            IconKey = iconKey.Trim(),
            Element = element,

            BaseHp = ParseFloat(
                GetCell(row, headers, "health"),
                "health"),

            BaseAtk = ParseFloat(
                GetCell(row, headers, "attack"),
                "attack"),

            BaseDef = ParseFloat(
                GetCell(row, headers, "defense"),
                "defense"),

            AtkSpeed = ParseFloat(
                GetCell(row, headers, "attack_speed"),
                "attack_speed"),

            AttackRange = ParseFloat(
                GetCell(row, headers, "attack_range"),
                "attack_range"),

            MoveSpeed = ParseFloat(
                GetCell(row, headers, "move_speed"),
                "move_speed"),

            Accuracy = ParseFloat(
                GetCell(row, headers, "accuracy"),
                "accuracy")
        };

        ValidateBossValues(result);

        return result;
    }

    private static void ValidateBossValues(BossCsvRow row)
    {
        if (row.BaseHp <= 0f)
        {
            throw new FormatException(
                $"health phải lớn hơn 0. Boss id: {row.Id}");
        }

        if (row.BaseAtk < 0f)
        {
            throw new FormatException(
                $"attack không được nhỏ hơn 0. Boss id: {row.Id}");
        }

        if (row.BaseDef < 0f)
        {
            throw new FormatException(
                $"defense không được nhỏ hơn 0. Boss id: {row.Id}");
        }

        if (row.AtkSpeed <= 0f)
        {
            throw new FormatException(
                $"attack_speed phải lớn hơn 0. Boss id: {row.Id}");
        }

        if (row.AttackRange < 0f)
        {
            throw new FormatException(
                $"attack_range không được nhỏ hơn 0. Boss id: {row.Id}");
        }

        if (row.MoveSpeed < 0f)
        {
            throw new FormatException(
                $"move_speed không được nhỏ hơn 0. Boss id: {row.Id}");
        }

        if (row.Accuracy < 0f)
        {
            throw new FormatException(
                $"accuracy không được nhỏ hơn 0. Boss id: {row.Id}");
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

    private static bool CreateOrUpdateAsset(BossCsvRow row)
    {
        string assetName =
            $"{AssetNamePrefix}{row.Id}.asset";

        string assetPath =
            $"{OutputFolder}/{assetName}";

        BossDataSO bossData =
            AssetDatabase.LoadAssetAtPath<BossDataSO>(assetPath);

        bool wasCreated = bossData == null;

        if (wasCreated)
        {
            bossData = CreateInstance<BossDataSO>();
        }
        else
        {
            Undo.RecordObject(
                bossData,
                $"Update Boss Data {row.Id}");
        }

        bossData.Id = row.Id;
        bossData.Name = row.Name;
        bossData.Element = row.Element;

        bossData.IconKey = row.IconKey;
        bossData.BossAddressKey = row.BossAddressKey;

        /*
         * Không gán bossData.Icon.
         *
         * CSV chỉ có IconKey nên Sprite Icon được gán thủ công
         * trước đó sẽ được giữ nguyên khi import lại.
         */

        bossData.BaseHP = row.BaseHp;
        bossData.BaseAtk = row.BaseAtk;
        bossData.BaseDef = row.BaseDef;
        bossData.AtkSpeed = row.AtkSpeed;
        bossData.AttackRange = row.AttackRange;
        bossData.MoveSpeed = row.MoveSpeed;

        /*
         * CSV có Accuracy nhưng BossDataSO hiện chưa có field Accuracy.
         *
         * Nếu thêm:
         * public float Accuracy;
         *
         * thì mở dòng dưới:
         *
         * bossData.Accuracy = row.Accuracy;
         */

        if (wasCreated)
        {
            AssetDatabase.CreateAsset(bossData, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(bossData);
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
            "id",
            "name",
            "key",
            "icon_key",
            "element",
            "health",
            "attack",
            "defense",
            "attack_speed",
            "attack_range",
            "move_speed",
            "accuracy"
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

    private sealed class BossCsvRow
    {
        public int Id;
        public string Name;

        public string BossAddressKey;
        public string IconKey;

        public Element Element;

        public float BaseHp;
        public float BaseAtk;
        public float BaseDef;
        public float AtkSpeed;
        public float AttackRange;
        public float MoveSpeed;
        public float Accuracy;
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