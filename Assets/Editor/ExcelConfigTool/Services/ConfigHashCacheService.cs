using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;

namespace Editor.ExcelConfigTool.Services
{
    public class ConfigHashCacheService
    {
        private const string KEY_PREFIX = "excel_config_tool_md5_";

        public bool HasChanged(string filePath)
        {
            var currentMd5 = CalculateMd5(filePath);
            var key = BuildKey(filePath);
            var cachedMd5 = EditorPrefs.GetString(key, string.Empty);

            if (string.IsNullOrWhiteSpace(cachedMd5))
            {
                return true;
            }

            return cachedMd5 != currentMd5;
        }

        public void SaveHash(string filePath)
        {
            var md5 = CalculateMd5(filePath);
            var key = BuildKey(filePath);
            EditorPrefs.SetString(key, md5);
        }

        public void DeleteHash(string filePath)
        {
            var key = BuildKey(filePath);

            if (EditorPrefs.HasKey(key))
            {
                EditorPrefs.DeleteKey(key);
            }
        }

        private string BuildKey(string filePath)
        {
            return KEY_PREFIX + filePath.Replace("\\", "/").ToLowerInvariant();
        }

        private string CalculateMd5(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);

            return BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }
}