using Immortal_Switch.Hero;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(HeroProgressionRuntimeBridge))]
    public class HeroProgressionRuntimeBridgeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var bridge = (HeroProgressionRuntimeBridge)target;

            GUILayout.Space(10);
            GUILayout.Label("Hero Progression Debug", EditorStyles.boldLabel);

            if (GUILayout.Button("Refresh From Progression"))
            {
                bridge.RefreshFromProgression();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 Shard"))
                bridge.AddShardDebug(1);
            if (GUILayout.Button("+5 Shard"))
                bridge.AddShardDebug(5);
            if (GUILayout.Button("+10 Shard"))
                bridge.AddShardDebug(10);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Upgrade Hero"))
            {
                bridge.TryUpgradeDebug();
            }

            GUILayout.Space(8);
            EditorGUILayout.HelpBox(bridge.GetDebugInfo(), MessageType.Info);

            if (GUI.changed)
                EditorUtility.SetDirty(bridge);
        }
    }
}