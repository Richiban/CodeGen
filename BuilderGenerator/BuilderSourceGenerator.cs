using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BuilderGenerator
{
    [Generator]
    public partial class BuilderSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            InjectAttribute(context);

            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                var usings = (root as CompilationUnitSyntax)!.Usings.ToString();

                var classDeclarations = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>();

                foreach (var c in classDeclarations)
                {
                    var srcs = GetEnhancersFor(c)
                        .Select(enhancer => enhancer.GenerateSrcFileContent(c, usings));

                    foreach (var (name, content) in srcs)
                    {
                        context.AddSource($"{name}.cs", SourceText.From(content, Encoding.UTF8));
                    }
                }
            }
        }

        private void InjectAttribute(GeneratorExecutionContext context)
        {
            const string primaryConstructorAttributeText = @"using System;
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class GenerateBuilderAttribute : Attribute
{
}
";
        context.AddSource("GenerateBuilder.g.cs", SourceText.From(primaryConstructorAttributeText, Encoding.UTF8));
        }

        private static IEnumerable<IPatternGenerator> GetEnhancersFor(ClassDeclarationSyntax c)
        {
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}