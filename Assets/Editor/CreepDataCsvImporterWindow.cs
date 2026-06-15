#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Immortal_Switch.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class CreepDataCsvImporterWindow : EditorWindow
{
    /*
     * Link Google Sheet CSV.
     *
     * Format:
     * https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/export?format=csv&gid=SHEET_GID
     */
    private const string GoogleSheetCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=0&single=true&output=csv";

    /*
     * Folder lưu CreepDataSo.
     * Tool sẽ tự tạo folder nếu chưa tồn tại.
     */
    private const string OutputFolder =
        "Assets/Immortal Switch/Addressable/Enemy/Data";

    private const string AssetNamePrefix = "CreepData_";

    private bool isImporting;
    private Vector2 logScrollPosition;

    private readonly List<string> importLogs = new();

    [MenuItem("Tools/Game Data/Creep Data Importer")]
    private static void OpenWindow()
    {
        CreepDataCsvImporterWindow window =
            GetWindow<CreepDataCsvImporterWindow>();

        window.titleContent = new GUIContent("Creep Data Importer");
        window.minSize = new Vector2(650f, 450f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10f);

        EditorGUILayout.LabelField(
            "Creep Data Google Sheet Importer",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(5f);

        DrawConfiguration();

        EditorGUILayout.Space(10f);

        using (new EditorGUI.DisabledScope(isImporting))
        {
            string buttonText = isImporting
                ? "Importing..."
                : "Download And Import Creep Data";

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
            "Tool sử dụng enemyId làm định danh. " +
            "Nếu asset đã tồn tại thì cập nhật, chưa tồn tại thì tạo mới.",
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
            foreach (string log in importLogs)
            {
                EditorGUILayout.LabelField(
                    log,
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
                "Creep Data Importer",
                "Bạn chưa cập nhật GoogleSheetCsvUrl.",
                "OK");

            return false;
        }

        if (!GoogleSheetCsvUrl.StartsWith(
                "https://docs.google.com/spreadsheets/",
                StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Creep Data Importer",
                "GoogleSheetCsvUrl không hợp lệ.",
                "OK");

            return false;
        }

        if (!OutputFolder.StartsWith(
                "Assets/",
                StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog(
                "Creep Data Importer",
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
                    CreepCsvRow creepCsvRow = ParseRow(
                        row,
                        rowIndex + 1,
                        headers);

                    bool wasCreated =
                        CreateOrUpdateAsset(creepCsvRow);

                    if (wasCreated)
                    {
                        createdCount++;

                        AddLog(
                            $"Created: {creepCsvRow.EnemyId} - " +
                            $"{creepCsvRow.Name}");
                    }
                    else
                    {
                        updatedCount++;

                        AddLog(
                            $"Updated: {creepCsvRow.EnemyId} - " +
                            $"{creepCsvRow.Name}");
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;

                    AddLog(
                        $"Row {rowIndex + 1} failed: " +
                        exception.Message);

                    Debug.LogError(
                        $"[Creep Data Importer] " +
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
            "Creep Data Importer",
            $"Import hoàn tất.\n\n" +
            $"Created: {createdCount}\n" +
            $"Updated: {updatedCount}\n" +
            $"Skipped: {skippedCount}\n" +
            $"Failed: {failedCount}",
            "OK");
    }

    private static CreepCsvRow ParseRow(
        IReadOnlyList<string> row,
        int csvRowNumber,
        IReadOnlyDictionary<string, int> headers)
    {
        string enemyIdText =
            GetCell(row, headers, "enemyId");

        string name =
            GetCell(row, headers, "name");

        string addressKey =
            GetCell(row, headers, "key");

        string iconKey =
            GetCell(row, headers, "icon_key");

        string elementText =
            GetCell(row, headers, "element");

        if (!int.TryParse(
                enemyIdText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int enemyId))
        {
            throw new FormatException(
                $"enemyId '{enemyIdText}' không hợp lệ.");
        }

        if (enemyId <= 0)
        {
            throw new FormatException(
                $"enemyId phải lớn hơn 0. Giá trị: {enemyId}");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new FormatException(
                $"name bị trống tại dòng {csvRowNumber}.");
        }

        if (string.IsNullOrWhiteSpace(addressKey))
        {
            throw new FormatException(
                $"key bị trống tại dòng {csvRowNumber}.");
        }

        if (string.IsNullOrWhiteSpace(iconKey))
        {
            throw new FormatException(
                $"icon_key bị trống tại dòng {csvRowNumber}.");
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

        return new CreepCsvRow
        {
            EnemyId = enemyId,
            Name = name,
            AddressKey = addressKey,
            IconKey = iconKey,
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

            BaseAtkSpeed = ParseFloat(
                GetCell(row, headers, "attack_speed"),
                "attack_speed"),

            BaseRange = ParseFloat(
                GetCell(row, headers, "attack_range"),
                "attack_range"),

            BaseMoveSpeed = ParseFloat(
                GetCell(row, headers, "move_speed"),
                "move_speed"),

            BaseAccuracy = ParseFloat(
                GetCell(row, headers, "accuracy"),
                "accuracy")
        };
    }

    private static float ParseFloat(
        string value,
        string columnName)
    {
        if (float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result))
        {
            return result;
        }

        throw new FormatException(
            $"{columnName} có giá trị '{value}' không hợp lệ.");
    }

    private static bool CreateOrUpdateAsset(CreepCsvRow row)
    {
        string assetName =
            $"{AssetNamePrefix}{row.EnemyId}.asset";

        string assetPath =
            $"{OutputFolder}/{assetName}";

        CreepDataSo creepData =
            AssetDatabase.LoadAssetAtPath<CreepDataSo>(assetPath);

        bool wasCreated = creepData == null;

        if (wasCreated)
        {
            creepData = CreateInstance<CreepDataSo>();
        }
        else
        {
            Undo.RecordObject(
                creepData,
                $"Update Creep Data {row.EnemyId}");
        }

        creepData.Id = row.EnemyId;
        creepData.Name = row.Name;

        creepData.IconKey = row.IconKey;
        creepData.CreepAddressKey = row.AddressKey;

        creepData.Element = row.Element;

        creepData.BaseHp = row.BaseHp;
        creepData.BaseAtk = row.BaseAtk;
        creepData.BaseDef = row.BaseDef;
        creepData.BaseAtkSpeed = row.BaseAtkSpeed;
        creepData.BaseRange = row.BaseRange;
        creepData.BaseMoveSpeed = row.BaseMoveSpeed;
        creepData.BaseAccuracy = row.BaseAccuracy;

        if (wasCreated)
        {
            AssetDatabase.CreateAsset(creepData, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(creepData);
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
            "enemyId",
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

        foreach (string requiredHeader in requiredHeaders)
        {
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
        {
            return string.Empty;
        }

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

    private sealed class CreepCsvRow
    {
        public int EnemyId;
        public string Name;

        public string IconKey;
        public string AddressKey;

        public Element Element;

        public float BaseHp;
        public float BaseAtk;
        public float BaseDef;
        public float BaseAtkSpeed;
        public float BaseRange;
        public float BaseMoveSpeed;
        public float BaseAccuracy;
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