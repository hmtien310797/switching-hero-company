#if UNITY_EDITOR
using Immortal_Switch.Scripts.GrowthSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(GrowthManager))]
    public class GrowthManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            GUILayout.Label("Growth Debug", EditorStyles.boldLabel);

            var manager = (GrowthManager)target;

            if (GUILayout.Button("Add 10000 Gold"))
            {
                manager.DebugAddGold();
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Unlock Next Tier"))
            {
                manager.DebugUnlockNextTier();
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Save"))
            {
                manager.DebugSave();
                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Load"))
            {
                manager.DebugLoad();
                EditorUtility.SetDirty(manager);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("CLEAR ALL DATA"))
            {
                manager.DebugClearData();
                EditorUtility.SetDirty(manager);
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif