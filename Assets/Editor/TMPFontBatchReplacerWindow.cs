using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class TMPFontBatchReplacerWindow : EditorWindow
{
    private TMP_FontAsset newFont;
    private TMP_FontAsset onlyReplaceFromFont; // optional filter
    private bool includeInactive = true;
    private bool scanAllOpenScenes = true;

    private readonly List<TMP_Text> found = new List<TMP_Text>();
    private Vector2 scroll;

    [MenuItem("Tools/UI/TMP Font Batch Replacer")]
    public static void Open()
    {
        GetWindow<TMPFontBatchReplacerWindow>("TMP Font Replacer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scan TMP_Text and Replace Font", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New Font (TMP_FontAsset)", newFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Optional Filter", EditorStyles.miniBoldLabel);
        onlyReplaceFromFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Only replace if current font is", onlyReplaceFromFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space(8);
        includeInactive = EditorGUILayout.ToggleLeft("Include Inactive Objects", includeInactive);
        scanAllOpenScenes = EditorGUILayout.ToggleLeft("Scan All Open Scenes (Multi-Scene)", scanAllOpenScenes);

        EditorGUILayout.Space(10);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan", GUILayout.Height(28)))
            {
                Scan();
            }

            EditorGUI.BeginDisabledGroup(found.Count == 0);
            if (GUILayout.Button("Select Found", GUILayout.Height(28)))
            {
                Selection.objects = found.ConvertAll(t => t.gameObject).ToArray();
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.Space(6);

        EditorGUI.BeginDisabledGroup(newFont == null || found.Count == 0);
        if (GUILayout.Button("Apply Replace Font", GUILayout.Height(34)))
        {
            Apply();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);
        DrawResultList();
    }

    private void Scan()
    {
        found.Clear();

        if (scanAllOpenScenes)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                ScanScene(scene);
            }
        }
        else
        {
            ScanScene(SceneManager.GetActiveScene());
        }

        Repaint();
        Debug.Log($"[TMP Font Replacer] Found {found.Count} TMP_Text in scene(s).");
    }

    private void ScanScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            // includeInactive=true sẽ lấy cả object inactive
            var texts = root.GetComponentsInChildren<TMP_Text>(includeInactive);
            foreach (var t in texts)
            {
                if (t == null) continue;

                // filter theo font hiện tại (nếu có)
                if (onlyReplaceFromFont != null && t.font != onlyReplaceFromFont)
                    continue;

                found.Add(t);
            }
        }
    }

    private void Apply()
    {
        if (newFont == null) return;

        int changed = 0;
        var dirtyScenes = new HashSet<Scene>();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("TMP Font Batch Replace");

        foreach (var t in found)
        {
            if (t == null) continue;

            // Nếu set đúng font rồi thì bỏ qua
            if (t.font == newFont) continue;

            Undo.RecordObject(t, "Replace TMP Font");
            t.font = newFont;

            // Force refresh
            EditorUtility.SetDirty(t);

            changed++;

            // Mark scene dirty để Save
            var goScene = t.gameObject.scene;
            if (goScene.IsValid() && goScene.isLoaded)
                dirtyScenes.Add(goScene);
        }

        foreach (var s in dirtyScenes)
            EditorSceneManager.MarkSceneDirty(s);

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[TMP Font Replacer] Changed {changed} TMP_Text.");
        // Rescan để list phản ánh đúng trạng thái filter
        Scan();
    }

    private void DrawResultList()
    {
        EditorGUILayout.LabelField($"Found: {found.Count}", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(240));
        for (int i = 0; i < found.Count; i++)
        {
            var t = found[i];
            if (t == null) continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping", GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(t.gameObject);
                    Selection.activeObject = t.gameObject;
                }

                EditorGUILayout.ObjectField(t, typeof(TMP_Text), true);
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
