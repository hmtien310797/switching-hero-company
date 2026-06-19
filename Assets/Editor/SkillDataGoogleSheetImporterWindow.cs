#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Immortal_Switch.Scripts.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class SkillDataGoogleSheetImporterWindow : EditorWindow
{
    private const string GoogleSheetUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1104382280&single=true&output=csv";

    private const string OutputFolder =
        "Assets/Immortal Switch/Addressable/Skill/Data";

    [SerializeField] private TextAsset fallbackCsvFile;
    [SerializeField] private bool updateExistingAssets = true;

    private Vector2 scrollPosition;
    private UnityWebRequest activeRequest;
    private bool isDownloading;

    [MenuItem("Tools/Game Data/Import Skill Identity From Google Sheet")]
    private static void OpenWindow()
    {
        SkillDataGoogleSheetImporterWindow window =
            GetWindow<SkillDataGoogleSheetImporterWindow>();

        window.titleContent =
            new GUIContent("Skill Identity Importer");

        window.minSize = new Vector2(560f, 360f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition =
            EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Skill Identity Google Sheet Importer",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Tool chỉ cập nhật:\n" +
            "• SkillId\n" +
            "• SkillKey\n" +
            "• SkillName\n" +
            "• IconSkillKey\n" +
            "• OwnerType = ClassSkill\n" +
            "• SkillTier\n\n" +
            "Tool không đọc hoặc sửa BasePhases, Levels, CastConfig, " +
            "RuntimeObjectConfig, SkillIcon, DescriptionTemplate và MaxLevel.",
            MessageType.Info);

        fallbackCsvFile =
            (TextAsset)EditorGUILayout.ObjectField(
                "Fallback CSV",
                fallbackCsvFile,
                typeof(TextAsset),
                false);

        updateExistingAssets = EditorGUILayout.Toggle(
            "Update Existing Assets",
            updateExistingAssets);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Source",
            EditorStyles.boldLabel);

        EditorGUILayout.SelectableLabel(
            GoogleSheetUrl,
            EditorStyles.textField,
            GUILayout.Height(38f));

        EditorGUILayout.LabelField(
            "Output Folder",
            OutputFolder);

        EditorGUILayout.Space(12);

        GUI.enabled = !isDownloading;

        if (GUILayout.Button(
                "Download, Validate And Import",
                GUILayout.Height(44f)))
        {
            LoadSourceCsv(ValidateAndImportCsvText);
        }

        GUI.enabled = true;

        if (isDownloading)
        {
            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Đang tải dữ liệu từ Google Sheet...",
                MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnDisable()
    {
        DisposeActiveRequest();
    }

    private void LoadSourceCsv(Action<string> onLoaded)
    {
        if (isDownloading)
            return;

        if (fallbackCsvFile != null)
        {
            onLoaded?.Invoke(fallbackCsvFile.text);
            return;
        }

        DownloadCsv(GoogleSheetUrl, onLoaded);
    }

    private void DownloadCsv(
        string csvUrl,
        Action<string> onLoaded)
    {
        DisposeActiveRequest();

        isDownloading = true;
        Repaint();

        activeRequest = UnityWebRequest.Get(csvUrl);
        activeRequest.timeout = 30;

        UnityWebRequestAsyncOperation operation =
            activeRequest.SendWebRequest();

        operation.completed += _ =>
        {
            try
            {
                if (activeRequest == null)
                    return;

                if (activeRequest.result !=
                    UnityWebRequest.Result.Success)
                {
                    Debug.LogError(
                        "[Skill Identity Sheet] Không thể tải Google Sheet.\n" +
                        $"Error: {activeRequest.error}\n" +
                        $"Response Code: {activeRequest.responseCode}\n" +
                        $"URL: {csvUrl}");

                    return;
                }

                string content =
                    activeRequest.downloadHandler?.text;

                if (string.IsNullOrWhiteSpace(content))
                {
                    Debug.LogError(
                        "[Skill Identity Sheet] Google Sheet trả về nội dung trống.");

                    return;
                }

                if (LooksLikeHtml(content))
                {
                    Debug.LogError(
                        "[Skill Identity Sheet] Google trả về HTML thay vì CSV.");

                    return;
                }

                onLoaded?.Invoke(content);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Skill Identity Sheet] Xử lý dữ liệu thất bại.\n{exception}");
            }
            finally
            {
                isDownloading = false;
                DisposeActiveRequest();
                Repaint();
            }
        };
    }

    private void DisposeActiveRequest()
    {
        if (activeRequest == null)
            return;

        activeRequest.Dispose();
        activeRequest = null;
    }

    private static bool LooksLikeHtml(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        string value = content.TrimStart();

        return value.StartsWith(
                   "<!DOCTYPE html",
                   StringComparison.OrdinalIgnoreCase)
               ||
               value.StartsWith(
                   "<html",
                   StringComparison.OrdinalIgnoreCase);
    }

    private void ValidateAndImportCsvText(string csvText)
    {
        SkillCsvDocument document;

        try
        {
            document = ParseDocument(csvText);
            ValidateDocument(document);
            EnsureAssetFolderExists(OutputFolder);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[Skill Identity Sheet] Validation thất bại. " +
                $"Chưa có asset nào bị sửa.\n{exception}");

            return;
        }

        int createdCount = 0;
        int updatedCount = 0;
        int unchangedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        bool assetEditingStarted = false;

        try
        {
            AssetDatabase.StartAssetEditing();
            assetEditingStarted = true;

            foreach (SkillCsvRow row in document.Rows)
            {
                try
                {
                    if (!TryReadRow(
                            document,
                            row,
                            out ParsedSkillRow parsedRow))
                    {
                        skippedCount++;
                        continue;
                    }

                    ImportResult result =
                        ImportRow(parsedRow, row);

                    switch (result)
                    {
                        case ImportResult.Created:
                            createdCount++;
                            break;

                        case ImportResult.Updated:
                            updatedCount++;
                            break;

                        case ImportResult.Unchanged:
                            unchangedCount++;
                            break;

                        case ImportResult.Skipped:
                            skippedCount++;
                            break;
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;

                    Debug.LogError(
                        $"[Skill Identity Sheet] Lỗi tại dòng " +
                        $"{row.RowNumber}.\n{exception.Message}");
                }
            }
        }
        finally
        {
            if (assetEditingStarted)
                AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "<color=green>[Skill Identity Import Complete]</color>\n" +
            $"Created: {createdCount}\n" +
            $"Updated: {updatedCount}\n" +
            $"Unchanged: {unchangedCount}\n" +
            $"Skipped: {skippedCount}\n" +
            $"Failed: {failedCount}");
    }

    private static void ValidateDocument(
        SkillCsvDocument document)
    {
        int validRowCount = 0;
        HashSet<int> skillIds = new();

        foreach (SkillCsvRow row in document.Rows)
        {
            if (!TryReadRow(
                    document,
                    row,
                    out ParsedSkillRow parsedRow))
            {
                continue;
            }

            if (!skillIds.Add(parsedRow.SkillId))
            {
                throw CreateRowException(
                    row,
                    $"SkillId {parsedRow.SkillId} bị trùng trong Sheet.");
            }

            validRowCount++;
        }

        if (validRowCount == 0)
        {
            throw new InvalidOperationException(
                "Không tìm thấy dòng skill hợp lệ.");
        }

        Debug.Log(
            "<color=green>[Skill Identity Validation Success]</color>\n" +
            $"Valid skills: {validRowCount}");
    }

    private ImportResult ImportRow(
        ParsedSkillRow rowData,
        SkillCsvRow sourceRow)
    {
        SkillDataSO skillData =
            FindExistingSkill(rowData.SkillId);

        bool created = false;

        if (skillData == null)
        {
            string assetPath =
                $"{OutputFolder}/{rowData.SkillKey}.asset";

            SkillDataSO assetAtPath =
                AssetDatabase.LoadAssetAtPath<SkillDataSO>(
                    assetPath);

            if (assetAtPath != null)
            {
                skillData = assetAtPath;

                if (skillData.SkillId != rowData.SkillId)
                {
                    Debug.LogWarning(
                        $"[Skill Identity Sheet] Asset '{assetPath}' " +
                        $"đang có SkillId cũ = {skillData.SkillId}. " +
                        $"Tool sẽ cập nhật thành SkillId mới = {rowData.SkillId}.");
                }
            }
            else
            {
                skillData =
                    CreateInstance<SkillDataSO>();

                AssetDatabase.CreateAsset(
                    skillData,
                    assetPath);

                created = true;
            }
        }
        else if (!updateExistingAssets)
        {
            return ImportResult.Skipped;
        }

        bool changed =
            skillData.SkillId != rowData.SkillId ||
            !string.Equals(
                skillData.SkillKey,
                rowData.SkillKey,
                StringComparison.Ordinal) ||
            !string.Equals(
                skillData.SkillName,
                rowData.SkillName,
                StringComparison.Ordinal) ||
            !string.Equals(
                skillData.IconSkillKey,
                rowData.IconSkillKey,
                StringComparison.Ordinal) ||
            skillData.OwnerType != SkillOwnerType.ClassSkill ||
            skillData.SkillTier != rowData.SkillTier;

        if (!changed)
            return created
                ? ImportResult.Created
                : ImportResult.Unchanged;

        Undo.RecordObject(
            skillData,
            $"Import Skill Identity {rowData.SkillId}");

        skillData.SkillId = rowData.SkillId;
        skillData.SkillKey = rowData.SkillKey;
        skillData.SkillName = rowData.SkillName;
        skillData.IconSkillKey = rowData.IconSkillKey;
        skillData.OwnerType = SkillOwnerType.ClassSkill;
        skillData.SkillTier = rowData.SkillTier;

        EditorUtility.SetDirty(skillData);

        return created
            ? ImportResult.Created
            : ImportResult.Updated;
    }

    private static bool TryReadRow(
        SkillCsvDocument document,
        SkillCsvRow row,
        out ParsedSkillRow result)
    {
        result = null;

        string rawSkillId =
            GetCell(document, row, "skill_id");

        if (string.IsNullOrWhiteSpace(rawSkillId))
            return false;

        if (!int.TryParse(
                rawSkillId.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int skillId))
        {
            throw CreateRowException(
                row,
                $"skill_id '{rawSkillId}' không hợp lệ.");
        }

        if (skillId <= 0)
        {
            throw CreateRowException(
                row,
                $"skill_id phải lớn hơn 0, hiện tại là {skillId}.");
        }

        string className =
            GetRequiredCell(document, row, "class");

        string skillName =
            GetRequiredCell(document, row, "name");

        string tierText =
            GetRequiredCell(document, row, "tier");

        if (!Enum.TryParse(
                tierText,
                true,
                out TierSkill skillTier))
        {
            throw CreateRowException(
                row,
                $"Tier '{tierText}' không hợp lệ. " +
                $"Giá trị hợp lệ: " +
                $"{string.Join(", ", Enum.GetNames(typeof(TierSkill)))}");
        }

        string normalizedClass =
            NormalizeKeyPart(className);

        if (string.IsNullOrWhiteSpace(normalizedClass))
        {
            throw CreateRowException(
                row,
                $"Class '{className}' không thể dùng để tạo SkillKey.");
        }

        string skillKey =
            $"skilldata_{normalizedClass}_{skillId}"
                .ToLowerInvariant();

        result = new ParsedSkillRow
        {
            SkillId = skillId,
            SkillKey = skillKey,
            SkillName = skillName,
            IconSkillKey =
                $"icon_{skillKey}".ToLowerInvariant(),
            SkillTier = skillTier
        };

        return true;
    }

    private static SkillCsvDocument ParseDocument(
        string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            throw new InvalidOperationException(
                "Nội dung CSV đang trống.");
        }

        List<List<string>> records =
            ParseCsvRecords(csvText);

        if (records.Count < 2)
        {
            throw new InvalidOperationException(
                "CSV phải có header và ít nhất một dòng data.");
        }

        List<string> headers = records[0]
            .Select(header =>
                RemoveBom(header ?? string.Empty).Trim())
            .ToList();

        ValidateHeaders(headers);

        SkillCsvDocument document = new()
        {
            Headers = headers
        };

        for (int i = 0; i < headers.Count; i++)
        {
            string normalizedHeader =
                NormalizeHeader(headers[i]);

            if (document.FirstHeaderIndex.ContainsKey(
                    normalizedHeader))
            {
                throw new InvalidOperationException(
                    $"Header '{headers[i]}' bị trùng.");
            }

            document.FirstHeaderIndex.Add(
                normalizedHeader,
                i);
        }

        for (int rowIndex = 1;
             rowIndex < records.Count;
             rowIndex++)
        {
            List<string> cells =
                records[rowIndex];

            if (cells.All(string.IsNullOrWhiteSpace))
                continue;

            while (cells.Count < headers.Count)
                cells.Add(string.Empty);

            document.Rows.Add(
                new SkillCsvRow
                {
                    RowNumber = rowIndex + 1,
                    Cells = cells
                });
        }

        return document;
    }

    private static void ValidateHeaders(
        IReadOnlyList<string> headers)
    {
        string[] requiredHeaders =
        {
            "skill_id",
            "class",
            "name",
            "tier"
        };

        HashSet<string> normalizedHeaders =
            headers
                .Select(NormalizeHeader)
                .ToHashSet();

        foreach (string requiredHeader in requiredHeaders)
        {
            if (!normalizedHeaders.Contains(
                    NormalizeHeader(requiredHeader)))
            {
                throw new InvalidOperationException(
                    $"Thiếu cột bắt buộc '{requiredHeader}'.");
            }
        }
    }

    private static List<List<string>> ParseCsvRecords(
        string csvText)
    {
        List<List<string>> records = new();
        List<string> currentRecord = new();
        StringBuilder currentField = new();

        bool insideQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char character = csvText[i];

            if (insideQuotes)
            {
                if (character == '"')
                {
                    bool escapedQuote =
                        i + 1 < csvText.Length &&
                        csvText[i + 1] == '"';

                    if (escapedQuote)
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    insideQuotes = true;
                    break;

                case ',':
                    currentRecord.Add(
                        currentField.ToString());

                    currentField.Clear();
                    break;

                case '\r':
                    break;

                case '\n':
                    currentRecord.Add(
                        currentField.ToString());

                    currentField.Clear();

                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                    break;

                default:
                    currentField.Append(character);
                    break;
            }
        }

        if (insideQuotes)
        {
            throw new FormatException(
                "CSV có field mở dấu quote nhưng không đóng.");
        }

        if (currentField.Length > 0 ||
            currentRecord.Count > 0)
        {
            currentRecord.Add(
                currentField.ToString());

            records.Add(currentRecord);
        }

        return records;
    }

    private static string GetRequiredCell(
        SkillCsvDocument document,
        SkillCsvRow row,
        string header)
    {
        string value =
            GetCell(document, row, header);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw CreateRowException(
                row,
                $"Cột '{header}' đang trống.");
        }

        return value.Trim();
    }

    private static string GetCell(
        SkillCsvDocument document,
        SkillCsvRow row,
        string header)
    {
        string normalizedHeader =
            NormalizeHeader(header);

        if (!document.FirstHeaderIndex.TryGetValue(
                normalizedHeader,
                out int columnIndex))
        {
            throw CreateRowException(
                row,
                $"Không tìm thấy cột '{header}'.");
        }

        if (columnIndex < 0 ||
            columnIndex >= row.Cells.Count)
        {
            return string.Empty;
        }

        return row.Cells[columnIndex];
    }

    private static SkillDataSO FindExistingSkill(
        int skillId)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:SkillDataSO",
                new[] { OutputFolder });

        foreach (string guid in guids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(guid);

            SkillDataSO skillData =
                AssetDatabase.LoadAssetAtPath<SkillDataSO>(
                    assetPath);

            if (skillData != null &&
                skillData.SkillId == skillId)
            {
                return skillData;
            }
        }

        return null;
    }

    private static void EnsureAssetFolderExists(
        string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new InvalidOperationException(
                "Output Folder đang trống.");
        }

        folderPath = folderPath
            .Replace("\\", "/")
            .TrimEnd('/');

        if (!folderPath.Equals(
                "Assets",
                StringComparison.Ordinal) &&
            !folderPath.StartsWith(
                "Assets/",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Output Folder phải nằm bên trong Assets.");
        }

        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts =
            folderPath.Split('/');

        string currentPath =
            parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath =
                $"{currentPath}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(
                    currentPath,
                    parts[i]);
            }

            currentPath = nextPath;
        }
    }

    private static string NormalizeHeader(string value)
    {
        return RemoveBom(value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }

    private static string RemoveBom(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.TrimStart('\uFEFF');
    }

    private static string NormalizeKeyPart(string value)
    {
        string normalized = Regex.Replace(
            value.Trim().ToLowerInvariant(),
            @"[^a-z0-9]+",
            "_");

        return normalized.Trim('_');
    }

    private static Exception CreateRowException(
        SkillCsvRow row,
        string message)
    {
        return new InvalidOperationException(
            $"CSV dòng {row.RowNumber}: {message}");
    }

    private enum ImportResult
    {
        Created,
        Updated,
        Unchanged,
        Skipped
    }

    private sealed class ParsedSkillRow
    {
        public int SkillId;
        public string SkillKey;
        public string SkillName;
        public string IconSkillKey;
        public TierSkill SkillTier;
    }

    private sealed class SkillCsvDocument
    {
        public List<string> Headers = new();

        public Dictionary<string, int> FirstHeaderIndex =
            new(StringComparer.OrdinalIgnoreCase);

        public List<SkillCsvRow> Rows = new();
    }

    private sealed class SkillCsvRow
    {
        public int RowNumber;
        public List<string> Cells = new();
    }
}

#endif
