#if UNITY_EDITOR
using System;
using System.Linq;
using Immortal_Switch.Scripts;
using Immortal_Switch.Scripts.EditorTools;
using Immortal_Switch.Scripts.Level.Pattern;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(CreepSpawnPatternCollectionSO))]
    public class CreepSpawnPatternCollectionSOEditor : UnityEditor.Editor
    {
        // field chỉ dùng trong editor (không serialize vào asset)
        private TextAsset _sourceText;

        public override void OnInspectorGUI()
        {
            // Vẽ inspector mặc định trước (để vẫn edit được ListSpawnPattern nếu muốn)
            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Import Tool", EditorStyles.boldLabel);

            _sourceText = (TextAsset)EditorGUILayout.ObjectField(
                "Source TextAsset",
                _sourceText,
                typeof(TextAsset),
                false
            );

            using (new EditorGUI.DisabledScope(_sourceText == null))
            {
                if (GUILayout.Button("Import From TextAsset", GUILayout.Height(28)))
                {
                    var so = (CreepSpawnPatternCollectionSO)target;
                    ImportInto(so, _sourceText);
                }
            }

            EditorGUILayout.HelpBox(
                "Text format: columns (id, pattern). Pattern uses ';' like 1001;1002;1003.\n" +
                "Supports TSV/CSV/space-separated columns. Header line is optional.",
                MessageType.Info
            );
        }

        private static void ImportInto(CreepSpawnPatternCollectionSO so, TextAsset textAsset)
        {
            if (so == null || textAsset == null) return;

            try
            {
                var rows = SpawnPatternParseUtil.ParseRows(textAsset.text);

                if (rows.Count == 0)
                {
                    Debug.LogError("No valid rows found in TextAsset.");
                    return;
                }

                // Optional: warn duplicate ids (Parse util có thể đã sort)
                var dup = rows.GroupBy(r => r.id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
                if (dup.Length > 0)
                    Debug.LogWarning($"Duplicate ids found: {string.Join(", ", dup)} (later ones still included).");

                Undo.RecordObject(so, "Import Spawn Patterns");
                so.ListSpawnPattern = rows
                    .Select(r => new CreepSpawnPattern { Id = r.id, ListEnemyId = r.enemyIds })
                    .ToArray();

                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();

                Debug.Log($"Imported {so.ListSpawnPattern.Length} patterns into: {AssetDatabase.GetAssetPath(so)}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
#endif