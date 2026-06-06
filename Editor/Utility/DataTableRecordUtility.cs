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
    public static class DataTableRecordUtility
    {
        public static List<RecordFieldInfo> GetSerializableFields(Type type)
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
                    if (IsUnitySerializableType(field.FieldType))
                    {
                        var info = new RecordFieldInfo()
                        {
                            name = field.Name,
                            description = field.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                            id = 0,
                            obsolete = field.IsDefined(typeof(ObsoleteAttribute), true),
                            type =  field.FieldType
                        };
                        serializableFields.Add(info);
                    }
                }
            }

            return serializableFields;
        }

        private static bool IsUnitySerializableType(Type type)
        {
            // プリミティブ型、string、一部の組み込み型（Vector3など）
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal)) return true;
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4)) return true;
            if (type == typeof(Quaternion) || type == typeof(Color) || type == typeof(Bounds) || type == typeof(Rect)) return true;

            // UnityEngine.Objectの派生クラス（MonoBehaviour, ScriptableObject, Texture2Dなど）
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;

            // 列挙型（Enum）
            if (type.IsEnum) return true;

            // 配列またはList<T>の場合、要素の型がシリアライズ可能かチェック
            if (type.IsArray)
            {
                return IsUnitySerializableType(type.GetElementType());
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return IsUnitySerializableType(type.GetGenericArguments()[0]);
            }

            // [System.Serializable] 属性がついているカスタムクラス/構造体
            if (type.IsDefined(typeof(SerializableAttribute), true)) return true;

            return false;
        }
        
       
        // C#の予約語リスト
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while"
        };        
        
        public static bool CheckCSharpSafeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
        
            // 先頭: 文字 (Unicode Letter) またはアンダースコア
            // 2文字目以降: 文字、数字、またはアンダースコア
            // (日本語などの文字も許容する正規表現パターンです)
            if (!Regex.IsMatch(name, @"^[\p{L}_][\p{L}\p{N}_]*$"))
            {
                return false;
            }

            // 予約語と完全一致しないかをチェック
            return !CSharpKeywords.Contains(name);
        }      
        
        public static bool CheckExistClass(string namespaceName, string className)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Any(t => t.Name == className && t.Namespace == namespaceName);
        }

        public static readonly Dictionary<Type, string> TypeAliases = new()
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(object), "object" },
            { typeof(string), "string" },
            { typeof(void), "void" }
        };
        
        // C#の別名を取得する拡張メソッド
        public static string GetCSharpAlias(this Type type)
        {
            // 辞書にあれば別名を返し、なければ通常の本名（Name）を返す
            return TypeAliases.TryGetValue(type, out var alias) ? alias : type.Name;
        }
        public static string GetCSharpAliasFull(this Type type)
        {
            // 辞書にあれば別名を返し、なければ通常の本名（Name）を返す
            return TypeAliases.TryGetValue(type, out var alias) ? alias : type.FullName;
        }
        
    }
}