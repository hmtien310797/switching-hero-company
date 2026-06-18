#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Immortal_Switch.Scripts.Skill;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class SkillDataGoogleSheetImporterWindow : EditorWindow
{
    private const string DefaultOutputFolder =
        "Assets/GameData/Skills/ClassSkills";

    private const string DefaultDealDamageActionEnum = "DealDamage";
    private const string DefaultDotDamageActionEnum = "ApplyDot";

    /// <summary>
    /// Hỗ trợ:
    /// phase_hit_1
    /// phase_hit_2
    /// phase_final_hit_1
    /// phase_final_hit_99
    /// </summary>
    private static readonly Regex PhaseHeaderRegex = new(
        @"^phase_(hit|final_hit)_(\d+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FlatDamageRegex = new(
        @"^flat_damage\s*\(\s*(?<value>-?\d+(?:\.\d+)?)\s*\)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DotDamageRegex = new(
        @"^dot_damage\s*\(\s*(?<parameters>.*?)\s*\)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [SerializeField]
    private string googleSheetUrl;

    [SerializeField]
    private TextAsset fallbackCsvFile;

    [SerializeField]
    private string outputFolder = DefaultOutputFolder;

    [SerializeField]
    private bool updateExistingAssets = true;

    [SerializeField]
    private string dealDamageActionEnum =
        DefaultDealDamageActionEnum;

    [SerializeField]
    private string dotDamageActionEnum =
        DefaultDotDamageActionEnum;

    private Vector2 scrollPosition;
    private UnityWebRequest activeRequest;
    private bool isDownloading;

    [MenuItem("Tools/Skill/Import Skill Data From Google Sheet")]
    private static void OpenWindow()
    {
        SkillDataGoogleSheetImporterWindow window =
            GetWindow<SkillDataGoogleSheetImporterWindow>();

        window.titleContent =
            new GUIContent("Skill Sheet Importer");

        window.minSize = new Vector2(570f, 500f);
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

        EditorGUILayout.LabelField(
            "Google Sheet Source",
            EditorStyles.boldLabel);

        googleSheetUrl = EditorGUILayout.TextField(
            "Full Google Sheet URL",
            googleSheetUrl);

        fallbackCsvFile =
            (TextAsset)EditorGUILayout.ObjectField(
                "Fallback CSV",
                fallbackCsvFile,
                typeof(TextAsset),
                false);

        EditorGUILayout.HelpBox(
            "Dán full link Google Sheet của đúng tab cần import.\n\n" +
            "Ví dụ:\n" +
            "https://docs.google.com/spreadsheets/d/xxx/edit#gid=123456\n\n" +
            "Google Sheet phải được share quyền Viewer cho người có link.",
            MessageType.Info);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Output",
            EditorStyles.boldLabel);

        outputFolder = EditorGUILayout.TextField(
            "Output Folder",
            outputFolder);

        updateExistingAssets = EditorGUILayout.Toggle(
            "Update Existing Assets",
            updateExistingAssets);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Action Enum Mapping",
            EditorStyles.boldLabel);

        dealDamageActionEnum = EditorGUILayout.TextField(
            "Deal Damage Enum",
            dealDamageActionEnum);

        dotDamageActionEnum = EditorGUILayout.TextField(
            "DOT Damage Enum",
            dotDamageActionEnum);

        EditorGUILayout.HelpBox(
            "Action hiện được hỗ trợ:\n" +
            "flat_damage(324)\n" +
            "dot_damage(DOT303/80/1/5/Burn)\n\n" +
            "Tool tự tìm phase_hit_N và phase_final_hit_N, " +
            "không giới hạn số N.",
            MessageType.None);

        EditorGUILayout.Space(12);

        bool hasSource =
            !string.IsNullOrWhiteSpace(googleSheetUrl) ||
            fallbackCsvFile != null;

        GUI.enabled = hasSource && !isDownloading;

        if (GUILayout.Button(
                "Validate Sheet",
                GUILayout.Height(34f)))
        {
            LoadSourceCsv(ValidateCsvText);
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button(
                "Download And Import",
                GUILayout.Height(42f)))
        {
            LoadSourceCsv(ImportCsvText);
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

    #region Source loading

    private void LoadSourceCsv(Action<string> onLoaded)
    {
        if (isDownloading)
            return;

        if (!string.IsNullOrWhiteSpace(googleSheetUrl))
        {
            string csvUrl;

            try
            {
                csvUrl = ConvertGoogleSheetUrlToCsvUrl(
                    googleSheetUrl);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Skill Sheet] Google Sheet URL không hợp lệ.\n" +
                    exception.Message);

                return;
            }

            DownloadCsv(csvUrl, onLoaded);
            return;
        }

        if (fallbackCsvFile != null)
        {
            onLoaded?.Invoke(fallbackCsvFile.text);
            return;
        }

        Debug.LogError(
            "[Skill Sheet] Chưa nhập Google Sheet URL hoặc CSV.");
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
                        "[Skill Sheet] Không thể tải Google Sheet.\n" +
                        $"Error: {activeRequest.error}\n" +
                        $"Response Code: {activeRequest.responseCode}\n" +
                        $"CSV URL: {csvUrl}");

                    return;
                }

                string content =
                    activeRequest.downloadHandler?.text;

                if (string.IsNullOrWhiteSpace(content))
                {
                    Debug.LogError(
                        "[Skill Sheet] Google Sheet trả về nội dung trống.");

                    return;
                }

                if (LooksLikeHtml(content))
                {
                    Debug.LogError(
                        "[Skill Sheet] Google trả về HTML thay vì CSV.\n" +
                        "Hãy kiểm tra quyền chia sẻ của Sheet. " +
                        "Sheet cần được đặt Anyone with the link → Viewer.");

                    return;
                }

                onLoaded?.Invoke(content);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Skill Sheet] Xử lý dữ liệu thất bại.\n{exception}");
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

    private static string ConvertGoogleSheetUrlToCsvUrl(
        string fullUrl)
    {
        if (string.IsNullOrWhiteSpace(fullUrl))
        {
            throw new ArgumentException(
                "Google Sheet URL đang trống.");
        }

        string trimmedUrl = fullUrl.Trim();

        if (!Uri.TryCreate(
                trimmedUrl,
                UriKind.Absolute,
                out Uri uri))
        {
            throw new ArgumentException(
                $"URL không hợp lệ: {trimmedUrl}");
        }

        if (!string.Equals(
                uri.Host,
                "docs.google.com",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "URL không thuộc docs.google.com.");
        }

        Match spreadsheetIdMatch = Regex.Match(
            trimmedUrl,
            @"/spreadsheets/d/(?<id>[^/?#]+)",
            RegexOptions.IgnoreCase);

        if (!spreadsheetIdMatch.Success)
        {
            throw new ArgumentException(
                "Không tìm thấy Spreadsheet ID trong URL.");
        }

        string spreadsheetId =
            spreadsheetIdMatch.Groups["id"].Value;

        string gid = ExtractGid(trimmedUrl);

        return
            $"https://docs.google.com/spreadsheets/d/" +
            $"{spreadsheetId}/export?format=csv&gid={gid}";
    }

    private static string ExtractGid(string fullUrl)
    {
        Match gidMatch = Regex.Match(
            fullUrl,
            @"(?:[?#&]gid=)(?<gid>\d+)",
            RegexOptions.IgnoreCase);

        return gidMatch.Success
            ? gidMatch.Groups["gid"].Value
            : "0";
    }

    #endregion

    #region Validate and import

    private void ValidateCsvText(string csvText)
    {
        try
        {
            SkillCsvDocument document =
                ParseDocument(csvText);

            int validSkillCount = 0;
            int hitPhaseCount = 0;
            int finalHitPhaseCount = 0;
            int totalActionCount = 0;

            foreach (SkillCsvRow row in document.Rows)
            {
                if (!TryParseSkillId(
                        document,
                        row,
                        out int skillId))
                {
                    continue;
                }

                ValidateRequiredRowData(document, row);

                List<ParsedPhase> phases =
                    ParsePhases(
                        document.Headers,
                        row,
                        skillId);

                validSkillCount++;

                foreach (ParsedPhase phase in phases)
                {
                    totalActionCount += phase.Actions.Count;

                    if (phase.Group == PhaseGroup.Hit)
                        hitPhaseCount++;
                    else
                        finalHitPhaseCount++;
                }
            }

            Debug.Log(
                "<color=green>[Skill Sheet Validation Success]</color>\n" +
                $"Skills: {validSkillCount}\n" +
                $"Hit Phases: {hitPhaseCount}\n" +
                $"Final Hit Phases: {finalHitPhaseCount}\n" +
                $"Actions: {totalActionCount}");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Skill Sheet] Validation thất bại.\n{exception}");
        }
    }

    private void ImportCsvText(string csvText)
    {
        SkillCsvDocument document;

        try
        {
            document = ParseDocument(csvText);
            EnsureAssetFolderExists(outputFolder);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[Skill Sheet] Không thể bắt đầu import.\n{exception}");

            return;
        }

        int createdCount = 0;
        int updatedCount = 0;
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
                    if (!TryParseSkillId(
                            document,
                            row,
                            out int skillId))
                    {
                        skippedCount++;
                        continue;
                    }

                    bool imported = TryImportRow(
                        document,
                        row,
                        skillId,
                        out bool created);

                    if (!imported)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (created)
                        createdCount++;
                    else
                        updatedCount++;
                }
                catch (Exception exception)
                {
                    failedCount++;

                    Debug.LogError(
                        $"[Skill Sheet] Import lỗi tại dòng " +
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
            "<color=green>[Skill Sheet Import Complete]</color>\n" +
            $"Created: {createdCount}\n" +
            $"Updated: {updatedCount}\n" +
            $"Skipped: {skippedCount}\n" +
            $"Failed: {failedCount}");
    }

    private bool TryImportRow(
        SkillCsvDocument document,
        SkillCsvRow row,
        int skillId,
        out bool created)
    {
        created = false;

        ValidateRequiredRowData(document, row);

        string className =
            GetRequiredCell(document, row, "class");

        string skillName =
            GetRequiredCell(document, row, "name");

        string tierText =
            GetRequiredCell(document, row, "tier");

        float cooldown =
            GetRequiredFloat(document, row, "cooldown(s)");

        float growthPercentPerLevel =
            GetRequiredFloat(document, row, "dmg_per_lv%");

        string normalizedClass =
            NormalizeKeyPart(className);

        string skillKey =
            $"skilldata_{normalizedClass}_{skillId}"
                .ToLowerInvariant();

        string iconSkillKey =
            $"icon_{skillKey}".ToLowerInvariant();

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

        List<ParsedPhase> parsedPhases =
            ParsePhases(
                document.Headers,
                row,
                skillId);

        SkillDataSO skillData =
            FindExistingSkill(skillId);

        if (skillData == null)
        {
            string assetPath =
                $"{outputFolder}/{skillKey}.asset";

            SkillDataSO assetAtExpectedPath =
                AssetDatabase.LoadAssetAtPath<SkillDataSO>(
                    assetPath);

            if (assetAtExpectedPath != null)
            {
                skillData = assetAtExpectedPath;
            }
            else
            {
                skillData =
                    CreateInstance<SkillDataSO>();

                AssetDatabase.CreateAsset(
                    skillData,
                    AssetDatabase.GenerateUniqueAssetPath(
                        assetPath));

                created = true;
            }
        }
        else if (!updateExistingAssets)
        {
            Debug.LogWarning(
                $"[Skill Sheet] SkillId {skillId} đã tồn tại. " +
                "Update Existing Assets đang tắt.");

            return false;
        }

        Undo.RecordObject(
            skillData,
            $"Import Skill {skillId}");

        ApplyGeneralData(
            skillData,
            skillId,
            skillKey,
            iconSkillKey,
            skillName,
            skillTier,
            cooldown,
            growthPercentPerLevel);

        ApplyParsedPhases(
            skillData,
            parsedPhases,
            row);

        EditorUtility.SetDirty(skillData);
        return true;
    }

    private static void ValidateRequiredRowData(
        SkillCsvDocument document,
        SkillCsvRow row)
    {
        GetRequiredCell(document, row, "skill_id");
        GetRequiredCell(document, row, "class");
        GetRequiredCell(document, row, "name");
        GetRequiredCell(document, row, "tier");
        GetRequiredFloat(document, row, "cooldown(s)");
        GetRequiredFloat(document, row, "dmg_per_lv%");
    }

    private static void ApplyGeneralData(
        SkillDataSO skillData,
        int skillId,
        string skillKey,
        string iconSkillKey,
        string skillName,
        TierSkill skillTier,
        float cooldown,
        float growthPercentPerLevel)
    {
        skillData.SkillId = skillId;
        skillData.SkillKey = skillKey;
        skillData.SkillName = skillName;
        skillData.IconSkillKey = iconSkillKey;

        skillData.OwnerType =
            SkillOwnerType.ClassSkill;

        skillData.SkillTier = skillTier;

        skillData.CastConfig ??=
            new SkillCastConfig();

        skillData.CastConfig.Cooldown =
            cooldown;

        /*
         * SkillDataSO hiện đang có cả:
         * - GrowthPercentPerLevel
         * - ClassSkillScaling.GrowthPercentPerLevel
         *
         * Runtime GetScaledClassSkillValue() dùng ClassSkillScaling,
         * nên importer set đồng thời cả hai để data không bị lệch.
         */
        skillData.GrowthPercentPerLevel =
            growthPercentPerLevel;

        skillData.ClassSkillScaling ??=
            new ClassSkillLevelScalingConfig();

        skillData.ClassSkillScaling.GrowthPercentPerLevel =
            growthPercentPerLevel;
    }

    #endregion

    #region Phase parsing

    private List<ParsedPhase> ParsePhases(
        IReadOnlyList<string> headers,
        SkillCsvRow row,
        int skillId)
    {
        Dictionary<PhaseGroup, List<PhaseColumn>> phaseColumns =
            CollectPhaseColumns(headers);

        List<ParsedPhase> phases = new();

        ParsedPhase hitPhase = ParseSinglePhase(
            PhaseGroup.Hit,
            phaseId: 1,
            eventName: "hit",
            phaseColumns[PhaseGroup.Hit],
            row,
            skillId);

        if (hitPhase.Actions.Count > 0)
            phases.Add(hitPhase);

        ParsedPhase finalHitPhase = ParseSinglePhase(
            PhaseGroup.FinalHit,
            phaseId: 2,
            eventName: "finalhit",
            phaseColumns[PhaseGroup.FinalHit],
            row,
            skillId);

        if (finalHitPhase.Actions.Count > 0)
            phases.Add(finalHitPhase);

        return phases;
    }

    private static Dictionary<PhaseGroup, List<PhaseColumn>>
        CollectPhaseColumns(IReadOnlyList<string> headers)
    {
        Dictionary<PhaseGroup, List<PhaseColumn>> result = new()
        {
            { PhaseGroup.Hit, new List<PhaseColumn>() },
            { PhaseGroup.FinalHit, new List<PhaseColumn>() }
        };

        for (int columnIndex = 0;
             columnIndex < headers.Count;
             columnIndex++)
        {
            string header =
                NormalizeHeader(headers[columnIndex]);

            Match match =
                PhaseHeaderRegex.Match(header);

            if (!match.Success)
                continue;

            string phaseName =
                match.Groups[1].Value;

            int actionOrder = int.Parse(
                match.Groups[2].Value,
                CultureInfo.InvariantCulture);

            PhaseGroup group =
                string.Equals(
                    phaseName,
                    "hit",
                    StringComparison.OrdinalIgnoreCase)
                    ? PhaseGroup.Hit
                    : PhaseGroup.FinalHit;

            result[group].Add(
                new PhaseColumn
                {
                    ColumnIndex = columnIndex,
                    ActionOrder = actionOrder,
                    HeaderName = headers[columnIndex]
                });
        }

        foreach (List<PhaseColumn> columns in result.Values)
        {
            columns.Sort(
                (left, right) =>
                    left.ActionOrder.CompareTo(
                        right.ActionOrder));
        }

        return result;
    }

    private ParsedPhase ParseSinglePhase(
        PhaseGroup group,
        int phaseId,
        string eventName,
        IReadOnlyList<PhaseColumn> columns,
        SkillCsvRow row,
        int skillId)
    {
        ParsedPhase result = new()
        {
            Group = group,
            PhaseId = phaseId,
            EventName = eventName
        };

        foreach (PhaseColumn column in columns)
        {
            if (column.ColumnIndex < 0 ||
                column.ColumnIndex >= row.Cells.Count)
            {
                continue;
            }

            string rawAction =
                row.Cells[column.ColumnIndex]?.Trim();

            /*
             * Ô trống hoặc 0:
             * không tạo action,
             * không thêm null vào list.
             */
            if (string.IsNullOrWhiteSpace(rawAction) ||
                string.Equals(
                    rawAction,
                    "0",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ParsedAction action;

            try
            {
                action = ParseAction(
                    rawAction,
                    skillId);
            }
            catch (Exception exception)
            {
                throw CreateRowException(
                    row,
                    $"Cột '{column.HeaderName}' có action lỗi: " +
                    $"'{rawAction}'. {exception.Message}");
            }

            action.Order = column.ActionOrder;
            result.Actions.Add(action);
        }

        result.Actions.Sort(
            (left, right) =>
                left.Order.CompareTo(right.Order));

        return result;
    }

    private static ParsedAction ParseAction(
        string rawAction,
        int skillId)
    {
        Match flatDamageMatch =
            FlatDamageRegex.Match(rawAction);

        if (flatDamageMatch.Success)
        {
            float damagePercent = ParseFloat(
                flatDamageMatch.Groups["value"].Value);

            return new ParsedAction
            {
                Kind = ParsedActionKind.FlatDamage,
                DamagePercent = damagePercent
            };
        }

        Match dotDamageMatch =
            DotDamageRegex.Match(rawAction);

        if (dotDamageMatch.Success)
        {
            string parameterText =
                dotDamageMatch.Groups["parameters"].Value;

            string[] parameters = parameterText
                .Split('/')
                .Select(parameter => parameter.Trim())
                .ToArray();

            if (parameters.Length != 5)
            {
                throw new FormatException(
                    "dot_damage phải có đúng 5 tham số:\n" +
                    "EffectId/TickDamageBonusPercent/" +
                    "TickInterval/Duration/DamageType");
            }

            string effectId = parameters[0];

            if (string.IsNullOrWhiteSpace(effectId))
            {
                throw new FormatException(
                    "EffectId không được để trống.");
            }

            string expectedEffectId =
                $"DOT{skillId}";

            if (!string.Equals(
                    effectId,
                    expectedEffectId,
                    StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    $"[Skill Sheet] SkillId {skillId}: " +
                    $"EffectId đang là '{effectId}', " +
                    $"giá trị dự kiến là '{expectedEffectId}'.");
            }

            return new ParsedAction
            {
                Kind = ParsedActionKind.DotDamage,
                EffectId = effectId,
                TickDamageBonusPercent =
                    ParseFloat(parameters[1]),
                TickInterval =
                    ParsePositiveFloat(
                        parameters[2],
                        "TickInterval"),
                Duration =
                    ParsePositiveFloat(
                        parameters[3],
                        "Duration"),
                DamageType = parameters[4]
            };
        }

        throw new FormatException(
            $"Action chưa được hỗ trợ: '{rawAction}'.");
    }

    #endregion

    #region Apply phases and actions

    private void ApplyParsedPhases(
        SkillDataSO skillData,
        IReadOnlyList<ParsedPhase> parsedPhases,
        SkillCsvRow row)
    {
        /*
         * Import lại phải replace BasePhases,
         * tránh cộng trùng action qua nhiều lần import.
         */
        List<SkillPhaseData> newPhases = new();

        foreach (ParsedPhase parsedPhase in parsedPhases)
        {
            SkillPhaseData phase = new()
            {
                PhaseId = parsedPhase.PhaseId,

                /*
                 * Đây là event được runtime object/Spine phát ra.
                 * SkillDataSO hiện hỗ trợ trigger theo event.
                 */
                TriggerType =
                    SkillPhaseTriggerType.SpineEvent,

                EventName =
                    parsedPhase.EventName,

                Delay = 0f,
                NormalizedTime = 0f,
                Actions = new List<SkillActionData>()
            };

            foreach (ParsedAction parsedAction in
                     parsedPhase.Actions)
            {
                SkillActionData action =
                    CreateSkillAction(
                        parsedAction,
                        row);

                if (action != null)
                    phase.Actions.Add(action);
            }

            if (phase.Actions.Count > 0)
                newPhases.Add(phase);
        }

        skillData.BasePhases = newPhases;
    }

    private SkillActionData CreateSkillAction(
        ParsedAction parsedAction,
        SkillCsvRow row)
    {
        return parsedAction.Kind switch
        {
            ParsedActionKind.FlatDamage =>
                CreateFlatDamageAction(
                    parsedAction,
                    row),

            ParsedActionKind.DotDamage =>
                CreateDotDamageAction(
                    parsedAction,
                    row),

            _ => throw CreateRowException(
                row,
                $"Action type chưa được hỗ trợ: " +
                $"{parsedAction.Kind}")
        };
    }

    private SkillActionData CreateFlatDamageAction(
        ParsedAction parsedAction,
        SkillCsvRow row)
    {
        SkillActionData action = new();

        SetEnumMemberByName(
            action,
            "ActionType",
            dealDamageActionEnum,
            row);

        action.ChancePercent = 100f;

        action.Damage ??=
            new SkillDamageData();

        action.Damage.SkillDamageBonusPercent =
            parsedAction.DamagePercent;

        action.Damage.CountAsSkillDamage = true;

        return action;
    }

    private SkillActionData CreateDotDamageAction(
        ParsedAction parsedAction,
        SkillCsvRow row)
    {
        SkillActionData action = new();

        SetEnumMemberByName(
            action,
            "ActionType",
            dotDamageActionEnum,
            row);

        action.ChancePercent = 100f;

        action.Dot ??=
            new SkillDotData();

        action.Dot.EffectId =
            parsedAction.EffectId;

        action.Dot.TickDamageBonusPercent =
            parsedAction.TickDamageBonusPercent;

        action.Dot.TickInterval =
            parsedAction.TickInterval;

        action.Dot.Duration =
            parsedAction.Duration;

        SetEnumMemberByName(
            action.Dot,
            "DamageType",
            parsedAction.DamageType,
            row);

        return action;
    }

    /// <summary>
    /// Dùng reflection cho enum để tool không bị phụ thuộc cứng
    /// vào tên enum type cụ thể.
    ///
    /// Ví dụ:
    /// SkillActionType.DealDamage
    /// DamageType.Burn
    /// </summary>
    private static void SetEnumMemberByName(
        object target,
        string memberName,
        string enumValueName,
        SkillCsvRow row)
    {
        if (target == null)
        {
            throw CreateRowException(
                row,
                $"Target null khi set '{memberName}'.");
        }

        Type targetType = target.GetType();

        FieldInfo field = targetType.GetField(
            memberName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (field != null)
        {
            object enumValue = ParseEnumValue(
                field.FieldType,
                enumValueName,
                targetType.Name,
                memberName,
                row);

            field.SetValue(target, enumValue);
            return;
        }

        PropertyInfo property = targetType.GetProperty(
            memberName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        if (property != null && property.CanWrite)
        {
            object enumValue = ParseEnumValue(
                property.PropertyType,
                enumValueName,
                targetType.Name,
                memberName,
                row);

            property.SetValue(target, enumValue);
            return;
        }

        throw CreateRowException(
            row,
            $"Không tìm thấy field/property " +
            $"'{targetType.Name}.{memberName}'.");
    }

    private static object ParseEnumValue(
        Type enumType,
        string enumValueName,
        string ownerTypeName,
        string memberName,
        SkillCsvRow row)
    {
        if (!enumType.IsEnum)
        {
            throw CreateRowException(
                row,
                $"{ownerTypeName}.{memberName} không phải enum.");
        }

        if (string.IsNullOrWhiteSpace(enumValueName))
        {
            throw CreateRowException(
                row,
                $"Tên enum cho {ownerTypeName}.{memberName} " +
                "đang trống.");
        }

        try
        {
            return Enum.Parse(
                enumType,
                enumValueName.Trim(),
                true);
        }
        catch
        {
            string validValues =
                string.Join(
                    ", ",
                    Enum.GetNames(enumType));

            throw CreateRowException(
                row,
                $"'{enumValueName}' không hợp lệ cho " +
                $"{enumType.Name}. Giá trị hợp lệ: {validValues}");
        }
    }

    #endregion

    #region CSV parsing

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

            /*
             * Header phase được duyệt trực tiếp theo index.
             * Dictionary này chỉ dùng cho field thông thường.
             */
            if (!document.FirstHeaderIndex.ContainsKey(
                    normalizedHeader))
            {
                document.FirstHeaderIndex.Add(
                    normalizedHeader,
                    i);
            }
        }

        for (int rowIndex = 1;
             rowIndex < records.Count;
             rowIndex++)
        {
            List<string> cells =
                records[rowIndex];

            if (cells.All(string.IsNullOrWhiteSpace))
                continue;

            /*
             * Bổ sung ô trống nếu dòng thiếu cột cuối.
             */
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
                    /*
                     * Bỏ qua CR.
                     * LF sẽ kết thúc record.
                     */
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

    private static void ValidateHeaders(
        IReadOnlyList<string> headers)
    {
        string[] requiredHeaders =
        {
            "skill_id",
            "class",
            "name",
            "tier",
            "cooldown(s)",
            "dmg_per_lv%"
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
                    $"Thiếu cột bắt buộc: '{requiredHeader}'.");
            }
        }

        bool hasAnyPhaseHeader = headers.Any(
            header =>
                PhaseHeaderRegex.IsMatch(
                    NormalizeHeader(header)));

        if (!hasAnyPhaseHeader)
        {
            throw new InvalidOperationException(
                "Không tìm thấy cột phase nào. " +
                "Header phải có dạng phase_hit_1 hoặc " +
                "phase_final_hit_1.");
        }
    }

    #endregion

    #region CSV cell helpers

    private static bool TryParseSkillId(
        SkillCsvDocument document,
        SkillCsvRow row,
        out int skillId)
    {
        skillId = 0;

        string rawValue =
            GetCell(
                document,
                row,
                "skill_id");

        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        if (int.TryParse(
                rawValue.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out skillId))
        {
            return true;
        }

        Debug.LogError(
            $"[Skill Sheet] Dòng {row.RowNumber}: " +
            $"skill_id '{rawValue}' không hợp lệ.");

        return false;
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

    private static float GetRequiredFloat(
        SkillCsvDocument document,
        SkillCsvRow row,
        string header)
    {
        string rawValue =
            GetRequiredCell(
                document,
                row,
                header);

        if (!float.TryParse(
                rawValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float value))
        {
            throw CreateRowException(
                row,
                $"Cột '{header}' có số không hợp lệ: " +
                $"'{rawValue}'.");
        }

        return value;
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

    private static float ParseFloat(string rawValue)
    {
        if (!float.TryParse(
                rawValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float result))
        {
            throw new FormatException(
                $"Giá trị số không hợp lệ: '{rawValue}'.");
        }

        return result;
    }

    private static float ParsePositiveFloat(
        string rawValue,
        string parameterName)
    {
        float result = ParseFloat(rawValue);

        if (result <= 0f)
        {
            throw new FormatException(
                $"{parameterName} phải lớn hơn 0, " +
                $"nhưng đang là {result}.");
        }

        return result;
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

    #endregion

    #region Asset helpers

    private static SkillDataSO FindExistingSkill(
        int skillId)
    {
        string[] guids =
            AssetDatabase.FindAssets("t:SkillDataSO");

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

    #endregion

    #region Internal data

    private enum PhaseGroup
    {
        Hit,
        FinalHit
    }

    private enum ParsedActionKind
    {
        FlatDamage,
        DotDamage
    }

    private sealed class PhaseColumn
    {
        public int ColumnIndex;
        public int ActionOrder;
        public string HeaderName;
    }

    private sealed class ParsedPhase
    {
        public PhaseGroup Group;
        public int PhaseId;
        public string EventName;
        public readonly List<ParsedAction> Actions = new();
    }

    private sealed class ParsedAction
    {
        public ParsedActionKind Kind;
        public int Order;

        public float DamagePercent;

        public string EffectId;
        public float TickDamageBonusPercent;
        public float TickInterval;
        public float Duration;
        public string DamageType;
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

    #endregion
}

#endif