using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyDataTable.Editor
{
    public class CSharpCodeBuilder
    {
        private StringBuilder _sb = new StringBuilder(4096);
        private int _indentLevel = 0;
        private const string IndentString = "    "; // 4スペース

        /// <summary>
        /// 現在のインデント文字列を取得
        /// </summary>
        private string CurrentIndent => string.Concat(Enumerable.Repeat(IndentString, _indentLevel));

        /// <summary>
        /// 行を追加する
        /// </summary>
        public CSharpCodeBuilder AppendLine(string line = "")
        {
            if (!string.IsNullOrEmpty(line))
            {
                _sb.AppendLine($"{CurrentIndent}{line}");
            }
            else
            {
                _sb.AppendLine();
            }
            return this;
        }

        /// <summary>
        /// インデントなしで行を追加する（#if ディレクティブなど用）
        /// </summary>
        public CSharpCodeBuilder AppendLineNoIndent(string line)
        {
            _sb.AppendLine(line);
            return this;
        }

        /// <summary>
        /// ブロックを開始する {
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginBlock(string header = "")
        {
            if (!string.IsNullOrEmpty(header))
            {
                AppendLine(header);
            }
            AppendLine("{");
            _indentLevel++;
            return new BlockScope( this);
        }

        /// <summary>
        /// ブロックを終了する }
        /// </summary>
        public CSharpCodeBuilder EndBlock()
        {
            if (_indentLevel > 0) _indentLevel--;
            
            AppendLine("}");
            return this;
        }
        /// <summary>
        /// ブロックを終了する }
        /// </summary>
        public CSharpCodeBuilder EndBlock(string footer )
        {
            if (_indentLevel > 0) _indentLevel--;
            
            AppendLine($"}}{footer}");
            return this;
        }

        /// <summary>
        /// using ステートメントを追加
        /// </summary>
        public CSharpCodeBuilder AddUsing(string namespaceName)
        {
            AppendLine($"using {namespaceName};");
            return this;
        }

        /// <summary>
        /// using アトリビュートを追加
        /// </summary>
        public CSharpCodeBuilder AddAttribute( string attributres )
        {
            AppendLine($"[{attributres}]");
            return this;
        }        
        
        /// <summary>
        /// 名前空間を開始
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginNamespace(string namespaceName)
        {
            if (string.IsNullOrEmpty(namespaceName))
            {
                return new BlockScope(this, false);
            }
            
            return BeginBlock($"namespace {namespaceName}");
        }

        /// <summary>
        /// クラス定義を開始
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginClass(string className, string accessModifier = "public", string inherit = null, bool isPartial = false)
        {
            var partialStr = isPartial ? "partial " : "";
            var inheritStr = string.IsNullOrEmpty(inherit) ? "" : $" : {inherit}";
            return BeginBlock($"{accessModifier} {partialStr}class {className}{inheritStr}");
        }

        /// <summary>
        /// クラス定義を開始
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginStruct(string className, string accessModifier = "public", string inherit = null, bool isPartial = false)
        {
            var partialStr = isPartial ? "partial " : "";
            var inheritStr = string.IsNullOrEmpty(inherit) ? "" : $" : {inherit}";
            return BeginBlock($"{accessModifier} {partialStr}struct {className}{inheritStr}");
        }
        
        /// <summary>
        /// 列挙定義を開始
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginEnum(string enumName, string accessModifier = "public")
        {
            return BeginBlock($"{accessModifier} enum {enumName}");
        }        

        /// <summary>
        /// メソッド定義を開始
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginMethod(string returnType, string methodName, string args = "", string accessModifier = "public", bool isStatic = false)
        {
            if (isStatic)
            {
                return BeginBlock($"{accessModifier} static{returnType} {methodName}({args})");
            }
            else
            {
                return BeginBlock($"{accessModifier} {returnType} {methodName}({args})");
            }
        }

        /// <summary>
        /// メソッド定義を開始
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginConstructor( string methodName, string args = "", string accessModifier = "public" , string serfix = null)
        {
            string _srtfix = string.IsNullOrEmpty(serfix) ? "" : $" : {serfix}";
            
            return BeginBlock($"{accessModifier} {methodName}({args}){_srtfix}");
        }
        
        /// <summary>
        /// If定義を開始
        /// </summary>
        public CSharpCodeBuilder.BlockScope BeginIf( string code)
        {
            return BeginBlock($"if ({code})");
        }            

        /// <summary>
        /// #define定義を開始
        /// </summary>
        public CSharpCodeBuilder.CompilerScope BeginIfdef( string macro)
        {
            AppendLineNoIndent($"#if {macro}");
            return new CompilerScope( this,$"#endif //{macro} ");
        }

        /// <summary>
        /// #define定義を開始
        /// </summary>
        public CSharpCodeBuilder.CompilerScope BeginRegion( string regionStr)
        {
            AppendLineNoIndent($"#region {regionStr}");
            return new CompilerScope( this,$"#endregion //{regionStr} ");
        }
        
        /// <summary>
        /// プロパティを追加
        /// </summary>
        public CSharpCodeBuilder AddProperty(string type, string name, string accessModifier = "public", bool hasSet = true)
        {
            var setStr = hasSet ? " set;" : "";
            AppendLine($"{accessModifier} {type} {name} {{ get;{setStr} }}");
            return this;
        }

        /// <summary>
        /// フィールドを追加
        /// </summary>
        public CSharpCodeBuilder AddField(string type, string name, string accessModifier = "public", string initialValue = null)
        {
            var initStr = initialValue != null ? $" = {initialValue}" : "";
            AppendLine($"{accessModifier} {type} {name}{initStr};");
            return this;
        }

        /// <summary>
        /// フィールドを追加
        /// </summary>
        public CSharpCodeBuilder AddCode( string line)
        {
            AppendLine($"{line};");
            return this;
        }
        
        /// <summary>
        /// Enumを追加
        /// </summary>
        public CSharpCodeBuilder AddEnums( IEnumerable<(string Name , string Value , string Comment,string attribute )> members )
        {
            var maxLength = members.Max( m => m.Name.Length );
            
            foreach (var member in members)
            {

                if (string.IsNullOrEmpty(member.attribute) is false)
                {
                    AppendLine($"[{member.attribute}]");
                }
                if (string.IsNullOrEmpty(member.Comment))
                {
                    AppendLine($"{member.Name.PadRight(maxLength)} = {member.Value.ToString()},");
                }
                else
                {
                    AppendLine($"{member.Name.PadRight(maxLength)} = {(member.Value.ToString()+",").PadRight(12)} // {member.Comment}");
                }
            }
            return this;
        }
        
        /// <summary>
        /// コメントを追加
        /// </summary>
        public CSharpCodeBuilder AddComment(string comment)
        {
            AppendLine($"// {comment}");
            return this;
        }

        /// <summary>
        /// XMLドキュメントコメントを追加
        /// </summary>
        public CSharpCodeBuilder AddSummary( params string[] summarys)
        {
            AppendLine("/// <summary>");
            foreach (var summary in summarys)
            {
                var lines = summary.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    AppendLine($"/// {line}");
                }
            }
            AppendLine("/// </summary>");
            return this;
        }

        public CSharpCodeBuilder AddSingleLineSummary( string summary)
        {
            AppendLine($"/// <summary> {summary} </summary>");
            return this;
        }

        /// <summary>
        /// 生成されたコードを取得
        /// </summary>
        public override string ToString()
        {
            return _sb.ToString();
        }

        public class BlockScope : IDisposable
        {
            private CSharpCodeBuilder builder;
            private bool enableScope;
            private string footer;
            public BlockScope(CSharpCodeBuilder builder , bool enableScope = true)
            {
                this.builder = builder;
                this.enableScope = enableScope;
            }

            public BlockScope Footer( string footer )
            {
                this.footer = footer;
                return this;
            }
            
            public void Dispose()
            {
                if (enableScope)
                {
                    builder.EndBlock(footer);
                }
            }
        }
        
        public class CompilerScope : IDisposable
        {
            private CSharpCodeBuilder builder;
            public string endLine;
            public CompilerScope(CSharpCodeBuilder builder , string end  )
            {
                this.builder = builder;
                endLine = end;
            }
            
            public void Dispose()
            {
                builder.AppendLineNoIndent(endLine);
            }
        }        
    }
}
