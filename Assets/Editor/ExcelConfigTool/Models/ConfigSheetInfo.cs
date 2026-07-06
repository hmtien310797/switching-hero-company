using System.Collections.Generic;

namespace Editor.ExcelConfigTool.Models
{
    public class ConfigSheetInfo
    {
        public string ExcelFilePath { get; set; }
        public string SheetName { get; set; }

        public string RowClassName { get; set; }
        public string DatabaseClassName { get; set; }

        public List<ConfigColumnInfo> Columns { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();
    }
}