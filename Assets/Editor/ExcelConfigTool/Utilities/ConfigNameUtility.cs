using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Editor.ExcelConfigTool.Utilities
{
    public static class ConfigNameUtility
    {
        public static string ToPascalCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Unknown";
            }

            value = Regex.Replace(value, @"[^a-zA-Z0-9_ ]", "_");

            var parts = value.Split(
                new[] { '_', ' ', },
                System.StringSplitOptions.RemoveEmptyEntries
            );

            var sb = new StringBuilder();

            foreach (var part in parts)
            {
                if (part.Length == 0)
                {
                    continue;
                }

                sb.Append(char.ToUpper(part[0], CultureInfo.InvariantCulture));

                if (part.Length > 1)
                {
                    sb.Append(part[1..]);
                }
            }

            var result = sb.ToString();

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "Unknown";
            }

            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            return result;
        }

        public static string ToCamelCase(string value)
        {
            var pascal = ToPascalCase(value);

            if (string.IsNullOrEmpty(pascal))
            {
                return "unknown";
            }

            return char.ToLower(pascal[0], CultureInfo.InvariantCulture) + pascal[1..];
        }
    }
}