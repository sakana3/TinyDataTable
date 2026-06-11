using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;


namespace TinyDataTable.Editor
{
    public static class SerializableUtility
    {
        public static bool IsUnitySerializableType(Type type)
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
        
        
        /// <summary>
        /// 指定された型がUnityでシリアライズ可能かどうかを判定する
        /// </summary>
        public static bool CheckUnitySerializable(Type type)
        {
            if (type == null) return false;

            //コンパイラが自動生成したものは除外
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return false;
            }
            
            // 1. プリミティブ型と文字列
            if (type.IsPrimitive || type == typeof(string)) return true;

            // 2. Enum
            if (type.IsEnum) return true;

            // 3. Unity Object (参照として保存可能)
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;

            // 4. 配列とリスト
            if (type.IsArray)
            {
                // 多次元配列は不可
                if (type.GetArrayRank() > 1) return false;
                return CheckUnitySerializable(type.GetElementType());
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return CheckUnitySerializable(type.GetGenericArguments()[0]);
            }

            // 5. Unityの特定の組み込み構造体 (代表的なもの)
            if (type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                type == typeof(Quaternion) || type == typeof(Matrix4x4) ||
                type == typeof(Color) || type == typeof(Color32) ||
                type == typeof(Rect) || type == typeof(Bounds) ||
                type == typeof(LayerMask) || type == typeof(AnimationCurve) || type == typeof(Gradient) ||
                type == typeof(RectOffset) || type == typeof(GUIStyle) ||
                type == typeof(Vector2Int) || type == typeof(Vector3Int) || type == typeof(RectInt) || type == typeof(BoundsInt))
            {
                return true;
            }

            // 6. [Serializable] 属性を持つクラス・構造体
            if (type.IsSerializable) // System.SerializableAttribute が付いているか
            {
                // ジェネリック定義そのもの (List<>など) は不可
                if (type.IsGenericTypeDefinition) return false;
                
                // decimal, DateTime, Dictionary など、.NETではSerializableだがUnityでは非対応なものを除外
                if (type == typeof(decimal) || type == typeof(DateTime) || type == typeof(TimeSpan) || 
                    type == typeof(Guid) || type == typeof(Uri))
                {
                    return false;
                }
                
                // ジェネリック型の場合、型引数もシリアライズ可能である必要がある
                if (type.IsGenericType)
                {
                    foreach (var arg in type.GetGenericArguments())
                    {
                        if (!CheckUnitySerializable(arg)) return false;
                    }
                }

                return true;
            }

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

        internal static string DispName(this Type type)
        {
            return type.FullName.Split('.').LastOrDefault();
        }
        
        // C#の別名を取得する拡張メソッド
        internal static string GetCSharpAlias(this Type type)
        {
            // 辞書にあれば別名を返し、なければ通常の本名（Name）を返す
            if (type.IsArray)
            {
                return TypeAliases.TryGetValue(type.GetElementType(), out var alias) ? $"{alias}[]" : type.DispName();
            }
            else
            {
                return TypeAliases.TryGetValue(type, out var alias) ? alias : type.DispName();
            }
        }
        internal static string GetCSharpAliasFull(this Type type)
        {
            // 辞書にあれば別名を返し、なければ通常の本名（Name）を返す
            if (type.IsArray)
            {
                return TypeAliases.TryGetValue(type.GetElementType(), out var alias) ? $"{alias}[]" : type.FullName.Replace("+", ".");
            }
            else
            {
                return TypeAliases.TryGetValue(type, out var alias) ? alias : type.FullName.Replace("+", ".");
            }
        }
        
    }
}