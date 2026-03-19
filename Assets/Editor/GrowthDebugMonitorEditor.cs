#if UNITY_EDITOR
using Immortal_Switch.Scripts.GrowthSystem;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(GrowthDebugMonitor))]
    public class GrowthDebugMonitorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            var monitor = (GrowthDebugMonitor)target;

            if (GUILayout.Button("🔄 Refresh Snapshot"))
            {
                monitor.Refresh();
                EditorUtility.SetDirty(monitor);
            }
        }
    }
}
#endif