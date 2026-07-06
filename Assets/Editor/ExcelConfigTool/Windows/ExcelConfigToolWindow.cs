using Editor.ExcelConfigTool.Services;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelConfigTool.Windows
{
    public class ExcelConfigToolWindow : EditorWindow
    {
        private ExcelConfigToolService _service;
        private string _inputFolder = "Assets/Immortal Switch/GameConfigs/Excel";
        private string _outputScriptFolder = "Assets/Immortal Switch/GameConfigs/Generated/Scripts";
        private string _outputAssetFolder = "Assets/Immortal Switch/GameConfigs/Generated/Assets";

        [MenuItem("Tools/Excel Config Tool")]
        public static void Open()
        {
            GetWindow<ExcelConfigToolWindow>("Excel Config Tool");
        }

        private void OnEnable()
        {
            _service = new ExcelConfigToolService();
        }

        private void OnGUI()
        {
            GUILayout.Label("Excel Config Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            DrawFolderPicker("Input Excel Folder", ref _inputFolder);
            DrawFolderPicker("Output Script Folder", ref _outputScriptFolder);
            DrawFolderPicker("Output Asset Folder", ref _outputAssetFolder);

            EditorGUILayout.Space(12);

            if (GUILayout.Button("1. Generate Scripts From CSV", GUILayout.Height(32)))
            {
                _service.GenerateScripts(
                    _inputFolder,
                    _outputScriptFolder
                );
            }

            EditorGUILayout.HelpBox(
                "Sau khi Generate Scripts, chờ Unity compile xong rồi bấm bước 2.",
                MessageType.Info
            );

            if (GUILayout.Button("2. Generate Or Update ScriptableObjects", GUILayout.Height(32)))
            {
                _service.GenerateOrUpdateAssets(
                    _inputFolder,
                    _outputAssetFolder
                );
            }
        }

        private static void DrawFolderPicker(string label, ref string folder)
        {
            EditorGUILayout.BeginHorizontal();

            folder = EditorGUILayout.TextField(label, folder);

            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                var selected = EditorUtility.OpenFolderPanel(
                    label,
                    Application.dataPath,
                    ""
                );

                if (!string.IsNullOrWhiteSpace(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        folder = "Assets" + selected[Application.dataPath.Length..];
                    }
                    else
                    {
                        folder = selected;
                    }

                    folder = folder.Replace("\\", "/");
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}