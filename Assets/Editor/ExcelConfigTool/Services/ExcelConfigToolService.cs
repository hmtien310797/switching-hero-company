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

        public int GenerateScripts(
            string inputFolder,
            string outputScriptFolder,
            bool force = false
        )
        {
            var sheets = ReadChangedOrMissingConfigs(
                inputFolder,
                outputScriptFolder,
                null,
                force
            );

            if (sheets.Count == 0)
            {
                Debug.Log("No changed CSV files. Skip generate scripts.");
                return 0;
            }

            var changedScriptCount = ConfigCodeGenerator.GenerateScripts(outputScriptFolder, sheets);

            Debug.Log($"Generated scripts changed: {changedScriptCount}");
            return changedScriptCount;
        }

        public void GenerateOrUpdateAssets(
            string inputFolder,
            string outputAssetFolder,
            bool force = false
        )
        {
            var sheets = ReadChangedOrMissingConfigs(
                inputFolder,
                null,
                outputAssetFolder,
                force
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
            string outputAssetFolder,
            bool force
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

            var candidates = files
                .Select(file =>
                {
                    var normalizedPath = file.Replace("\\", "/");

                    return new
                    {
                        Path = normalizedPath,
                        Sheet = _csvRepository.ReadFile(normalizedPath),
                        LastWriteTimeUtc = File.GetLastWriteTimeUtc(file),
                    };
                })
                .Where(candidate => candidate.Sheet.Columns.Count > 0)
                .ToList();

            var selectedCandidates = candidates
                .GroupBy(candidate => candidate.Sheet.DatabaseClassName)
                .Select(group => group
                    .OrderByDescending(candidate => candidate.LastWriteTimeUtc)
                    .ThenBy(candidate => candidate.Path)
                    .First()
                )
                .ToList();

            foreach (var duplicateGroup in candidates
                         .GroupBy(candidate => candidate.Sheet.DatabaseClassName)
                         .Where(group => group.Count() > 1))
            {
                var selected = selectedCandidates.First(candidate =>
                    candidate.Sheet.DatabaseClassName == duplicateGroup.Key
                );

                Debug.LogWarning(
                    $"[ExcelConfigTool] Multiple CSV files map to '{duplicateGroup.Key}'. " +
                    $"Using newest file: {selected.Path}"
                );
            }

            foreach (var candidate in selectedCandidates)
            {
                var normalizedCsvPath = candidate.Path;
                var sheet = candidate.Sheet;

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

                if (force ||
                    hasCsvChanged ||
                    isMissingRowScript ||
                    isMissingDatabaseScript ||
                    isMissingAsset)
                {
                    Debug.Log(
                        $"Need process config: {normalizedCsvPath} | " +
                        $"Force={force}, " +
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
