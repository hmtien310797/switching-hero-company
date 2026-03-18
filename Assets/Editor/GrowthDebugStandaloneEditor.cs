#if UNITY_EDITOR
using Immortal_Switch.Scripts.EditorTools;
using Immortal_Switch.Scripts.GrowthSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(GrowthDebugStandalone))]
    public class GrowthDebugStandaloneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var tool = (GrowthDebugStandalone)target;

            GUILayout.Space(10);
            GUILayout.Label("=== DEBUG ACTIONS ===", EditorStyles.boldLabel);

            if (GUILayout.Button("Rebuild"))
                tool.Rebuild();

            GUILayout.Space(5);

            GUILayout.Label("Upgrade", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("x1")) tool.UpgradeX1();
            if (GUILayout.Button("x10")) tool.UpgradeX10();
            if (GUILayout.Button("x100")) tool.UpgradeX100();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Custom Upgrade"))
                tool.UpgradeCustom();

            GUILayout.Space(5);

            if (GUILayout.Button("Unlock Tier"))
                tool.UnlockTier();

            GUILayout.Space(5);

            if (GUILayout.Button("Log Selected"))
                tool.LogSelected();

            if (GUILayout.Button("Log All"))
                tool.LogAll();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Gold 1M")) tool.Gold1M();
            if (GUILayout.Button("Gold MAX")) tool.GoldMax();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("RESET ALL"))
                tool.ResetAll();
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif