using UnityEditor;
using UnityEngine;

/// <summary>
/// Scaffolds the gitignored Staging/Prod NakamaServerConfig assets so real host/keys never need
/// to be typed into a committed file. Run once per machine, then fill in values via Inspector.
/// </summary>
public static class ServerConfigEditorTools
{
    private const string FolderPath = "Assets/Resources/ServerConfig";

    [MenuItem("Tools/Server Config/Create Staging Config")]
    private static void CreateStaging() => CreateIfMissing("Staging");

    [MenuItem("Tools/Server Config/Create Prod Config")]
    private static void CreateProd() => CreateIfMissing("Prod");

    private static void CreateIfMissing(string name)
    {
        var path = $"{FolderPath}/{name}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<NakamaServerConfig>(path);
        if (existing != null)
        {
            Debug.Log($"[ServerConfigEditorTools] {path} already exists.");
            Selection.activeObject = existing;
            EditorGUIUtility.PingObject(existing);
            return;
        }

        var config = ScriptableObject.CreateInstance<NakamaServerConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ServerConfigEditorTools] Created {path} — fill in real host/serverKey/httpKey " +
                   "via Inspector. This file is gitignored and never gets committed.");
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }
}
