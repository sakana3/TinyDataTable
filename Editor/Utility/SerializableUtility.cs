using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Globalization;


namespace TinyDataTable.Editor
{
    internal static class SerializableUtility
    {
        internal static bool IsUnitySerializableType(Type type)
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
        internal static bool CheckUnitySerializable(Type type)
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
        
        internal static bool CheckCSharpSafeName(string name)
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
        
        internal static bool CheckExistClass(string namespaceName, string className)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Any(t => t.Name == className && t.Namespace == namespaceName);
        }

        internal static readonly Dictionary<Type, string> TypeAliases = new()
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
        
        public static string ToArgString(object arg)
        {
            // null の場合は C# の null リテラル
            if (arg is null) return "null";

            return arg switch
            {
                // 文字列・文字型（エスケープ処理を入れて C# のリテラルにする）
                string s => $"\"{EscapeString(s)}\"",
                char c   => $"'{EscapeChar(c)}'",

                // 真偽値
                bool b   => b ? "true" : "false",

                // 浮動小数点数（C#のサフィックスを付与、かつロケール依存を防ぐため InvariantCulture を指定）
                float f  => f.ToString(CultureInfo.InvariantCulture) + "f",
                double d => d.ToString(CultureInfo.InvariantCulture) + "d",
                decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",

                // 整数系（そのまま文字列化）
                int i    => i.ToString(),
                long l   => l.ToString() + "L",
                uint ui  => ui.ToString() + "U",
                ulong ul => ul.ToString() + "UL",

                // その他（Enum や独自の型などの暫定対応。必要に応じて拡張してください）
                Enum e   => $"{e.GetType().Name}.{e}",

                // どの型にも当てはまらない場合は ToString()
                _        => arg.ToString() ?? "null"
            };
        }        


        public static object StringToObj(string argStr)
        {
            if (string.IsNullOrWhiteSpace(argStr)) return null;
            
            // 前後の余計な空白をトリム
            argStr = argStr.Trim();

            // 1. null リテラルのチェック
            if (argStr == "null") return null;

            // 2. 真偽値のチェック
            if (argStr == "true") return true;
            if (argStr == "false") return false;

            // 3. 文字列リテラルのチェック ("..." で囲まれているか)
            if (argStr.StartsWith("\"") && argStr.EndsWith("\""))
            {
                string inner = argStr.Substring(1, argStr.Length - 2);
                return UnescapeString(inner);
            }

            // 4. 文字リテラルのチェック ('...' で囲まれているか)
            if (argStr.StartsWith("'") && argStr.EndsWith("'"))
            {
                string inner = argStr.Substring(1, argStr.Length - 2);
                string unescaped = UnescapeString(inner);
                return unescaped.Length > 0 ? unescaped[0] : '\0';
            }

            // 5. 数値リテラルのチェック（サフィックスの解析）
            // 末尾の文字を確認して型を特定する
            char suffix = char.ToUpperInvariant(argStr[^1]);
            
            // UL や UL などの2文字サフィックスのケア
            if (argStr.EndsWith("UL", StringComparison.OrdinalIgnoreCase) || 
                argStr.EndsWith("LU", StringComparison.OrdinalIgnoreCase))
            {
                string numPart = argStr[..^2];
                if (ulong.TryParse(numPart,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out ulong ul)) return ul;
            }

            // 1文字サフィックスの判定
            string numberPart = suffix is 'F' or 'D' or 'M' or 'L' or 'U' ? argStr[..^1] : argStr;

            switch (suffix)
            {
                case 'F': // float
                    if (float.TryParse(numberPart,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out float f)) return f;
                    break;
                case 'D': // double
                    if (double.TryParse(numberPart,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double d)) return d;
                    break;
                case 'M': // decimal
                    if (decimal.TryParse(numberPart,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out decimal m)) return m;
                    break;
                case 'L': // long
                    if (long.TryParse(numberPart,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out long l)) return l;
                    break;
                case 'U': // uint
                    if (uint.TryParse(numberPart,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out uint ui)) return ui;
                    break;
                default:
                    // サフィックスがない場合、整数(int)かデフォルトの小数(double)としてパースを試みる
                    if (int.TryParse(argStr,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out int i)) return i;
                    if (double.TryParse(argStr,System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl)) return dbl;
                    break;
            }

            // 6. それでもパースできない場合は、文字列としてそのまま返すか、例外を投げる
            return argStr;
        }

        // エスケープされた文字列を元に戻す補助メソッド
        private static string UnescapeString(string s)
        {
            return s.Replace("\\\\", "\\")
                    .Replace("\\\"", "\"")
                    .Replace("\\'", "'")
                    .Replace("\\r", "\r")
                    .Replace("\\n", "\n")
                    .Replace("\\t", "\t");
        }        
            
        // 文字列内の改行やダブルクォーテーションをエスケープする補助メソッド
        private static string EscapeString(string s)
        {
            return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string EscapeChar(char c)
        {
            return c switch
            {
                '\'' => "\\'",
                '\\' => "\\\\",
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                _    => c.ToString()
            };
        }
    }
}