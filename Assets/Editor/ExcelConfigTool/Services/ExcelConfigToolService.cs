using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.ExcelConfigTool.Models;
using Editor.ExcelConfigTool.Repositories;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelConfigTool.Services
{
    public class ExcelConfigToolService
    {
        private readonly CsvConfigRepository _csvRepository = new();
        private readonly ConfigHashCacheService _hashCacheService = new();

        public List<ConfigSheetInfo> ReadExcelFolder(string inputFolder)
        {
            return _csvRepository.ReadFolder(inputFolder);
        }

        public void GenerateScripts(
            string inputFolder,
            string outputScriptFolder
        )
        {
            var sheets = ReadChangedOrMissingConfigs(
                inputFolder,
                outputScriptFolder,
                null
            );

            if (sheets.Count == 0)
            {
                Debug.Log("No changed CSV files. Skip generate scripts.");
                return;
            }

            ConfigCodeGenerator.GenerateScripts(outputScriptFolder, sheets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Generated scripts count: {sheets.Count * 2}");
        }

        public void GenerateOrUpdateAssets(
            string inputFolder,
            string outputAssetFolder
        )
        {
            var sheets = ReadChangedOrMissingConfigs(
                inputFolder,
                null,
                outputAssetFolder
            );

            if (sheets.Count == 0)
            {
                Debug.Log("No changed CSV files. Skip generate/update assets.");
                return;
            }

            Directory.CreateDirectory(outputAssetFolder);

            foreach (var sheet in sheets)
            {
                var assetPath = Path.Combine(
                        outputAssetFolder,
                        $"{sheet.DatabaseClassName}.asset"
                    )
                    .Replace("\\", "/");

                ScriptableObjectAssetWriter.CreateOrUpdate(
                    assetPath,
                    ConfigCodeGenerator.GetGeneratedNamespace(),
                    sheet
                );

                _hashCacheService.SaveHash(sheet.ExcelFilePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Generated/Updated assets count: {sheets.Count}");
        }

        private List<ConfigSheetInfo> ReadChangedOrMissingConfigs(
            string inputFolder,
            string outputScriptFolder,
            string outputAssetFolder
        )
        {
            var result = new List<ConfigSheetInfo>();

            if (!Directory.Exists(inputFolder))
            {
                return result;
            }

            var files = Directory
                .GetFiles(inputFolder, "*.csv", SearchOption.AllDirectories)
                .Where(v => !Path.GetFileName(v).StartsWith("~$"))
                .ToList();

            foreach (var file in files)
            {
                var normalizedCsvPath = file.Replace("\\", "/");
                var sheet = _csvRepository.ReadFile(normalizedCsvPath);

                if (sheet.Columns.Count == 0)
                {
                    continue;
                }

                var hasCsvChanged = _hashCacheService.HasChanged(normalizedCsvPath);

                var rowScriptPath = !string.IsNullOrWhiteSpace(outputScriptFolder)
                    ? Path.Combine(outputScriptFolder, $"{sheet.RowClassName}.cs").Replace("\\", "/")
                    : null;

                var databaseScriptPath = !string.IsNullOrWhiteSpace(outputScriptFolder)
                    ? Path.Combine(outputScriptFolder, $"{sheet.DatabaseClassName}.cs").Replace("\\", "/")
                    : null;

                var assetPath = !string.IsNullOrWhiteSpace(outputAssetFolder)
                    ? Path.Combine(outputAssetFolder, $"{sheet.DatabaseClassName}.asset").Replace("\\", "/")
                    : null;

                var isMissingRowScript =
                    !string.IsNullOrWhiteSpace(rowScriptPath) &&
                    !File.Exists(rowScriptPath);

                var isMissingDatabaseScript =
                    !string.IsNullOrWhiteSpace(databaseScriptPath) &&
                    !File.Exists(databaseScriptPath);

                var isMissingAsset =
                    !string.IsNullOrWhiteSpace(assetPath) &&
                    !File.Exists(assetPath);

                if (hasCsvChanged ||
                    isMissingRowScript ||
                    isMissingDatabaseScript ||
                    isMissingAsset)
                {
                    Debug.Log(
                        $"Need process config: {normalizedCsvPath} | " +
                        $"CsvChanged={hasCsvChanged}, " +
                        $"MissingRowScript={isMissingRowScript}, " +
                        $"MissingDatabaseScript={isMissingDatabaseScript}, " +
                        $"MissingAsset={isMissingAsset}"
                    );

                    result.Add(sheet);
                }
                else
                {
                    Debug.Log($"Skip unchanged CSV config: {normalizedCsvPath}");
                }
            }

            return result;
        }
    }
}