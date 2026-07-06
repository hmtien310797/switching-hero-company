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
    private const string SkillIdentityGoogleSheetUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1104382280&single=true&output=csv";

    private const string SkillProgressGoogleSheetUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=1836872555&single=true&output=csv";

    private const string OutputFolder =
        "Assets/Immortal Switch/Addressable/Skill/Data";

    [SerializeField] private bool updateExistingAssets = true;

    private Vector2 scrollPosition;
    private UnityWebRequest activeRequest;
    private bool isDownloading;

    [MenuItem("Tools/Game Data/Import Skill Data From Google Sheet")]
    private static void OpenWindow()
    {
        SkillDataGoogleSheetImporterWindow window =
            GetWindow<SkillDataGoogleSheetImporterWindow>();

        window.titleContent =
            new GUIContent("Skill Data Importer");

        window.minSize = new Vector2(640f, 500f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition =
            EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Skill Data Google Sheet Importer",
            EditorStyles.boldLabel);

        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Một lần bấm sẽ tải và validate cả hai sheet trước khi sửa asset.\n\n" +
            "Skill Identity cập nhật:\n" +
            "• SkillId\n" +
            "• SkillKey\n" +
            "• SkillName\n" +
            "• IconSkillKey\n" +
            "• OwnerType = ClassSkill\n" +
            "• SkillTier\n\n" +
            "Skill Progress cập nhật:\n" +
            "• MaxLevel\n" +
            "• UpgradeShardCosts\n\n" +
            "Tool không sửa BasePhases, Levels, CastConfig, RuntimeObjectConfig, " +
            "SkillIcon, DescriptionTemplate, CustomBehaviourPrefab hoặc các config runtime khác.",
            MessageType.Info);

        updateExistingAssets = EditorGUILayout.Toggle(
            "Update Existing Assets",
            updateExistingAssets);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Skill Identity Source",
            EditorStyles.boldLabel);

        EditorGUILayout.SelectableLabel(
            SkillIdentityGoogleSheetUrl,
            EditorStyles.textField,
            GUILayout.Height(38f));

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(
            "Skill Progress Source",
            EditorStyles.boldLabel);

        EditorGUILayout.SelectableLabel(
            SkillProgressGoogleSheetUrl,
            EditorStyles.textField,
            GUILayout.Height(38f));

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField(
            "Output Folder",
            OutputFolder);

        EditorGUILayout.Space(12);

        GUI.enabled = !isDownloading;

        if (GUILayout.Button(
                "Download, Validate And Import All Skill Data",
                GUILayout.Height(44f)))
        {
            DownloadValidateAndImportAll();
        }

        GUI.enabled = true;

        if (isDownloading)
        {
            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Đang tải và kiểm tra Skill Identity + Skill Progress...",
                MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnDisable()
    {
        DisposeActiveRequest();
        isDownloading = false;
    }

    private void DownloadValidateAndImportAll()
    {
        if (isDownloading)
            return;

        isDownloading = true;
        Repaint();

        DownloadCsv(
            SkillIdentityGoogleSheetUrl,
            "Skill Identity",
            identityCsvText =>
            {
                DownloadCsv(
                    SkillProgressGoogleSheetUrl,
                    "Skill Progress",
                    progressCsvText =>
                    {
                        try
                        {
                            ValidateAndImportAllCsvText(
                                identityCsvText,
                                progressCsvText);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError(
                                "[Skill Data Sheet] Import thất bại.\n" +
                                exception);
                        }
                        finally
                        {
                            FinishDownloadProcess();
                        }
                    },
                    FinishDownloadProcess);
            },
            FinishDownloadProcess);
    }

    private void DownloadCsv(
        string csvUrl,
        string sourceName,
        Action<string> onLoaded,
        Action onFailed)
    {
        DisposeActiveRequest();

        if (string.IsNullOrWhiteSpace(csvUrl))
        {
            Debug.LogError(
                $"[{sourceName} Sheet] Google Sheet URL đang trống.");

            onFailed?.Invoke();
            return;
        }

        UnityWebRequest request =
            UnityWebRequest.Get(csvUrl);

        activeRequest = request;
        request.timeout = 30;

        UnityWebRequestAsyncOperation operation =
            request.SendWebRequest();

        operation.completed += _ =>
        {
            string downloadedContent = null;
            bool success = false;

            try
            {
                if (request.result !=
                    UnityWebRequest.Result.Success)
                {
                    Debug.LogError(
                        $"[{sourceName} Sheet] Không thể tải Google Sheet.\n" +
                        $"Error: {request.error}\n" +
                        $"Response Code: {request.responseCode}\n" +
                        $"URL: {csvUrl}");

                    return;
                }

                downloadedContent =
                    request.downloadHandler?.text;

                if (string.IsNullOrWhiteSpace(downloadedContent))
                {
                    Debug.LogError(
                        $"[{sourceName} Sheet] Google Sheet trả về nội dung trống.");

                    return;
                }

                if (LooksLikeHtml(downloadedContent))
                {
                    Debug.LogError(
                        $"[{sourceName} Sheet] Google trả về HTML thay vì CSV. " +
                        "Hãy kiểm tra lại link publish CSV.");

                    return;
                }

                success = true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[{sourceName} Sheet] Xử lý dữ liệu tải về thất bại.\n" +
                    exception);
            }
            finally
            {
                if (ReferenceEquals(activeRequest, request))
                    activeRequest = null;

                request.Dispose();
            }

            // Callback được gọi sau khi request cũ đã dispose,
            // tránh request Identity dispose nhầm request Progress.
            if (success)
                onLoaded?.Invoke(downloadedContent);
            else
                onFailed?.Invoke();
        };
    }

    private void FinishDownloadProcess()
    {
        isDownloading = false;
        DisposeActiveRequest();
        Repaint();
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

    private void ValidateAndImportAllCsvText(
        string identityCsvText,
        string progressCsvText)
    {
        SkillIdentityDocument identityDocument;
        SkillProgressDocument progressDocument;

        try
        {
            identityDocument =
                ParseIdentityDocument(identityCsvText);

            ValidateIdentityDocument(identityDocument);

            progressDocument =
                ParseProgressDocument(progressCsvText);

            ValidateProgressDocument(progressDocument);

            ValidateCrossSheetData(
                identityDocument,
                progressDocument);

            EnsureAssetFolderExists(OutputFolder);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[Skill Data Sheet] Validation thất bại. " +
                "Chưa có asset nào bị sửa.\n" +
                exception);

            return;
        }

        ImportAllDocuments(
            identityDocument,
            progressDocument);
    }

    private void ImportAllDocuments(
        SkillIdentityDocument identityDocument,
        SkillProgressDocument progressDocument)
    {
        int createdCount = 0;
        int updatedCount = 0;
        int unchangedCount = 0;
        int skippedCount = 0;
        int failedCount = 0;

        Dictionary<int, SkillDataSO> existingSkillsById =
            FindExistingSkillsById();

        bool assetEditingStarted = false;

        try
        {
            AssetDatabase.StartAssetEditing();
            assetEditingStarted = true;

            foreach (ParsedSkillIdentityRow identityRow
                     in identityDocument.ParsedRows)
            {
                try
                {
                    if (!progressDocument.RowsBySkillId.TryGetValue(
                            identityRow.SkillId,
                            out List<ParsedSkillProgressRow> progressRows))
                    {
                        failedCount++;

                        Debug.LogError(
                            "[Skill Data Sheet] Không tìm thấy progress cho " +
                            $"SkillId {identityRow.SkillId}.");

                        continue;
                    }

                    SkillDataSO skillData = null;
                    bool created = false;

                    existingSkillsById.TryGetValue(
                        identityRow.SkillId,
                        out skillData);

                    if (skillData == null)
                    {
                        string assetPath =
                            $"{OutputFolder}/{identityRow.SkillKey}.asset";

                        SkillDataSO assetAtPath =
                            AssetDatabase.LoadAssetAtPath<SkillDataSO>(
                                assetPath);

                        if (assetAtPath != null)
                        {
                            skillData = assetAtPath;

                            if (skillData.SkillId != identityRow.SkillId)
                            {
                                Debug.LogWarning(
                                    $"[Skill Data Sheet] Asset '{assetPath}' " +
                                    $"đang có SkillId cũ = {skillData.SkillId}. " +
                                    "Tool sẽ cập nhật thành SkillId mới = " +
                                    $"{identityRow.SkillId}.");
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
                        skippedCount++;
                        continue;
                    }

                    if (!created)
                    {
                        Undo.RecordObject(
                            skillData,
                            $"Import Skill Data {identityRow.SkillId}");
                    }

                    bool identityChanged =
                        ApplyIdentityData(
                            skillData,
                            identityRow);

                    bool progressChanged =
                        ApplyProgressData(
                            skillData,
                            progressRows);

                    if (created || identityChanged || progressChanged)
                        EditorUtility.SetDirty(skillData);

                    existingSkillsById[identityRow.SkillId] =
                        skillData;

                    if (created)
                    {
                        createdCount++;
                    }
                    else if (identityChanged || progressChanged)
                    {
                        updatedCount++;
                    }
                    else
                    {
                        unchangedCount++;
                    }
                }
                catch (Exception exception)
                {
                    failedCount++;

                    Debug.LogError(
                        "[Skill Data Sheet] Import SkillId " +
                        $"{identityRow.SkillId} thất bại.\n" +
                        exception);
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
            "<color=green>[Skill Data Import Complete]</color>\n" +
            $"Created: {createdCount}\n" +
            $"Updated: {updatedCount}\n" +
            $"Unchanged: {unchangedCount}\n" +
            $"Skipped: {skippedCount}\n" +
            $"Failed: {failedCount}");
    }

    private static bool ApplyIdentityData(
        SkillDataSO skillData,
        ParsedSkillIdentityRow rowData)
    {
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
            return false;

        skillData.SkillId = rowData.SkillId;
        skillData.SkillKey = rowData.SkillKey;
        skillData.SkillName = rowData.SkillName;
        skillData.IconSkillKey = rowData.IconSkillKey;
        skillData.OwnerType = SkillOwnerType.ClassSkill;
        skillData.SkillTier = rowData.SkillTier;

        return true;
    }

    private static bool ApplyProgressData(
        SkillDataSO skillData,
        List<ParsedSkillProgressRow> sourceRows)
    {
        List<ParsedSkillProgressRow> orderedRows =
            sourceRows
                .OrderBy(row => row.Level)
                .ToList();

        ParsedSkillProgressRow maxLevelRow =
            orderedRows.First(row => row.IsMaxLevel);

        int importedMaxLevel =
            maxLevelRow.Level;

        List<SkillUpgradeCostEntry> importedCosts =
            new();

        for (int i = 0; i < orderedRows.Count; i++)
        {
            ParsedSkillProgressRow row =
                orderedRows[i];

            // Max level không còn chi phí nâng tiếp.
            if (row.IsMaxLevel)
                continue;

            importedCosts.Add(
                new SkillUpgradeCostEntry
                {
                    Level = row.Level,
                    RequiredShard = row.ShardCostToNext
                });
        }

        bool changed =
            skillData.MaxLevel != importedMaxLevel ||
            !AreUpgradeCostsEqual(
                skillData.UpgradeShardCosts,
                importedCosts);

        if (!changed)
            return false;

        skillData.MaxLevel = importedMaxLevel;
        skillData.UpgradeShardCosts = importedCosts;

        return true;
    }

    private static bool AreUpgradeCostsEqual(
        IReadOnlyList<SkillUpgradeCostEntry> current,
        IReadOnlyList<SkillUpgradeCostEntry> imported)
    {
        int currentCount =
            current?.Count ?? 0;

        int importedCount =
            imported?.Count ?? 0;

        if (currentCount != importedCount)
            return false;

        for (int i = 0; i < currentCount; i++)
        {
            SkillUpgradeCostEntry currentEntry =
                current[i];

            SkillUpgradeCostEntry importedEntry =
                imported[i];

            if (currentEntry == null || importedEntry == null)
            {
                if (!ReferenceEquals(currentEntry, importedEntry))
                    return false;

                continue;
            }

            if (currentEntry.Level != importedEntry.Level ||
                currentEntry.RequiredShard != importedEntry.RequiredShard)
            {
                return false;
            }
        }

        return true;
    }

    private static SkillIdentityDocument ParseIdentityDocument(
        string csvText)
    {
        SkillCsvDocument csvDocument =
            ParseCsvDocument(
                csvText,
                "Skill Identity",
                new[]
                {
                    "skill_id",
                    "class",
                    "name",
                    "tier"
                });

        SkillIdentityDocument document =
            new()
            {
                CsvDocument = csvDocument
            };

        foreach (SkillCsvRow row in csvDocument.Rows)
        {
            if (!TryParseIdentityRow(
                    csvDocument,
                    row,
                    out ParsedSkillIdentityRow parsedRow))
            {
                continue;
            }

            document.ParsedRows.Add(parsedRow);
        }

        return document;
    }

    private static bool TryParseIdentityRow(
        SkillCsvDocument document,
        SkillCsvRow row,
        out ParsedSkillIdentityRow result)
    {
        result = null;

        string rawSkillId =
            GetCell(document, row, "skill_id");

        if (string.IsNullOrWhiteSpace(rawSkillId))
            return false;

        int skillId =
            ParsePositiveInt(
                rawSkillId,
                row,
                "skill_id");

        string className =
            GetRequiredCell(
                document,
                row,
                "class");

        string skillName =
            GetRequiredCell(
                document,
                row,
                "name");

        string tierText =
            GetRequiredCell(
                document,
                row,
                "tier");

        if (!Enum.TryParse(
                tierText,
                true,
                out TierSkill skillTier))
        {
            throw CreateRowException(
                row,
                $"Tier '{tierText}' không hợp lệ. " +
                "Giá trị hợp lệ: " +
                string.Join(
                    ", ",
                    Enum.GetNames(typeof(TierSkill))));
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

        result = new ParsedSkillIdentityRow
        {
            RowNumber = row.RowNumber,
            SkillId = skillId,
            SkillKey = skillKey,
            SkillName = skillName,
            IconSkillKey =
                $"icon_{skillKey}".ToLowerInvariant(),
            SkillTier = skillTier
        };

        return true;
    }

    private static void ValidateIdentityDocument(
        SkillIdentityDocument document)
    {
        if (document.ParsedRows.Count == 0)
        {
            throw new InvalidOperationException(
                "Skill Identity không có dòng skill hợp lệ.");
        }

        HashSet<int> skillIds = new();
        HashSet<string> skillKeys =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (ParsedSkillIdentityRow row
                 in document.ParsedRows)
        {
            if (!skillIds.Add(row.SkillId))
            {
                throw new InvalidOperationException(
                    $"Skill Identity dòng {row.RowNumber}: " +
                    $"SkillId {row.SkillId} bị trùng.");
            }

            if (!skillKeys.Add(row.SkillKey))
            {
                throw new InvalidOperationException(
                    $"Skill Identity dòng {row.RowNumber}: " +
                    $"SkillKey '{row.SkillKey}' bị trùng.");
            }
        }

        Debug.Log(
            "<color=green>[Skill Identity Validation Success]</color>\n" +
            $"Valid skills: {document.ParsedRows.Count}");
    }

    private static SkillProgressDocument ParseProgressDocument(
        string csvText)
    {
        SkillCsvDocument csvDocument =
            ParseCsvDocument(
                csvText,
                "Skill Progress",
                new[]
                {
                    "SkillId",
                    "Level",
                    "ShardCostToNext",
                    "IsMaxLevel"
                });

        SkillProgressDocument document = new();

        foreach (SkillCsvRow row in csvDocument.Rows)
        {
            string rawSkillId =
                GetCell(csvDocument, row, "SkillId");

            if (string.IsNullOrWhiteSpace(rawSkillId))
                continue;

            ParsedSkillProgressRow parsedRow =
                ParseProgressRow(
                    csvDocument,
                    row);

            if (!document.RowsBySkillId.TryGetValue(
                    parsedRow.SkillId,
                    out List<ParsedSkillProgressRow> skillRows))
            {
                skillRows =
                    new List<ParsedSkillProgressRow>();

                document.RowsBySkillId.Add(
                    parsedRow.SkillId,
                    skillRows);
            }

            skillRows.Add(parsedRow);
        }

        return document;
    }

    private static ParsedSkillProgressRow ParseProgressRow(
        SkillCsvDocument document,
        SkillCsvRow row)
    {
        int skillId =
            ParsePositiveInt(
                GetRequiredCell(
                    document,
                    row,
                    "SkillId"),
                row,
                "SkillId");

        int level =
            ParsePositiveInt(
                GetRequiredCell(
                    document,
                    row,
                    "Level"),
                row,
                "Level");

        string rawShardCost =
            GetRequiredCell(
                document,
                row,
                "ShardCostToNext");

        if (!int.TryParse(
                rawShardCost,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int shardCostToNext))
        {
            throw CreateRowException(
                row,
                $"ShardCostToNext '{rawShardCost}' không hợp lệ.");
        }

        if (shardCostToNext < 0)
        {
            throw CreateRowException(
                row,
                "ShardCostToNext không được nhỏ hơn 0.");
        }

        bool isMaxLevel =
            ParseBoolean(
                GetRequiredCell(
                    document,
                    row,
                    "IsMaxLevel"),
                row,
                "IsMaxLevel");

        return new ParsedSkillProgressRow
        {
            RowNumber = row.RowNumber,
            SkillId = skillId,
            Level = level,
            ShardCostToNext = shardCostToNext,
            IsMaxLevel = isMaxLevel
        };
    }

    private static void ValidateProgressDocument(
        SkillProgressDocument document)
    {
        if (document.RowsBySkillId.Count == 0)
        {
            throw new InvalidOperationException(
                "Skill Progress không có dòng data hợp lệ.");
        }

        int totalRowCount = 0;

        foreach (KeyValuePair<int, List<ParsedSkillProgressRow>> pair
                 in document.RowsBySkillId)
        {
            ValidateSkillProgressRows(
                pair.Key,
                pair.Value);

            totalRowCount += pair.Value.Count;
        }

        Debug.Log(
            "<color=green>[Skill Progress Validation Success]</color>\n" +
            $"Skills: {document.RowsBySkillId.Count}\n" +
            $"Rows: {totalRowCount}");
    }

    private static void ValidateSkillProgressRows(
        int skillId,
        List<ParsedSkillProgressRow> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            throw new InvalidOperationException(
                $"SkillId {skillId} không có progress data.");
        }

        List<ParsedSkillProgressRow> orderedRows =
            rows
                .OrderBy(row => row.Level)
                .ToList();

        HashSet<int> levels = new();

        for (int i = 0; i < orderedRows.Count; i++)
        {
            ParsedSkillProgressRow row =
                orderedRows[i];

            if (!levels.Add(row.Level))
            {
                throw new InvalidOperationException(
                    $"Skill Progress dòng {row.RowNumber}: " +
                    $"SkillId {skillId} bị trùng Level {row.Level}.");
            }
        }

        List<ParsedSkillProgressRow> maxLevelRows =
            orderedRows
                .Where(row => row.IsMaxLevel)
                .ToList();

        if (maxLevelRows.Count == 0)
        {
            throw new InvalidOperationException(
                $"SkillId {skillId} không có dòng IsMaxLevel = TRUE.");
        }

        if (maxLevelRows.Count > 1)
        {
            throw new InvalidOperationException(
                $"SkillId {skillId} có nhiều dòng IsMaxLevel = TRUE: " +
                string.Join(
                    ", ",
                    maxLevelRows.Select(row => row.Level)));
        }

        ParsedSkillProgressRow maxLevelRow =
            maxLevelRows[0];

        int highestLevel =
            orderedRows[orderedRows.Count - 1].Level;

        if (maxLevelRow.Level != highestLevel)
        {
            throw new InvalidOperationException(
                $"SkillId {skillId}: Level {maxLevelRow.Level} được đánh dấu max " +
                $"nhưng level cao nhất là {highestLevel}.");
        }

        if (maxLevelRow.ShardCostToNext != 0)
        {
            throw new InvalidOperationException(
                $"SkillId {skillId}, Level {maxLevelRow.Level} là max level " +
                "nên ShardCostToNext phải bằng 0.");
        }

        for (int i = 0; i < orderedRows.Count; i++)
        {
            ParsedSkillProgressRow row =
                orderedRows[i];

            int expectedLevel = i + 1;

            if (row.Level != expectedLevel)
            {
                throw new InvalidOperationException(
                    $"SkillId {skillId} thiếu Level {expectedLevel}. " +
                    $"Level đọc được tại vị trí này là {row.Level}.");
            }

            if (!row.IsMaxLevel && row.ShardCostToNext <= 0)
            {
                throw new InvalidOperationException(
                    $"SkillId {skillId}, Level {row.Level} chưa phải max " +
                    "nên ShardCostToNext phải lớn hơn 0.");
            }
        }
    }

    private static void ValidateCrossSheetData(
        SkillIdentityDocument identityDocument,
        SkillProgressDocument progressDocument)
    {
        HashSet<int> identitySkillIds =
            identityDocument.ParsedRows
                .Select(row => row.SkillId)
                .ToHashSet();

        HashSet<int> progressSkillIds =
            progressDocument.RowsBySkillId.Keys
                .ToHashSet();

        List<int> missingProgressSkillIds =
            identitySkillIds
                .Where(skillId =>
                    !progressSkillIds.Contains(skillId))
                .OrderBy(skillId => skillId)
                .ToList();

        if (missingProgressSkillIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Các SkillId sau có trong Skill Identity nhưng thiếu Skill Progress:\n" +
                string.Join(
                    ", ",
                    missingProgressSkillIds));
        }

        List<int> missingIdentitySkillIds =
            progressSkillIds
                .Where(skillId =>
                    !identitySkillIds.Contains(skillId))
                .OrderBy(skillId => skillId)
                .ToList();

        if (missingIdentitySkillIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Các SkillId sau có trong Skill Progress nhưng thiếu Skill Identity:\n" +
                string.Join(
                    ", ",
                    missingIdentitySkillIds));
        }

        Debug.Log(
            "<color=green>[Skill Cross-Sheet Validation Success]</color>\n" +
            $"Matched skills: {identitySkillIds.Count}");
    }

    private static SkillCsvDocument ParseCsvDocument(
        string csvText,
        string sourceName,
        IReadOnlyList<string> requiredHeaders)
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            throw new InvalidOperationException(
                $"{sourceName} CSV đang trống.");
        }

        List<List<string>> records =
            ParseCsvRecords(csvText);

        if (records.Count < 2)
        {
            throw new InvalidOperationException(
                $"{sourceName} CSV phải có header và ít nhất một dòng data.");
        }

        List<string> headers =
            records[0]
                .Select(header =>
                    RemoveBom(header ?? string.Empty).Trim())
                .ToList();

        ValidateHeaders(
            sourceName,
            headers,
            requiredHeaders);

        SkillCsvDocument document =
            new()
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
                    $"{sourceName}: Header '{headers[i]}' bị trùng.");
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
        string sourceName,
        IReadOnlyList<string> headers,
        IReadOnlyList<string> requiredHeaders)
    {
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
                    $"{sourceName} thiếu cột bắt buộc '{requiredHeader}'.");
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

    private static int ParsePositiveInt(
        string rawValue,
        SkillCsvRow row,
        string columnName)
    {
        if (!int.TryParse(
                rawValue?.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value))
        {
            throw CreateRowException(
                row,
                $"{columnName} '{rawValue}' không hợp lệ.");
        }

        if (value <= 0)
        {
            throw CreateRowException(
                row,
                $"{columnName} phải lớn hơn 0, hiện tại là {value}.");
        }

        return value;
    }

    private static bool ParseBoolean(
        string rawValue,
        SkillCsvRow row,
        string columnName)
    {
        string normalizedValue =
            rawValue?
                .Trim()
                .ToLowerInvariant();

        switch (normalizedValue)
        {
            case "true":
            case "1":
            case "yes":
            case "y":
                return true;

            case "false":
            case "0":
            case "no":
            case "n":
                return false;

            default:
                throw CreateRowException(
                    row,
                    $"{columnName} '{rawValue}' không hợp lệ. " +
                    "Chỉ chấp nhận TRUE/FALSE.");
        }
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

    private static Dictionary<int, SkillDataSO>
        FindExistingSkillsById()
    {
        Dictionary<int, SkillDataSO> result = new();

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

            if (skillData == null || skillData.SkillId <= 0)
                continue;

            if (result.ContainsKey(skillData.SkillId))
            {
                Debug.LogWarning(
                    "[Skill Data Sheet] Có nhiều SkillDataSO cùng SkillId " +
                    $"{skillData.SkillId}. Tool sẽ dùng asset đầu tiên tìm thấy. " +
                    $"Asset bị bỏ qua: {assetPath}");

                continue;
            }

            result.Add(
                skillData.SkillId,
                skillData);
        }

        return result;
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

    private sealed class ParsedSkillIdentityRow
    {
        public int RowNumber;
        public int SkillId;
        public string SkillKey;
        public string SkillName;
        public string IconSkillKey;
        public TierSkill SkillTier;
    }

    private sealed class ParsedSkillProgressRow
    {
        public int RowNumber;
        public int SkillId;
        public int Level;
        public int ShardCostToNext;
        public bool IsMaxLevel;
    }

    private sealed class SkillIdentityDocument
    {
        public SkillCsvDocument CsvDocument;
        public List<ParsedSkillIdentityRow> ParsedRows = new();
    }

    private sealed class SkillProgressDocument
    {
        public Dictionary<int, List<ParsedSkillProgressRow>>
            RowsBySkillId = new();
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
