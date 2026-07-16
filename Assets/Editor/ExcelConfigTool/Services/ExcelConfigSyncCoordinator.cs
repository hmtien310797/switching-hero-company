using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor.ExcelConfigTool.Services
{
    public static class ExcelConfigSyncCoordinator
    {
        private const string PENDING_KEY = "ExcelConfigTool_PendingAssetGeneration";
        private const string INPUT_FOLDER_KEY = "ExcelConfigTool_PendingInputFolder";
        private const string OUTPUT_ASSET_FOLDER_KEY = "ExcelConfigTool_PendingOutputAssetFolder";

        public static void ScheduleAssetsAfterScriptReload(
            string inputFolder,
            string outputAssetFolder
        )
        {
            SessionState.SetString(INPUT_FOLDER_KEY, inputFolder);
            SessionState.SetString(OUTPUT_ASSET_FOLDER_KEY, outputAssetFolder);
            SessionState.SetBool(PENDING_KEY, true);

            Debug.Log(
                "[ExcelConfigTool] Generated scripts changed. " +
                "Assets will be regenerated after Unity reloads the scripts."
            );

            // Generate assets on the next editor tick if the generated files
            // end up unchanged and Unity therefore skips the script reload.
            QueueAssetGeneration();
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!SessionState.GetBool(PENDING_KEY, false))
            {
                return;
            }

            QueueAssetGeneration();
        }

        private static void ResumeAssetGeneration()
        {
            if (!SessionState.GetBool(PENDING_KEY, false))
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                QueueAssetGeneration();
                return;
            }

            var inputFolder = SessionState.GetString(INPUT_FOLDER_KEY, string.Empty);
            var outputAssetFolder = SessionState.GetString(OUTPUT_ASSET_FOLDER_KEY, string.Empty);

            SessionState.SetBool(PENDING_KEY, false);

            if (string.IsNullOrWhiteSpace(inputFolder) ||
                string.IsNullOrWhiteSpace(outputAssetFolder))
            {
                Debug.LogError("[ExcelConfigTool] Cannot resume asset generation: folder setting is missing.");
                return;
            }

            try
            {
                var service = new ExcelConfigToolService();
                service.GenerateOrUpdateAssets(inputFolder, outputAssetFolder, true);
                EditorUtility.DisplayDialog("Excel Config Tool", "Sync completed.", "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ExcelConfigTool] Asset generation failed after script reload:\n{e}");

                EditorUtility.DisplayDialog(
                    "Excel Config Tool",
                    "Asset generation failed. Check the Console for details.",
                    "OK"
                );
            }
        }

        private static void QueueAssetGeneration()
        {
            EditorApplication.delayCall -= ResumeAssetGeneration;
            EditorApplication.delayCall += ResumeAssetGeneration;
        }
    }
}
