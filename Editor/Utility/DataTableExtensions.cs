using System;
using System.Reflection;

namespace TinyDataTable.Editor
{
    internal static class DataTableExtensions
    {
        private static object GetEditorFieldValue(DataTableRecordBase recordBase , string fileName )
        {
            Type outerType = recordBase.GetType();

            Type innerType = outerType.GetNestedType("__editorInfo", BindingFlags.NonPublic);

            if (innerType == null)
            {
                return null;
            }            
            
            System.Reflection.FieldInfo fieldInfo = innerType.GetField(fileName, 
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                return null;
            }
            
            object value = fieldInfo.GetValue(null);
            
            return value;
        }
        
        // SorceGeneratorが埋め込んだIDの実装部分を取得する
        public static string GetIDImplement(this DataTableRecordBase recordBase)
        {
            return GetEditorFieldValue(recordBase,"CodeText") as string;
        }

        // SorceGeneratorが埋め込んだIDの実装部分を取得する
        public static string[] GetUsingImplement(this DataTableRecordBase recordBase)
        {
            return GetEditorFieldValue(recordBase,"UsingNamespaces") as string[];
        }
    }
}