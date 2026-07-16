using System;
using System.Collections.Generic;

namespace Editor.ExcelConfigTool.Models
{
    [Serializable]
    public class UrlEntry
    {
        public string url;
        public string fileName;
    }

    [Serializable]
    public class ExcelConfigToolSettings
    {
        public int version = 2;
        public List<UrlEntry> entries = new();
        public string inputFolder = "Assets/Immortal Switch/GameConfigs/Excel";
        public string outputScriptFolder = "Assets/Immortal Switch/GameConfigs/Generated/Scripts";
        public string outputAssetFolder = "Assets/Immortal Switch/GameConfigs/Generated/Assets";
    }
}
