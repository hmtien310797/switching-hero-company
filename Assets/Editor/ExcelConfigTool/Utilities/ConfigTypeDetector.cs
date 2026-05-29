using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Editor.ExcelConfigTool.Utilities
{
    public static class ConfigTypeDetector
    {
        public static string DetectType(IEnumerable<string> values)
        {
            var list = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            if (list.Count == 0)
            {
                return "string";
            }

            if (list.All(v => bool.TryParse(v, out _)))
            {
                return "bool";
            }

            if (list.All(v => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                return "int";
            }

            if (list.All(v => long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
            {
                return "long";
            }

            return list.All(v => float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) ? "float" : "string";
        }
    }
}