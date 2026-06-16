using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Globalization;
using JetBrains.Annotations;

namespace TinyDataTable.Editor
{
    public abstract class AttributeAdapterBase
    {
        public abstract string[] ToCode();
        public abstract void FromCode( Type attributeType,  string[] code );
        protected abstract void CreateUI(VisualElement root);
        public virtual bool DefaultEnable => false;
        
        public bool IsEnable { set; get; } = false;
        VisualElement optionUI;
        public virtual string Title
        {
            get
            {
                var val = AttributeValue;
                var title = val.type.Name;
                if (title.EndsWith("Attribute"))
                {
                    title = title.Substring(0, title.Length - "Attribute".Length);
                }
                
                return ObjectNames.NicifyVariableName(title);;
            }
        }

        public (Type type,string[] args) AttributeValue
        {
            get
            {
                var attr = this.GetType().GetCustomAttribute<AttributeOptionAttribute>();
                if (attr != null)
                {
                    return (attr.AttributeType,ToCode());
                }
                return (null, null);
            }
        }
        
        

        internal void FormFiledInfo(FieldInfo fieldInfo)
        {
            if (fieldInfo != null)
            {
                var attr = fieldInfo.CustomAttributes
                    .FirstOrDefault(t => t.Type == AttributeValue.type);
                if (attr.Type != null)
                {
                    IsEnable = true;
                    FromCode( attr.Type, attr.args);
                }
                else
                {
                    IsEnable = false;
                }
            }
            else
            {
                IsEnable = DefaultEnable;
            }
        }
        
        public VisualElement MakeUI()
        {
            var root = new VisualElement();

            root.style.backgroundColor = new Color(0.2f,0.2f,0.2f,0.5f);
            
            var toggle = new Toggle(Title);
            toggle.value = IsEnable;
            toggle.RegisterValueChangedCallback((evt) => OnChangeEnable(evt.newValue));
    
            root.Add( toggle );
            optionUI = new VisualElement();
            root.Add(optionUI);
            CreateUI(optionUI);
            OnChangeEnable(IsEnable);

            return root;            
        }

        
        public static string[] ToArgStrings(params object[] args)
        {
            return args.Select( a => ToArgString(a)).ToArray();
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

        [CanBeNull]
        public static object FromArg(string argStr)
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
        
        
        
        private void OnChangeEnable(bool isEnable)
        {
            IsEnable = isEnable;
            if( optionUI != null)
            {
                optionUI.enabledSelf = isEnable;
                optionUI.style.display = isEnable ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        public static List<AttributeAdapterBase> FindAttributeOptions( Type type , IReadOnlyCollection<Type> baseTypes)
        {
            var types = TypeCache.GetTypesDerivedFrom<AttributeAdapterBase>()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsDefined(typeof(AttributeOptionAttribute), true))
                .Where(t => t.GetCustomAttribute<AttributeOptionAttribute>().HasType(type) );

            
            var options = types
                .Select( t => Activator.CreateInstance(t))
                .OfType<AttributeAdapterBase>()
                .OrderBy(t => t.Title)
                .ToList();
            
            if (baseTypes != null)
            {
                foreach (var baseType in baseTypes.Reverse())
                {
                    options = options
                        .OrderByDescending(f => f.AttributeValue.type == baseType)
                        .ToList();                    
                }                
            }
            
            return options;
        }
    }
}