using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Text.RegularExpressions;
using System.ComponentModel;


namespace TinyDataTable.Editor
{
    /// <summary>
    /// レコードフィールド情報
    /// </summary>
    internal class FieldInfo
    {
        public string Name { set; get; }
        public string Description { set; get; }
        public bool Obsolete { set; get; }
        public Type Type { set; get; }
        public (Type Type ,Attribute,string codeText)[] Attributes { set; get; }
        
        public bool IsArray => Type.IsArray;
        public bool IsValid => Type != null && string.IsNullOrEmpty(Name) is false;
        
        public string ToBaseAttributeString( bool isFiled )
        {
            string str = isFiled ? "TINY" : "";

            if (Obsolete)
            {
                if (string.IsNullOrEmpty(str) is false)
                {
                    str += ",";
                }
                str += $"Obsolete";
            }
            if (string.IsNullOrEmpty(Description) is false)
            {
                if (string.IsNullOrEmpty(str) is false)
                {
                    str += ",";
                }
                str += $"Description(\"{Description}\")";
            }
            return str;
        }

        public IEnumerable<string> ToAttributesString()
        {
            if (Attributes != null && Attributes.Length > 0)
            {
                foreach (var attr in Attributes)
                {
                    yield return attr.codeText;
                }
            }
        }

        private string ToAttributeCodeString(Type type)
        {
            string input = type.FullName;
            string suffix = "Attribute";

            if (input.EndsWith(suffix))
            {
                // 末尾から「Attribute」の文字数分を削る
                return input.Substring(0, input.Length - suffix.Length);
            }
            return input;       
        }       

        /// <summary>
        /// フィールドを取得する
        /// </summary>
        public static List<FieldInfo> FieldsFromType(DataTableRecordBase recordAsset)
        {
            return EnumrateFieldsFromType(recordAsset.GetType(),recordAsset.SchemaType()).ToList();
        }
        
        /// <summary>
        /// フィールドを取得する
        /// </summary>
        public static List<FieldInfo> FieldsFromType(Type recordType, Type schemaType)
        {
            return EnumrateFieldsFromType(recordType,schemaType).ToList();
        }
        
        /// <summary>
        /// フィールドを取得する
        /// </summary>
        public static IEnumerable<FieldInfo> FieldsFromType<T>( Type recordType, Type schemaType)
        {
            foreach (var fieldInfo in EnumrateFieldsFromType(recordType,schemaType))
            {
                var firleType = fieldInfo.Type;
                while (firleType.HasElementType)
                {
                    firleType = firleType.GetElementType();
                }
                if (typeof(T).IsAssignableFrom(firleType))
                {
                    yield return fieldInfo;
                }
            }
        }        
        
        /// <summary>
        /// フィールドを取得する
        /// </summary>
        private static IEnumerable<FieldInfo> EnumrateFieldsFromType(Type recordType, Type schemaType)
        {
            // クラス内のすべてのインスタンスフィールド（public / private / protected）を取得
            System.Reflection.FieldInfo[] allFields = schemaType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Dictionary<string, (Type, string)[]> attributeDict = new Dictionary<string, (Type, string)[]>();
            
            Type innerType = recordType.GetNestedType("__editorInfo", BindingFlags.NonPublic);
            if (innerType != null)
            {
                System.Reflection.FieldInfo fieldInfo = innerType.GetField("FieldAttributesCode", 
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    object value = fieldInfo.GetValue(null);
                    attributeDict = value as Dictionary<string, (Type,string)[]>;
                }
            }            
            
            
            foreach (System.Reflection.FieldInfo field in allFields)
            {
                // 1. [NonSerialized] 属性がついている場合は除外
                if (field.IsDefined(typeof(NonSerializedAttribute), true))
                {
                    continue;
                }

                // __dummyは除外
                if (field.Name == "__dummy")
                {
                    continue;
                }
                
                bool hasSerializeField = field.IsDefined(typeof(SerializeField), true);
                
                // Unity 2024.1以降などの新機能を考慮する場合、[SerializeReference] も対象に含める
                bool hasSerializeReference = field.IsDefined(typeof(SerializeReference), true);

                if (field.IsPublic || hasSerializeField || hasSerializeReference)
                {
                    if (SerializableUtility.IsUnitySerializableType(field.FieldType))
                    {
                        (Type,Attribute,string)[] attributes = attributeDict.GetValueOrDefault(field.Name, Array.Empty<(Type, string)>())
                            .Select(attr => (attr.Item1, field.GetCustomAttribute(attr.Item1), attr.Item2))
                            .ToArray();
                        var info = new FieldInfo()
                        {
                            Name = field.Name,
                            Description = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? String.Empty,
                            Obsolete = field.IsDefined(typeof(ObsoleteAttribute), true),
                            Type =  field.FieldType,
                            Attributes = attributes
                        };
                        yield return info;
                    }
                }
            }
        }        
    }
}