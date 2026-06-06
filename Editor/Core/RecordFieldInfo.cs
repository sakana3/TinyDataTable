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
    public struct RecordFieldInfo
    {
        public string Name { set; get; }
        public string Description { set; get; }
        public int ID { set; get; }
        public bool Obsolete { set; get; }
        public Type Type { set; get; }

        public bool IsArray => Type.IsArray;


        /// <summary>
        /// フィールドを取得する
        /// </summary>
        public static List<RecordFieldInfo> FieldsFromType(Type type)
        {
            var serializableFields = new List<RecordFieldInfo>();

            // クラス内のすべてのインスタンスフィールド（public / private / protected）を取得
            FieldInfo[] allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (FieldInfo field in allFields)
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
                        var info = new RecordFieldInfo()
                        {
                            Name = field.Name,
                            Description = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                            ID = 0,
                            Obsolete = field.IsDefined(typeof(ObsoleteAttribute), true),
                            Type =  field.FieldType
                        };
                        serializableFields.Add(info);
                    }
                }
            }

            return serializableFields;
        }
        
        
    }
}