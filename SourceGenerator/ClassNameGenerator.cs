using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerator;

[Generator]
public class ClassNameGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (context, _) => context is ClassDeclarationSyntax,
            transform: (syntax, _) => (ClassDeclarationSyntax)syntax.Node)
            .Where(x => x is not null);

        var compilation = context.CompilationProvider
            .Combine(provider.Collect());


        context.RegisterSourceOutput(compilation, (spc, source) => Invoke(spc, source.Left, source.Right));
    }

    private void Invoke(SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> typeArray)
    {
        /// if (!Debugger.IsAttached) Debugger.Launch();
        var strBuilder = new StringBuilder();
        GetClassNames(context, compilation, typeArray, strBuilder);

        if (strBuilder.Length > 0)
            strBuilder.Length--;

        var code = $$"""
               namespace SourceGenerator{
                public static class ClassNames{
                    public static string ClassName = "Hello from Roslyn"; 
                    
                    public static List<String> Names = new List<string>(){
                       {{strBuilder}} 
                    };
                }
               }
            """;
        context.AddSource("ClassNames.g.cs", code);
    }

    private static void GetClassNames(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassDeclarationSyntax> typeArray, StringBuilder strBuilder)
    {
        if (typeArray.Length == 0)
        {
            var desc = new DiagnosticDescriptor("SG0001",
                "No Classes found",
                "No classes declared",
                "Problem",
                DiagnosticSeverity.Warning,
                true);
            context.ReportDiagnostic(Diagnostic.Create(desc, Location.None));
            return;
        }

        foreach (var syntax in typeArray)
        {           
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            var typeInfo = semanticModel.GetTypeInfo(syntax);


            if (semanticModel
                .GetDeclaredSymbol(syntax) is not INamedTypeSymbol symbol) continue;

            var members = symbol.GetMembers();
            var attributes = members.Select(x => x.GetAttributes());

            strBuilder.AppendLine();
            strBuilder.AppendLine($"\"{symbol.ToDisplayString()}\",");
        }


    }
}
