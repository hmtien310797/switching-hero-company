namespace Editor.ExcelConfigTool.Models
{
    public class ConfigColumnInfo
    {
        public string RawName { get; set; }
        public string FieldName { get; set; }
        public string CSharpType { get; set; }
        public int ColumnIndex { get; set; }
    }
}