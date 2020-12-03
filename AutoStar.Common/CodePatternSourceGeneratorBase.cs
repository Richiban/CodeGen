using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Richiban.AutoStar.Common;
using Richiban.AutoStar.Model;

namespace Richiban.AutoStar.BuilderPattern
{
    public abstract class CodePatternSourceGeneratorBase : ISourceGenerator
    {
        protected abstract string GeneratorName { get; }

        public void Execute(GeneratorExecutionContext context)
        {
            InjectAttribute(context);

            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                var usings = (root as CompilationUnitSyntax)!.Usings.ToString();

                foreach (var classDeclaration in GetCandidateClasses(root))
                {
                    var namespaceName = classDeclaration
                            .FindParent<NamespaceDeclarationSyntax>()
                            ?.Name.ToString();

                    GeneratePatternFor(classDeclaration, usings, namespaceName).AddTo(context);
                }
            }
        }

        private IEnumerable<ClassDeclarationSyntax> GetCandidateClasses(SyntaxNode root)
        {
            return root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => 
                    c.AttributeLists
                     .SelectMany(list => list.Attributes)
                     .Any(attr => attr.Name.ToString() == AttributeToInject.TypeDeclaration.Name
                               || attr.Name.ToString() == GeneratorName));
        }

        private void InjectAttribute(GeneratorExecutionContext context)
        {
            var attributeToInject = new GeneratedFile(AttributeToInject, GeneratorName);

            attributeToInject.AddTo(context);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }

        public virtual TypeFile AttributeToInject =>
            new TypeFile("using System;\r\n",
                new ClassDeclaration(GeneratorName + "Attribute")
                {
                    Attribute = new AttributeA("AttributeUsage")
                    {
                        Arguments = new[] { "AttributeTargets.Class", "Inherited = true", "AllowMultiple = false" }
                    },
                    BaseClass = "Attribute"
                });

        public abstract GeneratedFile GeneratePatternFor(ClassDeclarationSyntax classDeclaration,  string usings, string @namespace);
    }
}