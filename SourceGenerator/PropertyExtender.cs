using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace SourceGenerator;

[Generator]
public class PropertyExtender : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
                   predicate: (context, _) => context is ClassDeclarationSyntax classSyntax &&
                   classSyntax.AttributeLists.Any(x => x.Attributes.Any(c => c.Name is GenericNameSyntax { Identifier.ValueText: "Extend" })),
                   transform: (syntax, _) => (ClassDeclarationSyntax)syntax.Node)
                   .Where(x => x is not null);

        var compilation = context.CompilationProvider
           .Combine(provider.Collect());
        context.RegisterSourceOutput(compilation, (spc, source) => Invoke(spc, source.Left, source.Right));
    }

    private void Invoke(SourceProductionContext spc, Compilation _, ImmutableArray<ClassDeclarationSyntax> right)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace SourceGenarator {");
        foreach (var e in right)
        {
            var extender = e.Identifier.ValueText;
            var extendableType = GetExtendName(e);
            if (extendableType is "")
                continue;
            var extendableTypeVarName = extendableType.ToLower();
            sb.AppendLine($"public static partial class {extendableType}Extensions" + "{");
            sb.AppendLine($"private static Dictionary<{extendableType},{extender}> _wrappers = new();");
            foreach (var member in e.Members)
            {
                var memberName = GetMemberName(member);
                var propertyTypeName = GetPropertyTypeName(member);
                if (memberName is "" || propertyTypeName is "")
                    continue;

                sb.AppendLine($"public static {propertyTypeName} {memberName}(this {extendableType} {extendableTypeVarName})" + "{");
                var code = $$"""
                        if(_wrappers.ContainsKey({{extendableTypeVarName}}))
                           return _wrappers[{{extendableTypeVarName}}].{{memberName}};
                        _wrappers[{{extendableTypeVarName}}] = new {{extender}}();
                    return _wrappers[{{extendableTypeVarName}}].{{memberName}};
                    """;
                sb.AppendLine(code);
                sb.AppendLine("}");


                var propertyTypeVarName = "randomName";
                sb.AppendLine($"public static {extendableType} {memberName}(this {extendableType} {extendableTypeVarName}, {propertyTypeName} {propertyTypeVarName})" + "{");
                code = $$"""
                        if(_wrappers.TryGetValue({{extendableTypeVarName}},out var outVal)){
                            outVal.{{memberName}} = {{propertyTypeVarName}};
                            return {{extendableTypeVarName}};
                           }
                        _wrappers[{{extendableTypeVarName}}] = new {{extender}}(){
                            {{memberName}} = {{propertyTypeVarName}}
                        };
                    return {{extendableTypeVarName}};
                    """;
                sb.AppendLine(code);
                sb.AppendLine("}");

            }
            sb.AppendLine("}");
        }
        sb.AppendLine("}");

        spc.AddSource("PropertyExtensionsGenerated.g.cs", sb.ToString());
    }

    private static string GetExtendName(ClassDeclarationSyntax e)
    {
        var attribute = e.AttributeLists
                        .SelectMany(x => x.Attributes)
                        .FirstOrDefault(x => x.Name is GenericNameSyntax { Identifier.ValueText: "Extend" });
        if (attribute is null)
            return string.Empty;
        return GetNameOfGeneric(attribute);
    }

    private static string GetNameOfGeneric(AttributeSyntax attribute)
    {
        var name = (GenericNameSyntax)attribute.Name;
        var val = name.TypeArgumentList?.Arguments.Any(x => x is IdentifierNameSyntax);
        if (!val.HasValue || !val.Value)
            return string.Empty;
        var identifier = name.TypeArgumentList.Arguments
            .Where(x => x is IdentifierNameSyntax)
            .Select(c => (IdentifierNameSyntax)c)
            .First()
            .Identifier;
        return identifier.ValueText;
    }

    private static string GetMemberName(MemberDeclarationSyntax member)
     => member switch
     {
         PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Identifier.ValueText,
         _ => string.Empty
     };

    private static string GetPropertyTypeName(MemberDeclarationSyntax member)
        =>
        member switch
        {
            PropertyDeclarationSyntax property =>
                property.Type switch
                {
                    IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier.ValueText ?? "",
                    PredefinedTypeSyntax predefinedTypeSyntax => predefinedTypeSyntax.Keyword.ValueText,
                    _ => string.Empty
                },
            _ => string.Empty
        };
}
