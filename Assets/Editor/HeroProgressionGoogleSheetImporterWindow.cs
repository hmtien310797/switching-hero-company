#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Immortal_Switch.Scripts.Hero.Editor
{
    public class HeroProgressionGoogleSheetImporterWindow : EditorWindow
    {

        private const string googleSheetUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQq5Rq5h3ZiaDfG8U6-Q3hytEOHs3DqRgBETG7qcE2LjQZAhwR971MjEZqgc6wmsb_1Ey1mPK9-R13S/pub?gid=642786794&single=true&output=csv";
        private const string outputFolder = "Assets/Immortal Switch/Addressable/Hero/ProgressionData";
        [SerializeField] private bool deleteMissingNodes = true;

        private bool isImporting;
        private Vector2 scrollPosition;
        private string lastResult = string.Empty;

        [MenuItem("Tools/Game Data/Hero Progression Importer")]
        public static void OpenWindow()
        {
            HeroProgressionGoogleSheetImporterWindow window =
                GetWindow<HeroProgressionGoogleSheetImporterWindow>("Hero Progression Importer");

            window.minSize = new Vector2(620f, 430f);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Google Sheet Source", EditorStyles.boldLabel);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select", GUILayout.Width(80f)))
                {
                    SelectOutputFolder();
                }
            }

            deleteMissingNodes = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Replace the complete Nodes list",
                    "Bật: Nodes trong SO sẽ được thay hoàn toàn bằng data trên sheet. Đây là lựa chọn khuyến nghị."),
                deleteMissingNodes);

            EditorGUILayout.Space(10f);

            string csvUrl = BuildCsvUrl(googleSheetUrl);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Resolved CSV URL", csvUrl ?? string.Empty);
            }

            // if (!string.IsNullOrEmpty(urlError))
            // {
            //     EditorGUILayout.HelpBox(urlError, MessageType.Warning);
            // }

            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(isImporting))
            {
                if (GUILayout.Button("Download And Import", GUILayout.Height(36f)))
                {
                    ImportAsync();
                }
            }

            if (isImporting)
            {
                EditorGUILayout.HelpBox("Downloading and importing...", MessageType.Info);
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Expected Columns", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "HeroId, StartingTier, StartingStarInTier, NodeOrder, Tier, StarInTier, " +
                "ShardCostToNext, IsMaxNode, NextTier, NextStarInTier, HealthMultiplier, " +
                "AttackMultiplier, DefenseMultiplier, AccuracyMultiplier, AttackSpeedMultiplier, " +
                "AttackRangeMultiplier, MoveSpeedMultiplier, CritChanceMultiplier, CritDamageMultiplier",
                MessageType.None);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(lastResult, GUILayout.MinHeight(150f));

            EditorGUILayout.EndScrollView();
        }

        private async void ImportAsync()
        {
            string csvUrl = BuildCsvUrl(googleSheetUrl);

            if (!IsValidAssetFolder(outputFolder))
            {
                ShowResult("Output Folder phải nằm bên trong Assets/.", true);
                return;
            }

            isImporting = true;
            lastResult = "Downloading...";
            Repaint();

            try
            {
                string csvText = await DownloadTextAsync(csvUrl);
                ImportReport report = ImportCsv(csvText);

                ShowResult(report.BuildMessage(), report.ErrorCount > 0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowResult($"Import failed:\n{exception.Message}", true);
            }
            finally
            {
                isImporting = false;
                Repaint();
            }
        }

        private ImportReport ImportCsv(string csvText)
        {
            ImportReport report = new ImportReport();

            List<List<string>> csvRows = CsvUtility.Parse(csvText);
            if (csvRows.Count < 2)
            {
                report.AddError("CSV không có data hoặc chỉ có header.");
                return report;
            }

            HeaderMap headerMap = new HeaderMap(csvRows[0]);
            if (!ValidateRequiredHeaders(headerMap, report))
            {
                return report;
            }

            Dictionary<int, HeroProgressionImportGroup> groups = new();

            for (int rowIndex = 1; rowIndex < csvRows.Count; rowIndex++)
            {
                List<string> row = csvRows[rowIndex];
                int sheetRow = rowIndex + 1;

                if (IsEmptyRow(row))
                {
                    continue;
                }

                try
                {
                    ParsedProgressionRow parsed = ParseRow(row, headerMap, sheetRow);

                    if (!groups.TryGetValue(parsed.HeroId, out HeroProgressionImportGroup group))
                    {
                        group = new HeroProgressionImportGroup
                        {
                            HeroId = parsed.HeroId,
                            StartingTier = parsed.StartingTier,
                            StartingStarInTier = parsed.StartingStarInTier
                        };

                        groups.Add(parsed.HeroId, group);
                    }
                    else if (group.StartingTier != parsed.StartingTier ||
                             group.StartingStarInTier != parsed.StartingStarInTier)
                    {
                        throw new FormatException(
                            $"HeroId {parsed.HeroId} có StartingTier/StartingStarInTier không đồng nhất.");
                    }

                    group.Rows.Add(parsed);
                }
                catch (Exception exception)
                {
                    report.AddError($"Row {sheetRow}: {exception.Message}");
                }
            }

            if (report.ErrorCount > 0)
            {
                report.AddError("Không tạo hoặc cập nhật SO vì CSV còn lỗi.");
                return report;
            }

            if (groups.Count == 0)
            {
                report.AddError("Không tìm thấy progression row hợp lệ.");
                return report;
            }

            foreach (HeroProgressionImportGroup group in groups.Values)
            {
                ValidateGroup(group, report);
            }

            if (report.ErrorCount > 0)
            {
                report.AddError("Không tạo hoặc cập nhật SO vì validation còn lỗi.");
                return report;
            }

            EnsureAssetFolder(outputFolder);
            Dictionary<int, HeroProgressionConfigSO> existingAssets = FindExistingAssets(outputFolder, report);

            if (report.ErrorCount > 0)
            {
                return report;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (HeroProgressionImportGroup group in groups.Values.OrderBy(x => x.HeroId))
                {
                    bool created = false;

                    if (!existingAssets.TryGetValue(group.HeroId, out HeroProgressionConfigSO config) ||
                        config == null)
                    {
                        config = CreateInstance<HeroProgressionConfigSO>();
                        config.HeroId = group.HeroId;

                        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                            $"{outputFolder}/HeroProgressionConfig_{group.HeroId}.asset");

                        AssetDatabase.CreateAsset(config, assetPath);
                        existingAssets[group.HeroId] = config;
                        created = true;
                    }

                    Undo.RecordObject(config, "Import Hero Progression Config");

                    config.HeroId = group.HeroId;
                    config.StartingTier = group.StartingTier;
                    config.StartingStarInTier = group.StartingStarInTier;

                    List<HeroProgressionNode> importedNodes = group.Rows
                        .OrderBy(x => x.NodeOrder)
                        .Select(x => x.ToNode())
                        .ToList();

                    if (deleteMissingNodes || config.Nodes == null)
                    {
                        config.Nodes = importedNodes;
                    }
                    else
                    {
                        MergeNodes(config.Nodes, importedNodes);
                    }

                    EditorUtility.SetDirty(config);

                    if (created)
                    {
                        report.CreatedCount++;
                    }
                    else
                    {
                        report.UpdatedCount++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.ImportedHeroCount = groups.Count;
            report.ImportedNodeCount = groups.Values.Sum(x => x.Rows.Count);
            return report;
        }

        private static ParsedProgressionRow ParseRow(
            IReadOnlyList<string> row,
            HeaderMap headers,
            int sheetRow)
        {
            return new ParsedProgressionRow
            {
                SourceRow = sheetRow,
                HeroId = ParseInt(GetCell(row, headers, "HeroId"), "HeroId"),
                StartingTier = ParseTier(GetCell(row, headers, "StartingTier"), "StartingTier"),
                StartingStarInTier = ParseInt(
                    GetCell(row, headers, "StartingStarInTier"),
                    "StartingStarInTier"),
                NodeOrder = ParseInt(GetCell(row, headers, "NodeOrder"), "NodeOrder"),
                Tier = ParseTier(GetCell(row, headers, "Tier"), "Tier"),
                StarInTier = ParseInt(GetCell(row, headers, "StarInTier"), "StarInTier"),
                ShardCostToNext = ParseInt(
                    GetCell(row, headers, "ShardCostToNext"),
                    "ShardCostToNext"),
                IsMaxNode = ParseBool(GetCell(row, headers, "IsMaxNode"), "IsMaxNode"),
                NextTier = ParseTier(GetCell(row, headers, "NextTier"), "NextTier"),
                NextStarInTier = ParseInt(
                    GetCell(row, headers, "NextStarInTier"),
                    "NextStarInTier"),
                HealthMultiplier = ParseFloat(
                    GetCell(row, headers, "HealthMultiplier"),
                    "HealthMultiplier"),
                AttackMultiplier = ParseFloat(
                    GetCell(row, headers, "AttackMultiplier"),
                    "AttackMultiplier"),
                DefenseMultiplier = ParseFloat(
                    GetCell(row, headers, "DefenseMultiplier"),
                    "DefenseMultiplier"),
                AccuracyMultiplier = ParseFloat(
                    GetCell(row, headers, "AccuracyMultiplier"),
                    "AccuracyMultiplier"),
                AttackSpeedMultiplier = ParseFloat(
                    GetCell(row, headers, "AttackSpeedMultiplier"),
                    "AttackSpeedMultiplier"),
                AttackRangeMultiplier = ParseFloat(
                    GetCell(row, headers, "AttackRangeMultiplier"),
                    "AttackRangeMultiplier"),
                MoveSpeedMultiplier = ParseFloat(
                    GetCell(row, headers, "MoveSpeedMultiplier"),
                    "MoveSpeedMultiplier"),
                CritChanceMultiplier = ParseFloat(
                    GetCell(row, headers, "CritChanceMultiplier"),
                    "CritChanceMultiplier"),
                CritDamageMultiplier = ParseFloat(
                    GetCell(row, headers, "CritDamageMultiplier"),
                    "CritDamageMultiplier")
            };
        }

        private static void ValidateGroup(HeroProgressionImportGroup group, ImportReport report)
        {
            if (group.HeroId <= 0)
            {
                report.AddError($"HeroId {group.HeroId} không hợp lệ.");
            }

            if (group.StartingStarInTier < 0)
            {
                report.AddError($"HeroId {group.HeroId}: StartingStarInTier phải >= 0.");
            }

            List<ParsedProgressionRow> orderedRows = group.Rows
                .OrderBy(x => x.NodeOrder)
                .ToList();

            if (orderedRows.Count == 0)
            {
                report.AddError($"HeroId {group.HeroId} không có node.");
                return;
            }

            HashSet<int> nodeOrders = new();
            HashSet<(HeroProgressTier Tier, int Star)> nodeKeys = new();

            foreach (ParsedProgressionRow row in orderedRows)
            {
                if (!nodeOrders.Add(row.NodeOrder))
                {
                    report.AddError(
                        $"HeroId {group.HeroId}: NodeOrder {row.NodeOrder} bị trùng.");
                }

                if (!nodeKeys.Add((row.Tier, row.StarInTier)))
                {
                    report.AddError(
                        $"HeroId {group.HeroId}: node {row.Tier} {row.StarInTier} sao bị trùng.");
                }

                ValidateNonNegative(row, group.HeroId, report);
            }

            ParsedProgressionRow first = orderedRows[0];
            if (first.Tier != group.StartingTier ||
                first.StarInTier != group.StartingStarInTier)
            {
                report.AddError(
                    $"HeroId {group.HeroId}: node đầu tiên phải là " +
                    $"{group.StartingTier} {group.StartingStarInTier} sao, " +
                    $"nhưng sheet đang là {first.Tier} {first.StarInTier} sao.");
            }

            List<ParsedProgressionRow> maxNodes = orderedRows.Where(x => x.IsMaxNode).ToList();
            if (maxNodes.Count != 1)
            {
                report.AddError(
                    $"HeroId {group.HeroId}: phải có đúng 1 IsMaxNode, hiện có {maxNodes.Count}.");
            }
            else if (!ReferenceEquals(maxNodes[0], orderedRows[orderedRows.Count - 1]))
            {
                report.AddError(
                    $"HeroId {group.HeroId}: IsMaxNode phải là node cuối theo NodeOrder.");
            }

            for (int i = 0; i < orderedRows.Count - 1; i++)
            {
                ParsedProgressionRow current = orderedRows[i];
                ParsedProgressionRow next = orderedRows[i + 1];

                if (current.IsMaxNode)
                {
                    report.AddError(
                        $"HeroId {group.HeroId}: node row {current.SourceRow} là max nhưng vẫn còn node sau nó.");
                    continue;
                }

                if (current.NextTier != next.Tier ||
                    current.NextStarInTier != next.StarInTier)
                {
                    report.AddError(
                        $"HeroId {group.HeroId}: row {current.SourceRow} NextTier/NextStarInTier " +
                        $"không trỏ tới row tiếp theo ({next.Tier}, {next.StarInTier}).");
                }
            }
        }

        private static void ValidateNonNegative(
            ParsedProgressionRow row,
            int heroId,
            ImportReport report)
        {
            if (row.StarInTier < 0 ||
                row.ShardCostToNext < 0 ||
                row.NextStarInTier < 0 ||
                row.HealthMultiplier < 0f ||
                row.AttackMultiplier < 0f ||
                row.DefenseMultiplier < 0f ||
                row.AccuracyMultiplier < 0f ||
                row.AttackSpeedMultiplier < 0f ||
                row.AttackRangeMultiplier < 0f ||
                row.MoveSpeedMultiplier < 0f ||
                row.CritChanceMultiplier < 0f ||
                row.CritDamageMultiplier < 0f)
            {
                report.AddError(
                    $"HeroId {heroId}, row {row.SourceRow}: các giá trị number phải >= 0.");
            }
        }

        private static void MergeNodes(
            List<HeroProgressionNode> destination,
            IReadOnlyList<HeroProgressionNode> importedNodes)
        {
            foreach (HeroProgressionNode importedNode in importedNodes)
            {
                HeroProgressionNode existing = destination.Find(x =>
                    x.Tier == importedNode.Tier &&
                    x.StarInTier == importedNode.StarInTier);

                if (existing == null)
                {
                    destination.Add(importedNode);
                    continue;
                }

                CopyNode(importedNode, existing);
            }

            destination.Sort((a, b) =>
            {
                int tierComparison = a.Tier.CompareTo(b.Tier);
                return tierComparison != 0
                    ? tierComparison
                    : a.StarInTier.CompareTo(b.StarInTier);
            });
        }

        private static void CopyNode(HeroProgressionNode source, HeroProgressionNode destination)
        {
            destination.Tier = source.Tier;
            destination.StarInTier = source.StarInTier;
            destination.ShardCostToNext = source.ShardCostToNext;
            destination.IsMaxNode = source.IsMaxNode;
            destination.NextTier = source.NextTier;
            destination.NextStarInTier = source.NextStarInTier;
            destination.HealthMultiplier = source.HealthMultiplier;
            destination.AttackMultiplier = source.AttackMultiplier;
            destination.DefenseMultiplier = source.DefenseMultiplier;
            destination.AccuracyMultiplier = source.AccuracyMultiplier;
            destination.AttackSpeedMultiplier = source.AttackSpeedMultiplier;
            destination.AttackRangeMultiplier = source.AttackRangeMultiplier;
            destination.MoveSpeedMultiplier = source.MoveSpeedMultiplier;
            destination.CritChanceMultiplier = source.CritChanceMultiplier;
            destination.CritDamageMultiplier = source.CritDamageMultiplier;
        }

        private static Dictionary<int, HeroProgressionConfigSO> FindExistingAssets(
            string folder,
            ImportReport report)
        {
            Dictionary<int, HeroProgressionConfigSO> result = new();
            string[] guids = AssetDatabase.FindAssets("t:HeroProgressionConfigSO", new[] { folder });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                HeroProgressionConfigSO config =
                    AssetDatabase.LoadAssetAtPath<HeroProgressionConfigSO>(assetPath);

                if (config == null)
                {
                    continue;
                }

                if (result.ContainsKey(config.HeroId))
                {
                    report.AddError(
                        $"Có nhiều HeroProgressionConfigSO cùng HeroId {config.HeroId} trong {folder}.");
                    continue;
                }

                result.Add(config.HeroId, config);
            }

            return result;
        }

        private static bool ValidateRequiredHeaders(HeaderMap headers, ImportReport report)
        {
            string[] requiredHeaders =
            {
                "HeroId",
                "StartingTier",
                "StartingStarInTier",
                "NodeOrder",
                "Tier",
                "StarInTier",
                "ShardCostToNext",
                "IsMaxNode",
                "NextTier",
                "NextStarInTier",
                "HealthMultiplier",
                "AttackMultiplier",
                "DefenseMultiplier",
                "AccuracyMultiplier",
                "AttackSpeedMultiplier",
                "AttackRangeMultiplier",
                "MoveSpeedMultiplier",
                "CritChanceMultiplier",
                "CritDamageMultiplier"
            };

            foreach (string requiredHeader in requiredHeaders)
            {
                if (!headers.Contains(requiredHeader))
                {
                    report.AddError($"Missing column: {requiredHeader}");
                }
            }

            return report.ErrorCount == 0;
        }

        private static string GetCell(
            IReadOnlyList<string> row,
            HeaderMap headers,
            string header)
        {
            int index = headers.GetIndex(header);
            return index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        }

        private static int ParseInt(string raw, string fieldName)
        {
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }

            throw new FormatException($"{fieldName}='{raw}' không phải integer hợp lệ.");
        }

        private static float ParseFloat(string raw, string fieldName)
        {
            if (float.TryParse(
                    raw,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out float value))
            {
                return value;
            }

            string normalized = raw.Replace(',', '.');
            if (float.TryParse(
                    normalized,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value))
            {
                return value;
            }

            throw new FormatException($"{fieldName}='{raw}' không phải float hợp lệ.");
        }

        private static bool ParseBool(string raw, string fieldName)
        {
            if (bool.TryParse(raw, out bool value))
            {
                return value;
            }

            if (raw == "1") return true;
            if (raw == "0") return false;

            throw new FormatException($"{fieldName}='{raw}' phải là TRUE/FALSE hoặc 1/0.");
        }

        private static HeroProgressTier ParseTier(string raw, string fieldName)
        {
            if (Enum.TryParse(raw, true, out HeroProgressTier tier))
            {
                return tier;
            }

            string normalized = NormalizeHeader(raw);
            foreach (HeroProgressTier value in Enum.GetValues(typeof(HeroProgressTier)))
            {
                if (NormalizeHeader(value.ToString()) == normalized)
                {
                    return value;
                }
            }

            throw new FormatException($"{fieldName}='{raw}' không khớp HeroProgressTier.");
        }

        private static bool IsEmptyRow(IReadOnlyList<string> row)
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

        private static async Task<string> DownloadTextAsync(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 30;

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException(
                        $"Download failed: {request.responseCode} - {request.error}\nURL: {url}");
                }

                string text = request.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException("Google Sheet trả về nội dung rỗng.");
                }

                if (text.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                    text.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Google Sheet trả về HTML thay vì CSV. Hãy bật quyền 'Anyone with the link can view'.");
                }

                return text;
            }
        }

        private static string TryBuildCsvExportUrl(
            string rawUrl,
            out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                error = "Chưa nhập Google Sheet URL.";
                return null;
            }

            rawUrl = rawUrl.Trim();

            if (rawUrl.IndexOf("format=csv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawUrl.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return rawUrl;
            }

            const string marker = "/spreadsheets/d/";
            int markerIndex = rawUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                error = "Không tìm thấy Google Spreadsheet ID trong URL.";
                return null;
            }

            int idStart = markerIndex + marker.Length;
            int idEnd = rawUrl.IndexOf('/', idStart);
            if (idEnd < 0)
            {
                idEnd = rawUrl.IndexOf('?', idStart);
            }

            if (idEnd < 0)
            {
                idEnd = rawUrl.IndexOf('#', idStart);
            }

            if (idEnd < 0)
            {
                idEnd = rawUrl.Length;
            }

            string spreadsheetId = rawUrl.Substring(idStart, idEnd - idStart);
            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                error = "Spreadsheet ID rỗng.";
                return null;
            }

            string gid = ExtractQueryValue(rawUrl, "gid");
            if (!string.IsNullOrWhiteSpace(gid))
            {
                return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
            }

            // Không cần nhập Sheet GID riêng. Nếu URL không chứa gid, Google sẽ export tab đầu tiên.
            return $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv";
        }
        
        private static string BuildCsvUrl(string inputUrl)
        {
            if (string.IsNullOrWhiteSpace(inputUrl))
                throw new ArgumentException("Google Sheet URL is empty.");

            string url = inputUrl.Trim();

            // Link Publish to web đã là CSV trực tiếp.
            // Ví dụ:
            // https://docs.google.com/spreadsheets/d/e/2PACX-.../pub?gid=123&single=true&output=csv
            if (url.Contains("/spreadsheets/d/e/", StringComparison.OrdinalIgnoreCase) &&
                url.Contains("/pub", StringComparison.OrdinalIgnoreCase))
            {
                return EnsurePublishedCsvOutput(url);
            }

            // Link đã là link CSV thì giữ nguyên.
            if (url.Contains("output=csv", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("format=csv", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            // Link Google Sheet editor thông thường:
            // https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit#gid=123
            string spreadsheetId = ExtractSpreadsheetId(url);

            if (string.IsNullOrWhiteSpace(spreadsheetId))
            {
                throw new FormatException(
                    "Không tìm thấy Spreadsheet ID trong Google Sheet URL.");
            }

            string gid = ExtractGid(url);

            if (!string.IsNullOrWhiteSpace(gid))
            {
                return
                    $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export" +
                    $"?format=csv&gid={gid}";
            }

            return
                $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export" +
                "?format=csv";
        }
        
        private static string ExtractSpreadsheetId(string url)
        {
            const string marker = "/spreadsheets/d/";

            int startIndex = url.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase);

            if (startIndex < 0)
                return null;

            startIndex += marker.Length;

            // Không cho URL published /d/e/... chạy vào parser editor.
            if (url.Length > startIndex + 1 &&
                url[startIndex] == 'e' &&
                url[startIndex + 1] == '/')
            {
                return null;
            }

            int endIndex = url.IndexOf('/', startIndex);

            if (endIndex < 0)
            {
                endIndex = url.IndexOf('?', startIndex);
            }

            if (endIndex < 0)
            {
                endIndex = url.IndexOf('#', startIndex);
            }

            if (endIndex < 0)
            {
                endIndex = url.Length;
            }

            return url.Substring(startIndex, endIndex - startIndex);
        }

        private static string ExtractGid(string url)
        {
            const string gidKey = "gid=";

            int gidIndex = url.IndexOf(
                gidKey,
                StringComparison.OrdinalIgnoreCase);

            if (gidIndex < 0)
                return null;

            gidIndex += gidKey.Length;

            int endIndex = gidIndex;

            while (endIndex < url.Length &&
                   char.IsDigit(url[endIndex]))
            {
                endIndex++;
            }

            return endIndex > gidIndex
                ? url.Substring(gidIndex, endIndex - gidIndex)
                : null;
        }

        private static string EnsurePublishedCsvOutput(string url)
        {
            if (url.Contains("output=csv", StringComparison.OrdinalIgnoreCase))
                return url;

            char separator = url.Contains("?") ? '&' : '?';

            return $"{url}{separator}output=csv";
        }

        private static string ExtractQueryValue(string url, string key)
        {
            string token = key + "=";
            int index = url.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return string.Empty;
            }

            int valueStart = index + token.Length;
            int valueEnd = url.IndexOfAny(new[] { '&', '#', '?' }, valueStart);
            if (valueEnd < 0)
            {
                valueEnd = url.Length;
            }

            return Uri.UnescapeDataString(url.Substring(valueStart, valueEnd - valueStart));
        }

        private void SelectOutputFolder()
        {
            string absolutePath = EditorUtility.OpenFolderPanel(
                "Select Hero Progression Output Folder",
                Application.dataPath,
                string.Empty);

            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            string projectPath = Directory.GetParent(Application.dataPath)?.FullName
                ?.Replace('\\', '/');
            string normalized = absolutePath.Replace('\\', '/');

            if (string.IsNullOrEmpty(projectPath) || !normalized.StartsWith(projectPath))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Folder",
                    "Folder phải nằm trong Unity project.",
                    "OK");
                return;
            }
        }

        private static bool IsValidAssetFolder(string folder)
        {
            return !string.IsNullOrWhiteSpace(folder) &&
                   (folder == "Assets" || folder.StartsWith("Assets/", StringComparison.Ordinal));
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private void ShowResult(string message, bool isError)
        {
            lastResult = message;
            Repaint();

            if (isError)
            {
                Debug.LogError(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        private static string NormalizeHeader(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        private sealed class HeaderMap
        {
            private readonly Dictionary<string, int> indices = new();

            public HeaderMap(IReadOnlyList<string> headers)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    string normalized = NormalizeHeader(headers[i]);
                    if (!string.IsNullOrEmpty(normalized) && !indices.ContainsKey(normalized))
                    {
                        indices.Add(normalized, i);
                    }
                }
            }

            public bool Contains(string header)
            {
                return indices.ContainsKey(NormalizeHeader(header));
            }

            public int GetIndex(string header)
            {
                return indices.TryGetValue(NormalizeHeader(header), out int index) ? index : -1;
            }
        }

        private sealed class HeroProgressionImportGroup
        {
            public int HeroId;
            public HeroProgressTier StartingTier;
            public int StartingStarInTier;
            public readonly List<ParsedProgressionRow> Rows = new();
        }

        private sealed class ParsedProgressionRow
        {
            public int SourceRow;
            public int HeroId;
            public HeroProgressTier StartingTier;
            public int StartingStarInTier;
            public int NodeOrder;
            public HeroProgressTier Tier;
            public int StarInTier;
            public int ShardCostToNext;
            public bool IsMaxNode;
            public HeroProgressTier NextTier;
            public int NextStarInTier;
            public float HealthMultiplier;
            public float AttackMultiplier;
            public float DefenseMultiplier;
            public float AccuracyMultiplier;
            public float AttackSpeedMultiplier;
            public float AttackRangeMultiplier;
            public float MoveSpeedMultiplier;
            public float CritChanceMultiplier;
            public float CritDamageMultiplier;

            public HeroProgressionNode ToNode()
            {
                return new HeroProgressionNode
                {
                    Tier = Tier,
                    StarInTier = StarInTier,
                    ShardCostToNext = ShardCostToNext,
                    IsMaxNode = IsMaxNode,
                    NextTier = NextTier,
                    NextStarInTier = NextStarInTier,
                    HealthMultiplier = HealthMultiplier,
                    AttackMultiplier = AttackMultiplier,
                    DefenseMultiplier = DefenseMultiplier,
                    AccuracyMultiplier = AccuracyMultiplier,
                    AttackSpeedMultiplier = AttackSpeedMultiplier,
                    AttackRangeMultiplier = AttackRangeMultiplier,
                    MoveSpeedMultiplier = MoveSpeedMultiplier,
                    CritChanceMultiplier = CritChanceMultiplier,
                    CritDamageMultiplier = CritDamageMultiplier
                };
            }
        }

        private sealed class ImportReport
        {
            private readonly List<string> errors = new();

            public int ImportedHeroCount;
            public int ImportedNodeCount;
            public int CreatedCount;
            public int UpdatedCount;
            public int ErrorCount => errors.Count;

            public void AddError(string error)
            {
                errors.Add(error);
            }

            public string BuildMessage()
            {
                if (ErrorCount > 0)
                {
                    return "IMPORT FAILED\n\n" + string.Join("\n", errors.Select(x => "- " + x));
                }

                return
                    "IMPORT SUCCESS\n\n" +
                    $"Heroes imported: {ImportedHeroCount}\n" +
                    $"Nodes imported: {ImportedNodeCount}\n" +
                    $"Assets created: {CreatedCount}\n" +
                    $"Assets updated: {UpdatedCount}";
            }
        }

        private static class CsvUtility
        {
            public static List<List<string>> Parse(string csvText)
            {
                List<List<string>> rows = new();
                List<string> currentRow = new();
                StringBuilder currentField = new();
                bool insideQuotes = false;

                for (int i = 0; i < csvText.Length; i++)
                {
                    char character = csvText[i];

                    if (insideQuotes)
                    {
                        if (character == '"')
                        {
                            bool escapedQuote = i + 1 < csvText.Length && csvText[i + 1] == '"';
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
                            currentRow.Add(currentField.ToString());
                            currentField.Clear();
                            break;

                        case '\r':
                            if (i + 1 < csvText.Length && csvText[i + 1] == '\n')
                            {
                                i++;
                            }

                            FinishRow(rows, currentRow, currentField);
                            break;

                        case '\n':
                            FinishRow(rows, currentRow, currentField);
                            break;

                        default:
                            currentField.Append(character);
                            break;
                    }
                }

                if (insideQuotes)
                {
                    throw new FormatException("CSV có quoted field chưa đóng.");
                }

                if (currentField.Length > 0 || currentRow.Count > 0)
                {
                    FinishRow(rows, currentRow, currentField);
                }

                if (rows.Count > 0 && rows[0].Count > 0)
                {
                    rows[0][0] = rows[0][0].TrimStart('\uFEFF');
                }

                return rows;
            }

            private static void FinishRow(
                ICollection<List<string>> rows,
                List<string> currentRow,
                StringBuilder currentField)
            {
                currentRow.Add(currentField.ToString());
                currentField.Clear();
                rows.Add(new List<string>(currentRow));
                currentRow.Clear();
            }
        }
    }
}
#endif
