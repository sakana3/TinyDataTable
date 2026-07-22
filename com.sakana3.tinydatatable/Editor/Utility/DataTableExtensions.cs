using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyDataTable.Editor
{
    public static class DataTableExtensions
    {
        private static object GetEditorFieldValue(DataTableBase @base , string fileName )
        {
            Type outerType = @base.GetType();

            Type innerType = outerType.GetNestedType("__editorMetaData", BindingFlags.NonPublic);

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
        internal static string GetIDImplement(this DataTableBase @base)
        {
            return GetEditorFieldValue(@base,"CodeTextMetaData") as string;
        }

        // SorceGeneratorが埋め込んだIDの実装部分を取得する
        internal static string[] GetUsingImplement(this DataTableBase @base)
        {
            return GetEditorFieldValue(@base,"UsingNamespaceMetaData") as string[];
        }

        

        /// <summary>
        /// Schema内のリレーション先を検索しRelationに登録する
        /// </summary>
        public static void InjectRelation( this DataTableBase target )
        {
            var types = FieldInfo.FieldsFromType<IIdentifier>(target.GetType(),target.SchemaType())
                .Select(t => t.Type.GetCustomAttribute<IDAttribute>()?.RecordType )
                .Where(t => t != null && t != target.GetType())
                .ToArray();
            
            if ( target.Relations == null || target.Relations.Select(r=>r.GetType()).SequenceEqual(types) is false)
            {
                var newItems = types
                    .SelectMany(t=> AssetDatabase.FindAssets($"t:{t}"))
                    .Select(guid => AssetDatabase.LoadAssetAtPath<DataTableBase>(AssetDatabase.GUIDToAssetPath(guid)))
                    .ToArray();

                var so = new SerializedObject(target);
                var relations = so.FindProperty("_relations");
                relations.arraySize = newItems.Length;
                for (int i = 0; i < relations.arraySize; i++)
                {
                    var relation = relations.GetArrayElementAtIndex(i);
                    relation.objectReferenceValue = newItems[i];                    
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(target);
            }
        }

        public static bool CheckNameSafe( this DataTableBase target )
        {
            var hashSet = new HashSet<string>();
            foreach (var item in target.Headers)
            {
                if (string.IsNullOrEmpty(item.name))
                {
                    continue;
                }
                if (SerializableUtility.CheckCSharpSafeName(item.name) is false)
                {
                    return false;
                }
                if (!hashSet.Add(item.name))
                {
                    return false;
                }
            }

            return true;
        }
    }
}