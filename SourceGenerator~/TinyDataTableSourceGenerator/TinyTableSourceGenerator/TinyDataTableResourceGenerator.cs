using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
                                            (t is ClassDeclarationSyntax || t is StructDeclarationSyntax) && 
                                            t.AttributeLists.Count > 0,
                    transform: (ctx, _) => GetSemanticTargetForGeneration(ctx,"TinyDataTable.IDAttribute"))
                .Where(target => target != null);
            
            // ソースコード生成（StringBuilderで入れ子構造を構築）
            context.RegisterSourceOutput(typeDeclarations, (spc, typeDef) =>
            {
                if (typeDef == null) return;

                var cb = new CSharpCodeBuilder();
                
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
                    cb.BeginBlock($"namespace {typeDef.NamespaceName}" );
                }

                // 外側の親クラス群を順番にネストしていく
                foreach (var outer in typeDef.OuterTypes)
                {
                    cb.BeginBlock($"public partial {outer.TypeKeyword} {outer.TypeName}");
                }
                
                // ③ ターゲット自身の型定義
                var idTypeName = typeDef.TypeName;
                var recordTypeName = typeDef.attributeArgs[0].Value?.ToString() ?? string.Empty;
                var schemaTypeName = $"{recordTypeName}.Schema";
                var enumTypeName = typeDef.attributeArgs[1].Value?.ToString() ?? string.Empty;
                var enumNames = GetEnumNames(typeDef.attributeArgs[1]);

                //クラススコープ
                using( cb.BeginScope($"public partial {typeDef.TypeKeyword} {idTypeName} :  IEquatable<{idTypeName}>, IEquatable<{enumTypeName}>") )
                {
                    //メンバー
                    cb.AddComment("Member");
                    cb.AddAttribute("SerializeField");
                    cb.AddField($"{enumTypeName}", "_value", "private");
                    cb.AddAttribute("NonSerialized");
                    cb.AddField("int", "_index", "private");
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
                               "public", "this(value, EnumToIndex(value))"))
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
                                 .Where(t => string.IsNullOrEmpty(t.name) is false))
                    {
                        if (en.obsolete)
                        {
                            cb.AppendLine("[Obsolete]");
                        }
                        cb.AddCode($"public static readonly {idTypeName} {en.name} = new ({enumTypeName}.{en.name}, {en.index})");
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

                                cb.AddCode("_index = EnumToIndex(_value)");
                            }

                            cb.AddCode("return _index");
                        }
                    }
                    
                    //ValidIDList.Lengthで代用できるのでとりあえずオミット
                    //                    cb.AddComment("Size of record");
                    //                    cb.AddCode($"public static int Size => {data.Header.RowData.Length}");
                    cb.AddComment("Is this record is valid");
                    cb.AddCode($"public bool IsValid => Index != 0");
                    cb.AddComment("Is this record is invalid");
                    cb.AddCode($"public bool IsInvalid => Index == 0");
                    cb.AppendLine();                    
                    
                    //演算子オペレーター
                    cb.AddComment("Operators");
                    cb.AppendLine($"public bool Equals({idTypeName} other) => _value == other._value;");
                    cb.AppendLine($"public bool Equals({enumTypeName} other) => _value == other;");
                    cb.AppendLine($"public override bool Equals(object obj) => obj is ID other && Equals(other);");
                    
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
                    
                    //EnumをIndexに変換するメソッド（静的にテーブル展開されるので高速）
                    cb.AddComment("Enum to index");
                    using (cb.BeginScope($"private static int EnumToIndex({enumTypeName} value) => value switch").Footer(";"))
                    {
                        foreach (var en in enumNames
                                     .Where(t=> string.IsNullOrEmpty(t.name) is false && t.obsolete is false))
                        {
                            cb.AppendLine($"{enumTypeName}.{en.name} => {en.index},");
                        }
                        cb.AppendLine($"_ => 0");
                    }                    
                        
                    //静的テーブル
                    cb.AddComment("static id table");
                    var valids = enumNames.Where( t => t.obsolete is false && t.value > 0 && string.IsNullOrEmpty(t.name) is false );
                    if (valids.Any())
                    {
                        using (cb.BeginScope($"private static readonly IReadOnlyCollection<{idTypeName}> ValidIDList = new[]").Footer(";"))
                        {
                            foreach (var valid in valids)
                            {
                                cb.AppendLine($"{idTypeName}.{valid.name},");
                            }
                        }
                    }
                    else
                    {
                        cb.AddCode(($"private static readonly IReadOnlyCollection<{idTypeName}> ValidIDList = Array.Empty<{idTypeName}>()"));
                    }
                    cb.AppendLine();                             
                    
                    cb.AddComment("static valid enum table");
                    var validEnum = enumNames.Where(f => f.obsolete is false && f.value != 0);
                    if ( validEnum.Any())
                    {
                        using (cb.BeginScope($"private static readonly IReadOnlyCollection<{enumTypeName}> ValidEnumList = new[]").Footer(";"))
                        {
                            foreach (var valid in validEnum)
                            {
                                cb.AppendLine($"{enumTypeName}.{valid.name},");
                            }
                        }
                    }
                    else
                    {
                        cb.AddCode(($"private static readonly IReadOnlyCollection<{enumTypeName}> ValidEnumList = Array.Empty<{enumTypeName}>()"));
                    }
                    
                    // ④ メソッドの実装
                    cb.BeginBlock($"public void Dump()");
                    cb.AppendLine($"UnityEngine.Debug.Log(\"[TinyTable] This is {typeDef.TypeName} { string.Join( "," , enumNames.Select(f=>f.name) )} data.\");");
                    cb.EndBlock();
                }
                
                // 外側の親クラス群の閉じカッコ
                foreach (var outer in typeDef.OuterTypes)
                {
                    cb.EndBlock();
                }                
                
                //名前空間を閉じる
                if (hasNamespace)
                {
                    
                    cb.EndBlock();
                }

                // ファイル名が衝突しないように、親クラス名も巻き込んだファイル名にする
                string fileNameHint = typeDef.OuterTypes.Count > 0
                    ? string.Join("_", typeDef.OuterTypes.Select(o => o.TypeName)) + "_" + typeDef.TypeName
                    : typeDef.TypeName;

                spc.AddSource($"{fileNameHint}_TinyTable.g.cs", SourceText.From(cb.ToString(), Encoding.UTF8));
            });
        }

        private static TypeDefinition GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx , string attributeName )
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

            // 💡【新機能】親の型（外側のクラス）を最上層まで遡ってリストアップする
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

            return new TypeDefinition
            {
                NamespaceName = namespaceName,
                TypeName = symbol.Name,
                TypeKeyword = typeKeyword,
                attributeArgs = attributeArgs,
                OuterTypes = outerTypes
            };
        }
        
        private static (string name,int value ,int index,bool obsolete)[] GetEnumNames(TypedConstant typedConstant)
        {
            var enumMembers = new List<(string all,int value , int index ,bool obsolete)>();
            if (typedConstant.Value is INamedTypeSymbol enumTypeSymbol && 
                enumTypeSymbol.TypeKind == TypeKind.Enum)
            {
                int index = 0;
                foreach (var field in enumTypeSymbol.GetMembers().OfType<IFieldSymbol>())
                {
                    var obsolate = field.GetAttributes().FirstOrDefault(attr => 
                        (attr.AttributeClass?.ToDisplayString() ?? "").EndsWith("ObsoleteAttribute") );

                    enumMembers.Add((field.Name, (field.ConstantValue is int) ? (int)field.ConstantValue : 0,index, obsolate != null ) );
                    index++;
                }
            }
            return enumMembers.ToArray();
        }
    }
    
    internal class TypeDefinition
    {
        public string NamespaceName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string TypeKeyword { get; set; } = string.Empty;
        public TypedConstant[] attributeArgs { get; set; } = Array.Empty<TypedConstant>();
        public List<OuterTypeInfo> OuterTypes { get; set; } = new List<OuterTypeInfo>();
    }

    internal class OuterTypeInfo
    {
        public string TypeName { get; set; } = string.Empty;
        public string TypeKeyword { get; set; } = string.Empty;
    }
}