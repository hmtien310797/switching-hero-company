using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Editor.LocalizationSyncTool.Models;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEngine.Localization.Tables;

namespace Editor.LocalizationSyncTool.Services
{
    public class LocalizationSyncImportResult
    {
        public int ImportedFileCount { get; set; }
        public int KeyCount { get; set; }
        public int GeneratedConstantCount { get; set; }
        public List<string> LocaleCodes { get; } = new();
        public List<LocalizationDuplicateKey> DuplicateKeys { get; } = new();
    }

    public class LocalizationDuplicateKey
    {
        public string Key { get; set; }
        public int Count { get; set; }
    }

    public static class LocalizationSyncImporter
    {
        public static LocalizationSyncImportResult Import(
            IReadOnlyList<string> csvPaths,
            string tableCollectionName,
            string keyColumnName,
            IReadOnlyList<LocalizationSyncLocaleMapping> localeMappings,
            bool removeMissingEntries
        )
        {
            if (csvPaths == null ||
                csvPaths.Count == 0)
            {
                throw new ArgumentException("Không có file CSV để import.", nameof(csvPaths));
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(tableCollectionName);

            if (collection == null)
            {
                throw new InvalidOperationException(
                    $"Không tìm thấy String Table Collection '{tableCollectionName}'."
                );
            }

            var localeCodes = collection.StringTables
                .Where(table => table != null)
                .Select(table => table.LocaleIdentifier.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (localeCodes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"String Table Collection '{tableCollectionName}' chưa có locale table."
                );
            }

            var importedFileCount = 0;
            var existingCsvPaths = csvPaths.Where(File.Exists).ToList();
            var duplicateKeys = FindDuplicateKeys(existingCsvPaths, keyColumnName);

            foreach (var csvPath in existingCsvPaths)
            {
                var headers = ReadHeaders(csvPath);

                var resolvedKeyColumnName = ResolveHeader(
                    headers,
                    string.IsNullOrWhiteSpace(keyColumnName) ? "Key" : keyColumnName.Trim()
                );

                var mappings = CreateMappings(
                    resolvedKeyColumnName,
                    localeCodes,
                    localeMappings,
                    headers
                );

                using var reader = new StreamReader(
                    csvPath,
                    Encoding.UTF8,
                    true
                );

                Csv.ImportInto(
                    reader,
                    collection,
                    mappings,
                    true,
                    null,
                    removeMissingEntries
                );

                importedFileCount++;
            }

            if (importedFileCount == 0)
            {
                throw new FileNotFoundException("Không tìm thấy file CSV đã download để import.");
            }

            EditorUtility.SetDirty(collection.SharedData);

            foreach (StringTable table in collection.StringTables)
            {
                if (table != null)
                {
                    EditorUtility.SetDirty(table);
                }
            }

            var generatedConstantCount = LocalizationConstantsGenerator.Generate(
                collection.SharedData.Entries.Select(entry => entry.Key)
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var result = new LocalizationSyncImportResult
            {
                ImportedFileCount = importedFileCount,
                KeyCount = collection.SharedData.Entries.Count,
                GeneratedConstantCount = generatedConstantCount,
            };

            result.LocaleCodes.AddRange(localeCodes);
            result.DuplicateKeys.AddRange(duplicateKeys);
            return result;
        }

        private static List<LocalizationDuplicateKey> FindDuplicateKeys(
            IReadOnlyList<string> csvPaths,
            string configuredKeyColumnName
        )
        {
            var keyCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            var requestedKeyColumn = string.IsNullOrWhiteSpace(configuredKeyColumnName)
                ? "Key"
                : configuredKeyColumnName.Trim();

            foreach (var csvPath in csvPaths)
            {
                var rows = ReadCsvRows(csvPath);

                if (rows.Count == 0)
                {
                    continue;
                }

                var headers = rows[0];
                var resolvedKeyColumn = ResolveHeader(headers, requestedKeyColumn);

                var keyColumnIndex = headers.FindIndex(header =>
                    string.Equals(
                        header,
                        resolvedKeyColumn,
                        StringComparison.Ordinal
                    )
                );

                if (keyColumnIndex < 0)
                {
                    continue;
                }

                for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];

                    if (keyColumnIndex >= row.Count)
                    {
                        continue;
                    }

                    var key = row[keyColumnIndex].Trim();

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    keyCounts[key] = keyCounts.GetValueOrDefault(key) + 1;
                }
            }

            return keyCounts
                .Where(pair => pair.Value > 1)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new LocalizationDuplicateKey
                {
                    Key = pair.Key,
                    Count = pair.Value,
                })
                .ToList();
        }

        private static List<CsvColumns> CreateMappings(
            string keyColumnName,
            IReadOnlyList<string> localeCodes,
            IReadOnlyList<LocalizationSyncLocaleMapping> localeMappings,
            IReadOnlyList<string> headers
        )
        {
            var mappings = new List<CsvColumns>
            {
                new KeyIdColumns
                {
                    KeyFieldName = keyColumnName,
                    IncludeId = false,
                    IncludeSharedComments = false,
                },
            };

            foreach (var localeCode in localeCodes)
            {
                var configuredMapping = localeMappings?.FirstOrDefault(mapping =>
                    string.Equals(
                        mapping.localeCode,
                        localeCode,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                var csvColumnName = string.IsNullOrWhiteSpace(configuredMapping?.csvColumnName)
                    ? GetDefaultCsvColumnName(localeCode)
                    : configuredMapping.csvColumnName.Trim();

                csvColumnName = ResolveHeader(headers, csvColumnName);

                mappings.Add(new LocaleColumns
                {
                    LocaleIdentifier = localeCode,
                    FieldName = csvColumnName,
                    IncludeComments = false,
                });
            }

            return mappings;
        }

        private static string ResolveHeader(
            IReadOnlyList<string> headers,
            string configuredName
        )
        {
            if (headers == null ||
                string.IsNullOrWhiteSpace(configuredName))
            {
                return configuredName;
            }

            return headers.FirstOrDefault(header =>
                       string.Equals(
                           header,
                           configuredName,
                           StringComparison.OrdinalIgnoreCase
                       )) ??
                   configuredName;
        }

        private static List<string> ReadHeaders(string csvPath)
        {
            var rows = ReadCsvRows(csvPath);
            return rows.Count > 0 ? rows[0] : new List<string>();
        }

        private static List<List<string>> ReadCsvRows(string csvPath)
        {
            var content = File.ReadAllText(csvPath, Encoding.UTF8);
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var insideQuote = false;

            for (var index = 0; index < content.Length; index++)
            {
                var character = content[index];

                if (character == '"')
                {
                    if (insideQuote &&
                        index + 1 < content.Length &&
                        content[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        insideQuote = !insideQuote;
                    }

                    continue;
                }

                if (character == ',' &&
                    !insideQuote)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if ((character == '\r' || character == '\n') &&
                    !insideQuote)
                {
                    if (character == '\r' &&
                        index + 1 < content.Length &&
                        content[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString());
                    field.Clear();

                    if (row.Any(value => !string.IsNullOrEmpty(value)))
                    {
                        rows.Add(row);
                    }

                    row = new List<string>();
                    continue;
                }

                field.Append(character);
            }

            if (field.Length > 0 ||
                row.Count > 0)
            {
                row.Add(field.ToString());

                if (row.Any(value => !string.IsNullOrEmpty(value)))
                {
                    rows.Add(row);
                }
            }

            if (rows.Count > 0 &&
                rows[0].Count > 0)
            {
                rows[0][0] = rows[0][0].TrimStart('\uFEFF');
            }

            return rows;
        }

        public static string GetDefaultCsvColumnName(string localeCode)
        {
            return localeCode?.ToLowerInvariant() switch
            {
                "en" => "English",
                "vi" => "Vietnamese",
                "de" => "German",
                "it" => "Italian",
                "fr" => "French",
                "es" => "Spanish",
                "pt-pt" => "Portuguese (Portugal)",
                "pt-br" => "Portuguese (Brazil)",
                "zh-hans" or "zh-cn" => "Chinese Simplified",
                "zh-hant" or "zh-tw" or "zh-hk" => "Chinese Traditional",
                "ja" => "Japanese",
                "ko" => "Korean",
                "ru" => "Russian",
                "th" => "Thai",
                "id" => "Indonesian",
                "tr" => "Turkish",
                "ar" => "Arabic",
                _ => localeCode,
            };
        }
    }
}