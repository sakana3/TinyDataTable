using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TinyDataTable.SourceGenerator;

namespace TinyTable.SourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    internal class TinyDataTableResourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 対象の抽出
            var typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (node, _) => node is TypeDeclarationSyntax t &&
                                            (t is ClassDeclarationSyntax ) &&
                                            t.AttributeLists.Count > 0,
                    transform: (ctx, _) => GetSemanticTargetForGeneration(ctx, "TinyDataTable.RecordAttribute"))
                .Where(target => target != null);

            // ソースコード生成（StringBuilderで入れ子構造を構築）
            context.RegisterSourceOutput(typeDeclarations, (spc, typeDef) =>
            {
                if (typeDef == null) return;

                var cb = new CSharpCodeBuilder();

                cb.AddComment("This code was generate by TinyDataTableSourceGenerator");

                cb.AppendLine("#pragma warning disable CS0612");
                cb.AddUsing("System");
                cb.AddUsing("System.Collections.Generic");
                cb.AddUsing("System.Runtime.CompilerServices");
                cb.AddUsing("System.Linq");
                cb.AddUsing("UnityEngine");
                cb.AddUsing("TinyDataTable");
                cb.AppendLineNoIndent("#if UNITY_EDITOR");
                cb.AddUsing("DescriptionAttribute = System.ComponentModel.DescriptionAttribute");
                cb.AppendLineNoIndent("#else");
                cb.AddUsing("DescriptionAttribute = TinyDataTable.Description.DescriptionAttribute");
                cb.AppendLineNoIndent("#endif");
                cb.AppendLine();


                // 名前空間の開始
                bool hasNamespace = !string.IsNullOrEmpty(typeDef.NamespaceName);
                if (hasNamespace)
                {
                    cb.BeginBlock($"namespace {typeDef.NamespaceName}");
                }

                // 外側の親クラス群を順番にネストしていく
                /*
                foreach (var outer in typeDef.OuterTypes)
                {
                    cb.AddCommentBlock($"Class {outer.TypeName}");
                    cb.BeginBlock($"public partial {outer.TypeKeyword} {outer.TypeName}");
                }
                */

                // 型名定義
                var recordTypeName = typeDef.TypeName;
                var schemaTypeName = typeDef.attributeArgs[0].Value?.ToString() ?? string.Empty;
                if (typeDef.attributeArgs[0].Kind == TypedConstantKind.Type &&
                    typeDef.attributeArgs[0].Value is INamedTypeSymbol scemaTypeSymbol)
                {
                    schemaTypeName = scemaTypeSymbol.ToMinimalDisplayString(typeDef.SemanticModel, typeDef.SpanStart);
                }

                var schemaFields = GetFieldInfo(typeDef.attributeArgs[0]);
                var enumTypeName = typeDef.attributeArgs[1].Value?.ToString() ?? string.Empty;
                if (typeDef.attributeArgs[1].Kind == TypedConstantKind.Type &&
                    typeDef.attributeArgs[1].Value is INamedTypeSymbol enumTypeSymbol)
                {
                    enumTypeName = enumTypeSymbol.ToMinimalDisplayString(typeDef.SemanticModel, typeDef.SpanStart);
                }

                var enumNames = GetEnumNames(typeDef.attributeArgs[1]);

                var idTypeConstant = typeDef.attributeArgs[2];
                var idTypeName = idTypeConstant.Value?.ToString() ?? string.Empty;
                if (idTypeConstant.Kind == TypedConstantKind.Type &&
                    idTypeConstant.Value is INamedTypeSymbol idTypeSymbol)
                {
                    idTypeName = idTypeSymbol.ToMinimalDisplayString(typeDef.SemanticModel, typeDef.SpanStart);
                }


                using (cb.BeginClass($"{recordTypeName}", isPartial: true))
                {
                    // Valid Enum Table
                    cb.AddComment("static valid enum table");
                    {
                        var validEnum = enumNames.Where(f => f.IsObsolete is false && f.Value != 0);
                        if (validEnum.Any())
                        {
                            using (cb.BeginScope(
                                           $"public static readonly IReadOnlyList<{enumTypeName}> ValidEnumList = new[]")
                                       .Footer(";"))
                            {
                                foreach (var valid in validEnum)
                                {
                                    cb.AppendLine($"{enumTypeName}.{valid.Name},");
                                }
                            }
                        }
                        else
                        {
                            cb.AddCode(
                                ($"private static readonly IReadOnlyList<{enumTypeName}> ValidEnumList = Array.Empty<{enumTypeName}>()"));
                        }
                    }


                    //静的テーブル
                    cb.AddComment("static valid id table");
                    {
                        var valids = enumNames.Where(t =>
                            t.IsObsolete is false && t.Value > 0 && string.IsNullOrEmpty(t.Name) is false);
                        if (valids.Any())
                        {
                            using (cb.BeginScope(
                                           $"public static readonly IReadOnlyList<{idTypeName}> ValidIDList = new[]")
                                       .Footer(";"))
                            {
                                foreach (var valid in valids)
                                {
                                    cb.AppendLine($"{idTypeName}.{valid.Name},");
                                }
                            }
                        }
                        else
                        {
                            cb.AddCode(
                                ($"private static readonly IReadOnlyList<{idTypeName}> ValidIDList = Array.Empty<{idTypeName}>()"));
                        }

                        cb.AppendLine();
                    }

                    //EnumをIndexに変換するメソッド（静的にテーブル展開されるので高速）
                    cb.AddComment("Enum to index");
                    using (cb.BeginScope($"private static int ToIndex({enumTypeName} value) => value switch")
                               .Footer(";"))
                    {
                        foreach (var en in enumNames
                                     .Where(t => string.IsNullOrEmpty(t.Name) is false && t.IsObsolete is false))
                        {
                            cb.AppendLine($"{enumTypeName}.{en.Name} => {en.ArrayIndex},");
                        }

                        cb.AppendLine($"_ => 0");
                    }

                    cb.AppendLine();
                    cb.AddComment("Enum indexer");
                    cb.AppendLine(
                        $"public {schemaTypeName} this[{enumTypeName} enumValue] => Records[ToIndex(enumValue)];");
                    cb.AppendLine();

                    //クラススコープ
                    cb.AddCommentBlock("Record Class");


                    using (cb.BeginScope(
                               $"public partial struct {idTypeName} : IIdentifier, IEquatable<{idTypeName}>, IEquatable<{enumTypeName}>"))
                    {
                        //メンバー
                        cb.AddComment("Member");
                        cb.AddAttribute("SerializeField");
                        cb.AddField($"{enumTypeName}", "_value", "private");
                        cb.AddAttribute("NonSerialized");
                        cb.AddField("int", "_index", "private");
                        cb.AppendLine();

                        //フィールドプロパティ
                        //関数呼び出しを避けるためにインラインで３項演算子を使う
                        cb.AddComment($"filed properties");
                        foreach (var field in schemaFields)
                        {
                            var left = $"public {field.FieldType} {field.FieldName}";
                            var right = $"_recordArray[Index].{field.FieldName}";
#if true
                            cb.AddCode($"{left} => {right}");
#else
                            using (cb.BeginBlock($"public {typename} {field.name}"))
                            {
                                cb.AppendLine("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");      
                                cb.AddCode($"get => {right}");
                            }
#endif
                        }

                        cb.AppendLine();

                        //コンストラクター
                        cb.AddComment("Constructor");
                        using (cb.BeginConstructor(idTypeName, $"{enumTypeName} value, int index", "private"))
                        {
                            cb.AddCode("this._value = value");
                            cb.AddCode("this._index = index");
                        }

                        cb.AppendLine();
                        cb.AddComment("Constructor");
                        using (cb.BeginConstructor(idTypeName, $"{enumTypeName} value",
                                   "public", $"this(value, {recordTypeName}.ToIndex(value))"))
                        {
                        }

                        cb.AppendLine();

                        cb.AddComment("Constructor");
                        using (cb.BeginConstructor(idTypeName, $"{idTypeName} value",
                                   "public", "this(value._value, value._index)"))
                        {
                        }

                        cb.AppendLine();

                        //プライベートメンバー
                        cb.AddComment("Private member");
                        using (cb.BeginScope($"private static {schemaTypeName}[] _recordArray"))
                        {
                            cb.AddAttribute("MethodImpl(MethodImplOptions.AggressiveInlining)");
                            cb.AddCode($"get => {recordTypeName}.Instance.Records");
                        }

                        cb.AppendLine();

                        cb.AddComment("static propieries");
                        foreach (var en in enumNames
                                     .Where(t => string.IsNullOrEmpty(t.Name) is false))
                        {
                            if (en.IsObsolete && en.IsMissing)
                            {
                                cb.AppendLine("[Missing,Obsolete]");
                            }
                            else if (en.IsObsolete)
                            {
                                cb.AppendLine("[Obsolete]");
                            }
                            else if (en.IsMissing)
                            {
                                cb.AppendLine("[Missing]");
                            }

                            if (en.ArrayIndex >= 0)
                            {
                                cb.AddCode($"public static readonly {idTypeName} {en.Name} = new ({enumTypeName}.{en.Name}, {en.ArrayIndex})");
                            }
                            else
                            {
                                cb.AddCode($"public static readonly {idTypeName} {en.Name} = new ({enumTypeName}.{en.Name})");
                            }
                        }

                        cb.AppendLine();

                        //Index
                        cb.AddComment("Index of this ID");
                        using (cb.BeginScope("public int Index"))
                        {
                            cb.AddAttribute("MethodImpl(MethodImplOptions.AggressiveInlining)");
                            using (cb.BeginScope("get"))
                            {
                                using (cb.BeginIf("_index == 0"))
                                {
                                    using (cb.BeginIf($"_value == 0"))
                                    {
                                        cb.AddCode("return 0");
                                    }

                                    cb.AddCode($"_index = {recordTypeName}.ToIndex(_value)");
                                }

                                cb.AddCode("return _index");
                            }
                        }

                        //ValidIDList.Lengthで代用できるのでとりあえずオミット
                        //                    cb.AddComment("Size of record");
                        //                    cb.AddCode($"public static int Size => {data.Header.RowData.Length}");
                        cb.AddComment("If this record is valid");
                        cb.AddCode($"public bool IsValid => Index != 0");
                        cb.AddComment("If this record is invalid");
                        cb.AddCode($"public bool IsInvalid => Index == 0");
                        cb.AppendLine();

                        //演算子オペレーター
                        cb.AddComment("Operators");
                        cb.AppendLine($"public bool Equals({idTypeName} other) => EqualityComparer<Enum>.Default.Equals(_value, other._value);");
                        cb.AppendLine($"public bool Equals({enumTypeName} other) => EqualityComparer<Enum>.Default.Equals(_value, other);");
                        cb.AppendLine($"public override bool Equals(object other) => (other is ID id) ? Equals(id) : (other is Enum en) ? Equals(en) : false;");

                        cb.AppendLine($"public static bool operator ==({idTypeName} left, {idTypeName} right) => left.Equals(right);");
                        cb.AppendLine($"public static bool operator !=({idTypeName} left, {idTypeName} right) => !left.Equals(right);");
                        cb.AppendLine($"public static bool operator ==({idTypeName} left, {enumTypeName} right) => left.Equals(right);");
                        cb.AppendLine($"public static bool operator !=({idTypeName} left, {enumTypeName} right) => !left.Equals(right);");
                        cb.AppendLine($"public static bool operator ==({enumTypeName} left, {idTypeName} right) => right.Equals(left);");
                        cb.AppendLine($"public static bool operator !=({enumTypeName} left, {idTypeName} right) => !right.Equals(left);");

                        cb.AppendLine($"public static implicit operator {idTypeName}({enumTypeName} value) => new {idTypeName}(value);");
                        cb.AppendLine($"public static implicit operator {enumTypeName}({idTypeName} value) => value._value;");

                        cb.AppendLine($"public override int GetHashCode() => (int)_value;");
                        cb.AppendLine($"public override string ToString() => _value.ToString();");

/*
                    cb.BeginBlock($"public void Dump()");
                    cb.AppendLine($"UnityEngine.Debug.Log(\"[TinyTable] {typeDef.TypeName} { string.Join( "," , enumNames.Select(f=>f.name) )} \");");
                    cb.AppendLine($"UnityEngine.Debug.Log(\"[TinyTable] {typeDef.TypeName} { string.Join( "," , fields.Select(f=>f.FieldName) )} \");");
                    cb.EndBlock();
*/
                    }

                    cb.AppendLine();
                    cb.AppendLineNoIndent("#if UNITY_EDITOR");

                    cb.AddComment($"{idTypeName} Editor Part");
                    using (cb.BeginScope($"public partial struct {idTypeName} : ISerializationCallbackReceiver"))
                    {
                        cb.AppendLine($"void ISerializationCallbackReceiver.OnAfterDeserialize() => _index = {recordTypeName}.ToIndex(_value);");
                        cb.AppendLine("void ISerializationCallbackReceiver.OnBeforeSerialize(){}");

                    }

                    //Editor Infos
                    cb.AddComment("Editor Infos");
                    using (cb.BeginScope($"private static class __editorMetaData"))
                    {
                        cb.AddComment("ID Code");
                        cb.AppendLine($"private const string CodeTextMetaData = {GetImplementationCodeFromTypeConstant(idTypeConstant).ToLiteral()};");
                        cb.AddComment("Using Namespaces");
                        cb.AppendLine($"private static readonly string[] UsingNamespaceMetaData = new string[] {{ {string.Join(",", typeDef.UsingNamespaces)} }};");
                        //Attribute MetaData
                        cb.AddComment("Field Attribute MetaData");
                        cb.AppendLine("private static readonly Dictionary<string,(Type,string)[]> FieldAttributesMetaData = new () {");
                        cb.IncIndent();
                        foreach (var field in schemaFields)
                        {
                            var attr =
                                $"{string.Join(",", field.Attributes.Select(a => $"(typeof({a.TypeName}),{a.Code})"))}";
                            if (attr.Length > 0)
                            {
                                cb.AppendLine($"{{{field.FieldName.ToLiteral()}, new[] {{{attr}}} }},");
                            }
                            else
                            {
                                cb.AppendLine($"{{{field.FieldName.ToLiteral()},Array.Empty<(Type,string)>()}},");
                            }
                        }

                        cb.DecIndent();
                        cb.AppendLine("};");
                    }

                    cb.AppendLineNoIndent("#endif");

                }
#if false
                    foreach (var outer in typeDef.OuterTypes)
                    {
                        cb.EndBlock();
                    }
#endif
                if (hasNamespace)
                {
                    cb.EndBlock();
                }

                string fileNameHint = typeDef.OuterTypes.Count > 0
                    ? string.Join("_", typeDef.OuterTypes.Select(o => o.TypeName)) + "_" + typeDef.TypeName
                    : typeDef.TypeName;

                spc.AddSource($"{fileNameHint}_TinyDataTable.g.cs", SourceText.From(cb.ToString(), Encoding.UTF8));
            });
        }


        private static TypeDefinition? GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx, string attributeName)
        {
            var typeDeclaration = (TypeDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
            if (symbol == null) return null;

            var attributeData = symbol.GetAttributes().FirstOrDefault(attr =>
                attr.AttributeClass?.ToDisplayString() == attributeName);

            if (attributeData == null) return null;

            TypedConstant[] attributeArgs = attributeData.ConstructorArguments
                .ToArray();

            var namespaceName = symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : symbol.ContainingNamespace.ToDisplayString();

            string typeKeyword = typeDeclaration is StructDeclarationSyntax ? "struct" : "class";

            var outerTypes = new List<OuterTypeInfo>();
            var currentContainingType = symbol.ContainingType;
            while (currentContainingType != null)
            {
                outerTypes.Add(new OuterTypeInfo
                {
                    TypeName = currentContainingType.Name,
                    TypeKeyword = currentContainingType.IsValueType ? "struct" : "class"
                });
                currentContainingType = currentContainingType.ContainingType;
            }

            outerTypes.Reverse(); // 外側から順に並ぶように反転

            var compilationUnit = typeDeclaration.SyntaxTree.GetRoot() as CompilationUnitSyntax;
            return new TypeDefinition
            {
                SemanticModel = ctx.SemanticModel,
                SpanStart = typeDeclaration.SpanStart,
                NamespaceName = namespaceName,
                TypeName = symbol.Name,
                TypeKeyword = typeKeyword,
                attributeArgs = attributeArgs,
                OuterTypes = outerTypes,
                CodeText = string.Concat(typeDeclaration.Members.Select(member => member.ToFullString())),
                UsingNamespaces = compilationUnit?.Usings
                    .Where(u => u.Name.ToString().EndsWith("DescriptionAttribute") is false)
                    .Select(u => u.ToString().ToLiteral())
                    .ToArray() ?? Array.Empty<string>(),
            };
        }

        private static EnumDefinition[] GetEnumNames(TypedConstant typedConstant)
        {
            var enumMembers = new List<EnumDefinition>();
            if (typedConstant.Value is INamedTypeSymbol enumTypeSymbol &&
                enumTypeSymbol.TypeKind == TypeKind.Enum)
            {
                int index = 0;
                foreach (var field in enumTypeSymbol.GetMembers().OfType<IFieldSymbol>())
                {
                    var obsolate = field.GetAttributes().FirstOrDefault(attr =>
                        (attr.AttributeClass?.ToDisplayString() ?? "").EndsWith("ObsoleteAttribute"));
                    var missing = field.GetAttributes().FirstOrDefault(attr =>
                        (attr.AttributeClass?.ToDisplayString() ?? "").EndsWith("MissingAttribute"));

                    var indexAttribute = field.GetAttributes().FirstOrDefault(attr =>
                        attr.AttributeClass?.ToDisplayString() == "TinyDataTable.EnumIndexAttribute");
/*
                    if (indexAttribute == null)
                    {
                        return enumMembers.ToArray();
                    }
*/
                    int arrayIndex = indexAttribute == null ? -1 : indexAttribute.ConstructorArguments.FirstOrDefault().Value as int? ?? 0;

                    var enumDefinition = new EnumDefinition()
                    {
                        Name = field.Name,
                        Value = (field.ConstantValue is int) ? (int)field.ConstantValue : 0,
                        Index = index,
                        ArrayIndex = arrayIndex,
                        IsObsolete = obsolate != null,
                        IsMissing = obsolate != null
                    };
                    enumMembers.Add(enumDefinition);
                    index++;
                }
            }

            return enumMembers.ToArray();
        }

        private static FieldDefinition[] GetFieldInfo(TypedConstant typedConstant)
        {
            var fieldList = new List<FieldDefinition>();
            if (typedConstant.Kind == TypedConstantKind.Type)
            {
                if (typedConstant.Value is INamedTypeSymbol targetTypeSymbol)
                {
                    foreach (var fieldSymbol in targetTypeSymbol.GetMembers().OfType<IFieldSymbol>())
                    {
                        if (fieldSymbol.IsImplicitlyDeclared) continue;

                        var tiny = fieldSymbol.GetAttributes().Any(attr =>
                            (attr.AttributeClass?.ToDisplayString() ?? "").EndsWith("TINYAttribute"));

                        if (tiny)
                        {
                            fieldList.Add(new FieldDefinition
                            {
                                FieldName = fieldSymbol.Name,
                                FieldType = fieldSymbol.Type.ToDisplayString(),
                                Accessibility = fieldSymbol.DeclaredAccessibility.ToString(),
                                Attributes = fieldSymbol.GetAttributes()
                                    .Where(attr =>
                                        (attr.AttributeClass?.ToDisplayString() ?? "").EndsWith("TINYAttribute") is
                                        false)
                                    .Where(attr =>
                                        (attr.AttributeClass?.ToDisplayString() ?? "").EndsWith("ObsoleteAttribute") is
                                        false)
                                    .Where(attr =>
                                        (attr.AttributeClass?.ToDisplayString() ?? "").EndsWith("DescriptionAttribute")
                                        is false)
                                    .Select(attr => AttributeDefinition.Form(attr))
                                    .ToArray()
                            });
                        }
                    }
                }
            }

            return fieldList.ToArray();
        }
        
        /// <summary>
        /// TypedConstantが表す型（クラスまたは構造体）のメンバー定義のソースコードを取得します。
        /// この関数は、メンバーが存在しない場合でも、ブロック内に書かれたコメントを取得します。
        /// </summary>
        /// <param name="typedConstant">型情報を持つTypedConstant。</param>
        /// <returns>
        /// メンバーおよびコメントを含むソースコード文字列。
        /// 型が外部ライブラリ（DLL）で定義されているなど、ソースコードにアクセスできない場合は、
        /// その理由を示すコメント文字列を返します。
        /// </returns>
        private static string GetImplementationCodeFromTypeConstant(TypedConstant typedConstant)
        {
            if (typedConstant.Kind != TypedConstantKind.Type || typedConstant.Value is not ITypeSymbol typeSymbol)
            {
                return string.Empty;
            }

            if (!typeSymbol.DeclaringSyntaxReferences.Any())
            {
                return string.Empty;
            }

            // 3. 構文ノードの取得
            SyntaxReference? syntaxRef = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
            {
                return string.Empty;
            }

            SyntaxNode syntaxNode = syntaxRef.GetSyntax();

            if (syntaxNode is TypeDeclarationSyntax typeDeclarationSyntax)
            {
                // ファイル全体のソーステキストを取得
                var sourceText = typeDeclarationSyntax.SyntaxTree.GetText();

                // 開始および終了波括弧の行番号（0-indexed）を取得
                int openBraceLine = sourceText.Lines.IndexOf(typeDeclarationSyntax.OpenBraceToken.SpanStart);
                int closeBraceLine = sourceText.Lines.IndexOf(typeDeclarationSyntax.CloseBraceToken.SpanStart);

                // 抽出する行の範囲を定義（波括弧のある行の「次」から「前」まで）
                int startLine = openBraceLine + 1;
                int endLine = closeBraceLine - 1;

                // 波括弧の間に有効な行が存在しない場合は、空文字列を返す
                if (startLine > endLine)
                {
                    return string.Empty;
                }
                // 抽出範囲の開始位置（startLineの先頭）と終了位置（endLineの末尾）を特定
                int startPosition = sourceText.Lines[startLine].Start;
                int endPosition = sourceText.Lines[endLine].End;

                // 上記の位置からTextSpanを作成
                var spanToExtract = TextSpan.FromBounds(startPosition, endPosition);

                // TextSpanに該当する部分の文字列を返却
                return sourceText.ToString(spanToExtract);
            }

            return string.Empty;
        }

        private class TypeDefinition
        {
            public SemanticModel SemanticModel { get; set; } = null;
            public int SpanStart { set; get; } = 0;
            public string NamespaceName { get; set; } = string.Empty;
            public string TypeName { get; set; } = string.Empty;
            public string TypeKeyword { get; set; } = string.Empty;
            public TypedConstant[] attributeArgs { get; set; } = Array.Empty<TypedConstant>();
            public List<OuterTypeInfo> OuterTypes { get; set; } = new List<OuterTypeInfo>();
            public string CodeText { get; set; } = string.Empty;
            public string[] UsingNamespaces { get; set; } = Array.Empty<string>();
        }

        private class EnumDefinition
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; } = 0;
            public int Index { get; set; } = 0;
            public int ArrayIndex { get; set; } = 0;
            public bool IsObsolete { get; set; } = false;
            public bool IsMissing { get; set; } = false;
        }

        private class FieldDefinition
        {
            public string FieldName { get; set; } = string.Empty;
            public string FieldType { get; set; } = string.Empty;
            public string Accessibility { get; set; } = string.Empty;
            public bool IsObsolete { get; set; } = false;
            public AttributeDefinition[] Attributes { get; set; } = Array.Empty<AttributeDefinition>();
        }

        private class AttributeDefinition
        {
            public string TypeName { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;

            public static AttributeDefinition Form(AttributeData attr)
            {
                return new AttributeDefinition()
                {
                    TypeName = attr.AttributeClass?.ToDisplayString() ?? "",
                    //                Args = attr.ConstructorArguments.Select(a => a.Value?.ToString() ?? "").ToArray(),
                    Code = GetRawAttributeText(attr).ToLiteral(),
                };
            }

            private static string GetRawAttributeText(AttributeData attributeData)
            {
                SyntaxReference? syntaxRef = attributeData.ApplicationSyntaxReference;
                if (syntaxRef == null)
                {
                    return null;
                }

                SyntaxNode syntaxNode = syntaxRef.GetSyntax();
                return syntaxNode.ToString();
            }
        }

        private class OuterTypeInfo
        {
            public string TypeName { get; set; } = string.Empty;
            public string TypeKeyword { get; set; } = string.Empty;
        }
    }
}


public static class StringExtensions
{
    /// <summary>
    /// 文字列を C# のコードに埋め込めるリテラル表現（"..."）に変換します。
    /// </summary>
    public static string ToLiteral(this string? input)
    {
        if ( input == null) return "null";
        
        return SymbolDisplay.FormatLiteral(input, quote: true);
    }
}