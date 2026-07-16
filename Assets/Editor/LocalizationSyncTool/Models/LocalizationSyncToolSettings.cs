using System;
using System.Collections.Generic;

namespace Editor.LocalizationSyncTool.Models
{
    [Serializable]
    public class LocalizationSyncUrlEntry
    {
        public string url;
        public string fileName;
    }

    [Serializable]
    public class LocalizationSyncLocaleMapping
    {
        public string localeCode;
        public string csvColumnName;
    }

    [Serializable]
    public class LocalizationSyncToolSettings
    {
        public int version = 1;
        public List<LocalizationSyncUrlEntry> entries = new();
        public string csvFolder = "Assets/Immortal Switch/Addressable/Localizations/CSV";
        public string tableCollectionName = "Default";
        public string keyColumnName = "KEY";
        public List<LocalizationSyncLocaleMapping> localeMappings = new();
        public bool removeMissingEntries;
    }
}