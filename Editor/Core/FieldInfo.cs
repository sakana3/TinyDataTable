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
    public class FieldInfo
    {
        public string Name { set; get; }
        public string Description { set; get; }
        public bool Obsolete { set; get; }
        public Type Type { set; get; }
        public (Type Type , string[] args)[] CustomAttributes { set; get; }

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
            if (CustomAttributes != null && CustomAttributes.Length > 0)
            {
                foreach (var attr in CustomAttributes)
                {
                    var attrCode = ToAttributeCodeString(attr.Type);
                    if (attr.args == null || attr.args.Length <= 0)
                    {
                        var str = $"{attrCode},AttributeMetaData(typeof({attr.Type}))";
                        yield return str;
                    }
                    else
                    {
                        var args = attr.args;
                        var codes = attr.args.Select( t => $"\"{t}\"");
                        var str = $"{attrCode}({string.Join(",",args)}),AttributeMetaData(typeof({attr.Type}),{string.Join(",",codes)})";
                        yield return str;
                    }
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
        public static List<FieldInfo> FieldsFromType(Type type)
        {
            return EnumrateFieldsFromType(type).ToList();
        }
        
        /// <summary>
        /// フィールドを取得する
        /// </summary>
        public static IEnumerable<FieldInfo> FieldsFromType<T>(Type type)
        {
            foreach (var fieldInfo in EnumrateFieldsFromType(type))
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
        public static IEnumerable<FieldInfo> EnumrateFieldsFromType(Type type)
        {
            // クラス内のすべてのインスタンスフィールド（public / private / protected）を取得
            System.Reflection.FieldInfo[] allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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
                        var info = new FieldInfo()
                        {
                            Name = field.Name,
                            Description = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? String.Empty,
                            Obsolete = field.IsDefined(typeof(ObsoleteAttribute), true),
                            Type =  field.FieldType,
                            CustomAttributes = field
                                .GetCustomAttributes<AttributeMetaDataAttribute>()
                                .Select( f => f.Attribute )
                                .ToArray()
                        };
                        yield return info;
                    }
                }
            }
        }        
    }
}