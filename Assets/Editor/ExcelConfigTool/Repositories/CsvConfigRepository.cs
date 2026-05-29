using System.Collections.Generic;
using System.IO;
using System.Linq;
using Editor.ExcelConfigTool.Models;
using Editor.ExcelConfigTool.Utilities;

namespace Editor.ExcelConfigTool.Repositories
{
    public class CsvConfigRepository
    {
        public ConfigSheetInfo ReadFile(string filePath)
        {
            return ReadCsv(filePath);
        }

        public List<ConfigSheetInfo> ReadFolder(string folderPath)
        {
            var result = new List<ConfigSheetInfo>();

            if (!Directory.Exists(folderPath))
            {
                return result;
            }

            var files = Directory
                .GetFiles(folderPath, "*.csv", SearchOption.AllDirectories)
                .Where(v => !Path.GetFileName(v).StartsWith("~$"))
                .ToList();

            result.AddRange(files.Select(ReadFile).Where(sheet => sheet.Columns.Count > 0));
            return result;
        }

        private ConfigSheetInfo ReadCsv(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var configName = ConfigNameUtility.ToPascalCase(fileName);

            var sheetInfo = new ConfigSheetInfo
            {
                ExcelFilePath = filePath,
                SheetName = fileName,
                RowClassName = $"{configName}Row",
                DatabaseClassName = $"{configName}Database",
            };

            var lines = File.ReadAllLines(filePath);

            if (lines.Length <= 1)
            {
                return sheetInfo;
            }

            var headers = ParseCsvLine(lines[0]);
            var rawColumns = new List<(int Index, string RawName, string FieldName)>();

            for (var i = 0; i < headers.Count; i++)
            {
                var rawName = headers[i].Trim();

                if (string.IsNullOrWhiteSpace(rawName))
                {
                    continue;
                }

                rawColumns.Add((
                    i,
                    rawName,
                    ConfigNameUtility.ToCamelCase(rawName)
                ));
            }

            foreach (var rawColumn in rawColumns)
            {
                var values = new List<string>();

                for (var rowIndex = 1; rowIndex < lines.Length; rowIndex++)
                {
                    var cols = ParseCsvLine(lines[rowIndex]);
                    values.Add(rawColumn.Index < cols.Count ? cols[rawColumn.Index] : string.Empty);
                }

                sheetInfo.Columns.Add(new ConfigColumnInfo
                {
                    RawName = rawColumn.RawName,
                    FieldName = rawColumn.FieldName,
                    ColumnIndex = rawColumn.Index,
                    CSharpType = ConfigTypeDetector.DetectType(values),
                });
            }

            for (var rowIndex = 1; rowIndex < lines.Length; rowIndex++)
            {
                var cols = ParseCsvLine(lines[rowIndex]);
                var rowData = new Dictionary<string, string>();
                var hasValue = false;

                foreach (var column in sheetInfo.Columns)
                {
                    var value = column.ColumnIndex < cols.Count
                        ? cols[column.ColumnIndex].Trim()
                        : string.Empty;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        hasValue = true;
                    }

                    rowData[column.FieldName] = value;
                }

                if (hasValue)
                {
                    sheetInfo.Rows.Add(rowData);
                }
            }

            return sheetInfo;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var insideQuote = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (insideQuote &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        insideQuote = !insideQuote;
                    }

                    continue;
                }

                if (c == ',' &&
                    !insideQuote)
                {
                    result.Add(current);
                    current = "";
                    continue;
                }

                current += c;
            }

            result.Add(current);
            return result;
        }
    }
}