using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Editor.ExcelConfigTool.Models;

namespace Editor.ExcelConfigTool.Services
{
    public static class ConfigCodeGenerator
    {
        private const string GENERATED_NAMESPACE = "Game.Configs.Generated";

        private static readonly IReadOnlyList<IConfigRowCustomMapping> CustomRowMappings =
            new IConfigRowCustomMapping[]
            {
                new ItemConfigCurrencyEnumMapping(),
                new FeatureUnlockEnumMapping(),
            };

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

                foreach (var mapping in CustomRowMappings.Where(mapping => mapping.CanHandle(sheet)))
                {
                    changedFileCount += mapping.Generate(outputScriptFolder, sheet) ? 1 : 0;
                }
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

        /// <summary>Định nghĩa một bước generate code tùy chỉnh từ từng row của một config cụ thể.</summary>
        private interface IConfigRowCustomMapping
        {
            /// <summary>Kiểm tra mapping có xử lý sheet hiện tại hay không.</summary>
            bool CanHandle(ConfigSheetInfo sheet);

            /// <summary>Generate file hoặc vùng code tùy chỉnh và trả true khi nội dung thay đổi.</summary>
            bool Generate(string outputScriptFolder, ConfigSheetInfo sheet);
        }

        /// <summary>Base mapping dùng để generate enum từ cặp key và id của từng row config.</summary>
        private abstract class GeneratedEnumMappingBase : IConfigRowCustomMapping
        {
            private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
                "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
                "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
                "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
                "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
                "void", "volatile", "while",
            };

            protected abstract string SheetSuffix { get; }
            protected abstract string IdFieldName { get; }
            protected abstract string KeyFieldName { get; }
            protected abstract string EnumName { get; }

            /// <summary>Kiểm tra sheet hiện tại có đúng tên và đủ cột để generate enum hay không.</summary>
            public bool CanHandle(ConfigSheetInfo sheet)
            {
                return sheet.SheetName?.EndsWith(SheetSuffix, StringComparison.OrdinalIgnoreCase) == true &&
                       sheet.Columns.Any(column => column.FieldName == IdFieldName) &&
                       sheet.Columns.Any(column => column.FieldName == KeyFieldName);
            }

            /// <summary>Cập nhật riêng vùng enum tương ứng trong EnumsGenerated.cs.</summary>
            public bool Generate(string outputScriptFolder, ConfigSheetInfo sheet)
            {
                var members = ReadMembers(sheet);
                var generatedRegion = GenerateRegion(members);
                var markerName = $"{SheetSuffix}:{EnumName}";

                return GeneratedEnumsFile.UpdateRegion(
                    outputScriptFolder,
                    markerName,
                    generatedRegion
                );
            }

            private List<(string Name, int Value)> ReadMembers(ConfigSheetInfo sheet)
            {
                var result = new List<(string Name, int Value)>();
                var names = new HashSet<string>(StringComparer.Ordinal);
                var values = new HashSet<int>();

                for (var index = 0; index < sheet.Rows.Count; index++)
                {
                    var row = sheet.Rows[index];
                    row.TryGetValue(KeyFieldName, out var rawKey);
                    row.TryGetValue(IdFieldName, out var rawId);

                    if (string.IsNullOrWhiteSpace(rawKey) &&
                        string.IsNullOrWhiteSpace(rawId))
                    {
                        continue;
                    }

                    if (!int.TryParse(rawId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                    {
                        throw new InvalidDataException(
                            $"{SheetSuffix} row {index + 2}: {IdFieldName} '{rawId}' không phải số nguyên hợp lệ."
                        );
                    }

                    var memberName = ToEnumMember(rawKey);

                    if (!names.Add(memberName))
                    {
                        throw new InvalidDataException(
                            $"{SheetSuffix} row {index + 2}: enum member '{memberName}' bị trùng."
                        );
                    }

                    if (!values.Add(id))
                    {
                        throw new InvalidDataException(
                            $"{SheetSuffix} row {index + 2}: {IdFieldName} '{id}' bị trùng."
                        );
                    }

                    result.Add((memberName, id));
                }

                return result.OrderBy(member => member.Value).ToList();
            }

            private string ToEnumMember(string itemKey)
            {
                var memberName = Regex.Replace(itemKey?.Trim() ?? string.Empty, @"[^a-zA-Z0-9_]", "_");

                if (string.IsNullOrWhiteSpace(memberName))
                {
                    throw new InvalidDataException(
                        $"{SheetSuffix} có {KeyFieldName} rỗng hoặc không thể dùng làm enum member."
                    );
                }

                if (char.IsDigit(memberName[0]))
                {
                    memberName = "_" + memberName;
                }
                else if (CSharpKeywords.Contains(memberName))
                {
                    memberName = "@" + memberName;
                }

                return memberName;
            }

            private string GenerateRegion(IReadOnlyList<(string Name, int Value)> members)
            {
                var sb = new StringBuilder();

                sb.AppendLine("    /// <summary>");

                sb.AppendLine(
                    $"    /// Auto generated từ {SheetSuffix}: {KeyFieldName} = {IdFieldName}."
                );

                sb.AppendLine("    /// Không chỉnh sửa thủ công nội dung nằm giữa hai marker.");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public enum {EnumName}");
                sb.AppendLine("    {");

                foreach (var member in members)
                {
                    sb.AppendLine($"        {member.Name} = {member.Value},");
                }

                sb.AppendLine("    }");
                return sb.ToString();
            }
        }

        /// <summary>Generate ECurrencyType từ item_key và item_id của bảng item_config.</summary>
        private sealed class ItemConfigCurrencyEnumMapping : GeneratedEnumMappingBase
        {
            protected override string SheetSuffix => "item_config";
            protected override string IdFieldName => "itemId";
            protected override string KeyFieldName => "itemKey";
            protected override string EnumName => "ECurrencyType";
        }

        /// <summary>Generate EFeatureUnlockType từ feature_key và feature_id.</summary>
        private sealed class FeatureUnlockEnumMapping : GeneratedEnumMappingBase
        {
            protected override string SheetSuffix => "feature_unlock_config";
            protected override string IdFieldName => "featureId";
            protected override string KeyFieldName => "featureKey";
            protected override string EnumName => "EFeatureUnlockType";
        }

        /// <summary>Quản lý các vùng enum được generate chung trong EnumsGenerated.cs.</summary>
        private static class GeneratedEnumsFile
        {
            private const string FILE_NAME = "EnumsGenerated.cs";

            /// <summary>Cập nhật một vùng enum mà không ảnh hưởng các enum generated còn lại.</summary>
            public static bool UpdateRegion(
                string outputScriptFolder,
                string markerName,
                string generatedContent
            )
            {
                Directory.CreateDirectory(outputScriptFolder);

                var outputPath = Path.Combine(outputScriptFolder, FILE_NAME);
                var beginMarker = $"    // <auto-generated:{markerName}>";
                var endMarker = $"    // </auto-generated:{markerName}>";

                var generatedRegion =
                    beginMarker +
                    Environment.NewLine +
                    generatedContent.TrimEnd() +
                    Environment.NewLine +
                    endMarker;

                var content = File.Exists(outputPath)
                    ? File.ReadAllText(outputPath)
                    : $"namespace {GENERATED_NAMESPACE}{Environment.NewLine}" +
                      $"{{{Environment.NewLine}}}{Environment.NewLine}";

                var nextContent = ReplaceOrAppendRegion(
                    content,
                    beginMarker,
                    endMarker,
                    generatedRegion,
                    outputPath
                );

                return WriteIfChanged(outputPath, nextContent);
            }

            private static string ReplaceOrAppendRegion(
                string content,
                string beginMarker,
                string endMarker,
                string generatedRegion,
                string outputPath
            )
            {
                var beginIndex = content.IndexOf(beginMarker, StringComparison.Ordinal);
                var endIndex = content.IndexOf(endMarker, StringComparison.Ordinal);

                if (beginIndex >= 0 &&
                    endIndex >= beginIndex)
                {
                    endIndex += endMarker.Length;
                    return content[..beginIndex] + generatedRegion + content[endIndex..];
                }

                if (beginIndex >= 0 ||
                    endIndex >= 0)
                {
                    throw new InvalidDataException(
                        $"Marker '{beginMarker}' trong '{outputPath}' không đầy đủ."
                    );
                }

                var namespaceCloseIndex = content.LastIndexOf('}');

                if (namespaceCloseIndex < 0)
                {
                    throw new InvalidDataException(
                        $"Không tìm thấy dấu đóng namespace trong '{outputPath}'."
                    );
                }

                return content.Insert(
                    namespaceCloseIndex,
                    Environment.NewLine + generatedRegion + Environment.NewLine
                );
            }
        }
    }
}