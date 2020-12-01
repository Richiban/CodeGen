using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using Richiban.CodeGen.Model;

namespace Richiban.CodeGen.AutoConstructor
{
    [Generator]
    public class AutoConstructorGenerator : ISourceGenerator
    {

        private const string ShortAttributeName = "AutoConstructor";

        private const string primaryConstructorAttributeText = @"using System;
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class " + ShortAttributeName + @"Attribute : Attribute
{
}
";
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            InjectPrimaryConstructorAttributes(context);

            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            var classSymbols = GetClassSymbols(context, receiver);

            foreach (var classSymbol in classSymbols)
            {
                context.AddSource($"{classSymbol.Name}.{ShortAttributeName}.g.cs",
                    SourceText.From(CreatePrimaryConstructor(classSymbol), Encoding.UTF8));
            }
        }

        private static string CreatePrimaryConstructor(INamedTypeSymbol classSymbol)
        {
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var fieldList = classSymbol.GetMembers().OfType<IFieldSymbol>()
                .Where(x => x.CanBeReferencedByName && x.IsReadOnly)
                .Select(it => new { Type = it.Type.ToDisplayString(), ParameterName = ToCamelCase(it.Name), it.Name })
                .ToList();

            var parameters = fieldList.Select(it => new Parameter(it.ParameterName, it.Type)).ToList();
            var assignments = fieldList.Select(field => new AssignmentStatement($"this.{field.Name}", field.ParameterName)).ToList();

            var @namespace = new NamespaceDeclaration(namespaceName)
            {
                TypeDeclarations = new[]
                {
                    new ClassDeclaration(classSymbol.Name)
                    {
                        IsPartial = true,
                        Constructor = new Constructor.BlockConstructor(classSymbol.Name)
                        {
                            Parameters = parameters,
                            Statements = assignments
                        }
                    }
                }
            };

            var sourceBuilder = new CodeBuilder();

            @namespace.WriteTo(sourceBuilder);

            return sourceBuilder.ToString();
        }

        private static string ToCamelCase(string name)
        {
            name = name.TrimStart('_');

            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }

        private static List<INamedTypeSymbol> GetClassSymbols(GeneratorExecutionContext context, SyntaxReceiver receiver)
        {
            var options = ((CSharpCompilation)context.Compilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(primaryConstructorAttributeText, Encoding.UTF8), options));

            var attributeSymbol = compilation.GetTypeByMetadataName(ShortAttributeName + "Attribute")!;

            var classSymbols = new List<INamedTypeSymbol>();

            foreach (var @class in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(@class.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(@class)!;

                if (classSymbol.GetAttributes()
                               .Any(ad => ad.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                {
                    classSymbols.Add(classSymbol);
                }
            }

            return classSymbols;
        }

        private static void InjectPrimaryConstructorAttributes(GeneratorExecutionContext context)
        {
            context.AddSource(
                ShortAttributeName + "Attribute.g.cs",
                SourceText.From(primaryConstructorAttributeText, Encoding.UTF8));
        }
    }
}