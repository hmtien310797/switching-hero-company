#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class TMPPrefabFontReplacerWindow : EditorWindow
{
    [Serializable]
    private sealed class PrefabScanResult
    {
        public string AssetPath;
        public int MatchedCount;
        public int TotalTMPCount;
    }

    private TMP_FontAsset targetFont;
    private TMP_FontAsset sourceFont;

    private DefaultAsset searchFolder;
    private bool onlyReplaceSourceFont;
    private bool onlyReplaceMissingFont;
    private bool includeTextMeshProUGUI = true;
    private bool includeTextMeshPro3D = true;

    private Vector2 scrollPosition;
    private readonly List<PrefabScanResult> scanResults = new();

    private int totalMatchedTexts;
    private int totalTMPTexts;

    [MenuItem("Tools/TMP/Replace Font In Prefabs")]
    private static void OpenWindow()
    {
        var window = GetWindow<TMPPrefabFontReplacerWindow>();
        window.titleContent = new GUIContent("TMP Font Replacer");
        window.minSize = new Vector2(620f, 500f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Search Scope", EditorStyles.boldLabel);
        searchFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent(
                "Search Folder",
                "Để trống để quét toàn bộ thư mục Assets. Có thể kéo một folder trong Project vào đây."),
            searchFolder,
            typeof(DefaultAsset),
            false);

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Font Replacement", EditorStyles.boldLabel);

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            new GUIContent("Target Font", "Font TMP sẽ được gắn vào các text tìm thấy."),
            targetFont,
            typeof(TMP_FontAsset),
            false);

        onlyReplaceSourceFont = EditorGUILayout.ToggleLeft(
            "Only replace a specific source font",
            onlyReplaceSourceFont);

        using (new EditorGUI.DisabledScope(!onlyReplaceSourceFont))
        {
            sourceFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                new GUIContent("Source Font", "Chỉ thay các text đang dùng font này."),
                sourceFont,
                typeof(TMP_FontAsset),
                false);
        }

        onlyReplaceMissingFont = EditorGUILayout.ToggleLeft(
            "Only replace texts with missing/null font",
            onlyReplaceMissingFont);

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("TMP Component Types", EditorStyles.boldLabel);
        includeTextMeshProUGUI = EditorGUILayout.ToggleLeft(
            "TextMeshProUGUI (Canvas UI)",
            includeTextMeshProUGUI);

        includeTextMeshPro3D = EditorGUILayout.ToggleLeft(
            "TextMeshPro (3D / World Space)",
            includeTextMeshPro3D);

        EditorGUILayout.Space(10);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan Prefabs", GUILayout.Height(32)))
            {
                ScanPrefabs();
            }

            using (new EditorGUI.DisabledScope(scanResults.Count == 0 || targetFont == null))
            {
                if (GUILayout.Button("Apply Font", GUILayout.Height(32)))
                {
                    ApplyFont();
                }
            }

            if (GUILayout.Button("Clear", GUILayout.Height(32), GUILayout.Width(90)))
            {
                scanResults.Clear();
                totalMatchedTexts = 0;
                totalTMPTexts = 0;
            }
        }

        EditorGUILayout.Space(10);
        DrawSummary();
        DrawResults();
    }

    private void DrawSummary()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Matched prefabs: {scanResults.Count}");
        EditorGUILayout.LabelField($"Matched TMP components: {totalMatchedTexts}");
        EditorGUILayout.LabelField($"Total TMP components scanned: {totalTMPTexts}");
        EditorGUILayout.EndVertical();
    }

    private void DrawResults()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Scan Results", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (scanResults.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Chưa có kết quả. Bấm Scan Prefabs để xem trước các prefab sẽ bị thay đổi.",
                MessageType.Info);
        }
        else
        {
            foreach (var result in scanResults)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        result.AssetPath,
                        GUILayout.ExpandWidth(true));

                    GUILayout.Label(
                        $"{result.MatchedCount}/{result.TotalTMPCount}",
                        GUILayout.Width(70));

                    if (GUILayout.Button("Ping", GUILayout.Width(50)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(result.AssetPath);
                        EditorGUIUtility.PingObject(asset);
                        Selection.activeObject = asset;
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void ScanPrefabs()
    {
        scanResults.Clear();
        totalMatchedTexts = 0;
        totalTMPTexts = 0;

        if (!ValidateSettings(requireTargetFont: false))
        {
            return;
        }

        string[] searchFolders = GetSearchFolders();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

                if (string.IsNullOrEmpty(assetPath) ||
                    assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                float progress = prefabGuids.Length == 0
                    ? 1f
                    : (float)i / prefabGuids.Length;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Scanning TMP Fonts",
                        assetPath,
                        progress))
                {
                    break;
                }

                GameObject prefabRoot = null;

                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

                    TMP_Text[] texts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
                    int matchedCount = texts.Count(IsMatch);

                    totalTMPTexts += texts.Length;

                    if (matchedCount <= 0)
                    {
                        continue;
                    }

                    totalMatchedTexts += matchedCount;

                    scanResults.Add(new PrefabScanResult
                    {
                        AssetPath = assetPath,
                        MatchedCount = matchedCount,
                        TotalTMPCount = texts.Length
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[TMPPrefabFontReplacer] Failed to scan prefab: {assetPath}\n{exception}");
                }
                finally
                {
                    if (prefabRoot != null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        scanResults.Sort((a, b) =>
            string.Compare(a.AssetPath, b.AssetPath, StringComparison.OrdinalIgnoreCase));

        Debug.Log(
            $"[TMPPrefabFontReplacer] Scan complete. " +
            $"Prefabs: {scanResults.Count}, Matched TMP: {totalMatchedTexts}, Total TMP: {totalTMPTexts}");
    }

    private void ApplyFont()
    {
        if (!ValidateSettings(requireTargetFont: true))
        {
            return;
        }

        if (scanResults.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "TMP Font Replacer",
                "Không có prefab nào trong danh sách kết quả.",
                "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Apply TMP Font",
            $"Sẽ thay font trong {scanResults.Count} prefab.\n\n" +
            $"Target font: {targetFont.name}\n\n" +
            "Nên commit hoặc backup project trước khi chạy.",
            "Apply",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        int changedPrefabCount = 0;
        int changedTextCount = 0;

        try
        {
            for (int i = 0; i < scanResults.Count; i++)
            {
                PrefabScanResult result = scanResults[i];

                float progress = scanResults.Count == 0
                    ? 1f
                    : (float)i / scanResults.Count;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Replacing TMP Fonts",
                        result.AssetPath,
                        progress))
                {
                    break;
                }

                GameObject prefabRoot = null;

                try
                {
                    prefabRoot = PrefabUtility.LoadPrefabContents(result.AssetPath);
                    TMP_Text[] texts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);

                    int changedInPrefab = 0;

                    foreach (TMP_Text text in texts)
                    {
                        if (!IsMatch(text))
                        {
                            continue;
                        }

                        text.font = targetFont;
                        EditorUtility.SetDirty(text);
                        changedInPrefab++;
                    }

                    if (changedInPrefab <= 0)
                    {
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, result.AssetPath);

                    changedPrefabCount++;
                    changedTextCount += changedInPrefab;
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[TMPPrefabFontReplacer] Failed to update prefab: {result.AssetPath}\n{exception}");
                }
                finally
                {
                    if (prefabRoot != null)
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        ScanPrefabs();

        EditorUtility.DisplayDialog(
            "TMP Font Replacer",
            $"Hoàn tất.\n\nChanged prefabs: {changedPrefabCount}\nChanged TMP components: {changedTextCount}",
            "OK");

        Debug.Log(
            $"[TMPPrefabFontReplacer] Apply complete. " +
            $"Changed prefabs: {changedPrefabCount}, Changed TMP: {changedTextCount}");
    }

    private bool IsMatch(TMP_Text text)
    {
        if (text == null)
        {
            return false;
        }

        bool supportedType =
            (includeTextMeshProUGUI && text is TextMeshProUGUI) ||
            (includeTextMeshPro3D && text is TextMeshPro);

        if (!supportedType)
        {
            return false;
        }

        if (onlyReplaceMissingFont && text.font != null)
        {
            return false;
        }

        if (onlyReplaceSourceFont && text.font != sourceFont)
        {
            return false;
        }

        if (!onlyReplaceMissingFont &&
            !onlyReplaceSourceFont &&
            targetFont != null &&
            text.font == targetFont)
        {
            return false;
        }

        return true;
    }

    private bool ValidateSettings(bool requireTargetFont)
    {
        if (!includeTextMeshProUGUI && !includeTextMeshPro3D)
        {
            EditorUtility.DisplayDialog(
                "TMP Font Replacer",
                "Hãy chọn ít nhất một loại TMP component.",
                "OK");
            return false;
        }

        if (requireTargetFont && targetFont == null)
        {
            EditorUtility.DisplayDialog(
                "TMP Font Replacer",
                "Target Font chưa được chọn.",
                "OK");
            return false;
        }

        if (onlyReplaceSourceFont && sourceFont == null)
        {
            EditorUtility.DisplayDialog(
                "TMP Font Replacer",
                "Source Font chưa được chọn.",
                "OK");
            return false;
        }

        if (onlyReplaceMissingFont && onlyReplaceSourceFont)
        {
            EditorUtility.DisplayDialog(
                "TMP Font Replacer",
                "Không thể bật đồng thời Source Font và Missing Font filter.",
                "OK");
            return false;
        }

        if (searchFolder != null)
        {
            string folderPath = AssetDatabase.GetAssetPath(searchFolder);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog(
                    "TMP Font Replacer",
                    "Search Folder phải là một folder hợp lệ trong Project.",
                    "OK");
                return false;
            }
        }

        return true;
    }

    private string[] GetSearchFolders()
    {
        if (searchFolder == null)
        {
            return new[] { "Assets" };
        }

        string folderPath = AssetDatabase.GetAssetPath(searchFolder);
        return new[] { folderPath };
    }
}
#endif
