using System;
using System.Globalization;

namespace Editor.ExcelConfigTool.Utilities
{
    public static class ConfigValueConverter
    {
        public static object ConvertValue(string value, Type targetType)
        {
            value ??= string.Empty;
            value = value.Trim();

            if (targetType == typeof(string))
            {
                return value;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return GetDefaultValue(targetType);
            }

            if (targetType == typeof(int))
            {
                return int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(long))
            {
                return long.Parse(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(float))
            {
                return float.Parse(value, CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(bool))
            {
                return bool.Parse(value);
            }

            return value;
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType
                ? Activator.CreateInstance(type)
                : null;
        }
    }
}