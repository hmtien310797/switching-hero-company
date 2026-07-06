using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Editor.ExcelConfigTool.Models;
using Editor.ExcelConfigTool.Utilities;
using UnityEditor;
using UnityEngine;

namespace Editor.ExcelConfigTool.Services
{
    public static class ScriptableObjectAssetWriter
    {
        public static void CreateOrUpdate(
            string assetPath,
            string generatedNamespace,
            ConfigSheetInfo sheetInfo
        )
        {
            var databaseType = FindType($"{generatedNamespace}.{sheetInfo.DatabaseClassName}");
            var rowType = FindType($"{generatedNamespace}.{sheetInfo.RowClassName}");

            if (databaseType == null)
            {
                Debug.LogError($"Database type not found: {sheetInfo.DatabaseClassName}");
                return;
            }

            if (rowType == null)
            {
                Debug.LogError($"Row type not found: {sheetInfo.RowClassName}");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance(databaseType);
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var rowsField = databaseType.GetField("rows");

            if (rowsField == null)
            {
                Debug.LogError($"Field 'rows' not found in {databaseType.Name}");
                return;
            }

            var listType = typeof(List<>).MakeGenericType(rowType);
            var list = (IList)Activator.CreateInstance(listType);

            foreach (var rowData in sheetInfo.Rows)
            {
                var rowInstance = Activator.CreateInstance(rowType);

                foreach (var column in sheetInfo.Columns)
                {
                    var field = rowType.GetField(column.FieldName);

                    if (field == null)
                    {
                        continue;
                    }

                    rowData.TryGetValue(column.FieldName, out var rawValue);

                    var value = ConfigValueConverter.ConvertValue(
                        rawValue,
                        field.FieldType
                    );

                    field.SetValue(rowInstance, value);
                }

                list.Add(rowInstance);
            }

            rowsField.SetValue(asset, list);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Generated/Updated SO: {assetPath}");
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
        }
    }
}