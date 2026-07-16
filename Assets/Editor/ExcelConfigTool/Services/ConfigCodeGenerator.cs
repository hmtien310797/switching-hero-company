using System.Collections.Generic;
using System.IO;
using System.Text;
using Editor.ExcelConfigTool.Models;

namespace Editor.ExcelConfigTool.Services
{
    public static class ConfigCodeGenerator
    {
        private const string GENERATED_NAMESPACE = "Game.Configs.Generated";

        public static int GenerateScripts(
            string outputScriptFolder,
            IReadOnlyList<ConfigSheetInfo> sheets
        )
        {
            Directory.CreateDirectory(outputScriptFolder);
            var changedFileCount = 0;

            foreach (var sheet in sheets)
            {
                var rowCode = GenerateRowClass(sheet);
                var databaseCode = GenerateDatabaseClass(sheet);

                changedFileCount += WriteIfChanged(
                    Path.Combine(outputScriptFolder, $"{sheet.RowClassName}.cs"),
                    rowCode
                )
                    ? 1
                    : 0;

                changedFileCount += WriteIfChanged(
                    Path.Combine(outputScriptFolder, $"{sheet.DatabaseClassName}.cs"),
                    databaseCode
                )
                    ? 1
                    : 0;
            }

            return changedFileCount;
        }

        private static bool WriteIfChanged(string path, string content)
        {
            if (File.Exists(path) &&
                File.ReadAllText(path) == content)
            {
                return false;
            }

            File.WriteAllText(path, content);
            return true;
        }

        private static string GenerateRowClass(ConfigSheetInfo sheet)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {GENERATED_NAMESPACE}");
            sb.AppendLine("{");

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Auto generated row data from CSV: {sheet.SheetName}");
            sb.AppendLine("    /// </summary>");

            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public class {sheet.RowClassName}");
            sb.AppendLine("    {");

            foreach (var column in sheet.Columns)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// CSV Column: {column.RawName}");
                sb.AppendLine("        /// </summary>");

                sb.AppendLine(
                    $"        public {column.CSharpType} {column.FieldName};"
                );

                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GenerateDatabaseClass(ConfigSheetInfo sheet)
        {
            return $@"
using System.Collections.Generic;
using UnityEngine;

namespace {GENERATED_NAMESPACE}
{{
    /// <summary>
    /// Auto generated ScriptableObject database from CSV: {sheet.SheetName}
    /// </summary>
    [CreateAssetMenu(
        fileName = ""{sheet.DatabaseClassName}"",
        menuName = ""Game Configs/{sheet.DatabaseClassName}""
    )]
    public class {sheet.DatabaseClassName} : ScriptableObject
    {{
        /// <summary>
        /// Config rows.
        /// </summary>
        public List<{sheet.RowClassName}> rows = new();
    }}
}}
";
        }

        public static string GetGeneratedNamespace()
        {
            return GENERATED_NAMESPACE;
        }
    }
}