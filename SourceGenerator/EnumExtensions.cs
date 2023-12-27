using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerator;

[Generator]
public class EnumExtensions : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
                   predicate: (context, _) => context is EnumDeclarationSyntax enumSyntax
                        && enumSyntax.AttributeLists.Any(x => x.Attributes.Any(c => c.Name is IdentifierNameSyntax { Identifier.ValueText: "EnumExtension" })),
                   transform: (syntax, _) => (EnumDeclarationSyntax)syntax.Node)
                   .Where(x => x is not null);

        var compilation = context.CompilationProvider
            .Combine(provider.Collect());
        context.RegisterSourceOutput(compilation, (spc, source) => Invoke(spc, source.Left, source.Right));

    }

    private void Invoke(SourceProductionContext context, Compilation _, ImmutableArray<EnumDeclarationSyntax> typeArray)
    {
        //if (!Debugger.IsAttached) Debugger.Launch();
        var str = new StringBuilder();
        str.AppendLine("namespace SourceGenerator{");
        foreach (var item in typeArray)
        {
            str.AppendLine($"public static class {item.Identifier.ValueText}Extensions");
            str.AppendLine("{");
            ConstructNameExtensions(item, str);
            str.AppendLine("}");
        }



        str.AppendLine("}");
        context.AddSource("EnumExtensionsGenerated.g.cs", str.ToString());
    }

    private static void ConstructNameExtensions(EnumDeclarationSyntax item, StringBuilder sb)
    {
        var mainVar = item.Identifier.ValueText.ToLower();
        var start = $$"""
                public static string GetName(this {{item.Identifier.ValueText}} {{mainVar}})
                {
                   return {{mainVar}} switch {               
                """;
        sb.AppendLine(start);
        foreach (var member in item.Members)
        {
            var code = $"""
                {item.Identifier.ValueText}.{member.Identifier.ValueText} => "{GetName(member)}",
            """;
            sb.AppendLine(code);
        }
        sb.Length--;
        sb.Length--;
        sb.Length--;
        sb.AppendLine("};");
        sb.AppendLine("}");

    }

    private static string GetName(EnumMemberDeclarationSyntax enumMember)
    {
        if (!HasEnumName(enumMember))
            return "";


        var value = enumMember.AttributeLists
            .SelectMany(x => x.Attributes)
            .First(x => x.Name is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "EnumName")
            .ArgumentList.Arguments.Select(x => x.Expression as LiteralExpressionSyntax)
            .Where(x => x is not null)
            .First().Token.ValueText;
        return value;
    }

    private static bool HasEnumName(EnumMemberDeclarationSyntax enumMember)
    {
        return enumMember.AttributeLists.Any(x => x.Attributes.Any(c => c.Name is IdentifierNameSyntax { Identifier.ValueText: "EnumName" }));
    }

}
