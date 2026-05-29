using System;
using System.Collections.Generic;

namespace Editor.ExcelConfigTool.Models
{
    [Serializable]
    public class ConfigHashCache
    {
        public List<ConfigHashRecord> records = new();
    }

    [Serializable]
    public class ConfigHashRecord
    {
        public string filePath;
        public string md5;
        public string updatedAt;
    }
}