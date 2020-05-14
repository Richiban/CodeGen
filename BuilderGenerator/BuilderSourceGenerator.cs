using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BuilderCommon;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BuilderGenerator
{
    [Generator]
    public class BuilderSourceGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context)
        {
            // using the context, get a list of syntax trees in the users compilation
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var classBuilders = GenerateBuilder(syntaxTree);

                // add the filepath of each tree to the class we're building
                foreach (var classBuilder in classBuilders)
                {
                    context.AddSource(
                        $"{classBuilder.Key}.Builder.cs",
                        SourceText.From(classBuilder.Value, Encoding.UTF8));
                }
            }

            // inject the created source into the users compilation
        }

        public static Dictionary<string, string> GenerateBuilder(SyntaxTree syntaxTree)
        {
            var classToBuilder = new Dictionary<string, string>();

            var root = syntaxTree.GetRoot();
            var usings = (root as CompilationUnitSyntax).Usings.ToString();

            var classesWithAttribute = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(
                    cds => cds.AttributeLists.HasAttribute(
                        nameof(GenerateBuilderAttribute)))
                .ToList();

            foreach (var classDeclaration in classesWithAttribute)
            {
                var sb = new StringBuilder();

                var namespaceName = classDeclaration
                    .FindParent<NamespaceDeclarationSyntax>()
                    .Name.ToString();

                var className = classDeclaration.Identifier.Text;

                var props = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().Select(m =>
                    new RecordProperty(name: m.Identifier.ToString(), type: m.Type.ToFullString()))
                    .ToList();

                var record = new RecordDeclaration(className, props);

                var cb = new CodeBuilder();

                var recordClass = RecordToClass(record);

                var output = new ClassFile(usings, namespaceName, recordClass);

                output.WriteTo(cb);

                classToBuilder[className] = cb.ToString();
            }

            return classToBuilder;
        }

        public void Initialize(InitializationContext context)
        {
            // No initialization required for this one
        }


        public static ClassDec RecordToClass(RecordDeclaration record)
        {
            var constructorParams = record.RecordProperties
                .Select(p => new Parameter(p.Name, p.Type))
                .ToArray();

            var constructorAssignments = record.RecordProperties
                .Select(p => new AssignmentStatement($"this.{p.Name}", p.Name))
                .ToArray();

            var validateMethod = new Method(
                "Validate",
                "void",
                Visibility.Public,
                false,
                new Parameter[] { },
                new IWriteableCode[] { new Statement("var errors = new System.Collections.Generic.List<string>();") }
                    .Concat(record.RecordProperties.Select(p =>
                        new Statement($"if ({p.Name} is null) errors.Add(\"{p.Name} is null\");")))
                    .Concat(new IWriteableCode[] { new Statement(
                "if (errors.Count > 1) throw new ValidationException(errors);") })
                    .ToArray());

            var buildMethod = new Method(
                "Build",
                record.Name,
                Visibility.Public,
                false,
                new Parameter[] { },
                new IWriteableCode[] {
            new Statement("Validate();"),
            new ConstructorCall(record.Name, record.RecordProperties.Select(p => p.Name).ToArray()) });

            var builderException = new ClassDec(
                "ValidationException",
                visibility: Visibility.Public,
                contents: new IWriteableCode[] {
            new Property("Errors", "System.Collections.Generic.IReadOnlyCollection<string>", hasSetter: false, visibility: Visibility.Public),
            new Method(
                "GetMessage", "string",
                Visibility.Private,
                isStatic: true,
                parameters: new Parameter[] {new Parameter("errors", "System.Collections.Generic.IReadOnlyCollection<string>") },
                contents: new IWriteableCode[] { new Statement("return string.Join(System.Environment.NewLine, errors);")}) },
                baseClass: "System.Exception",
                constructor: new Constructor.BlockConstructor(
                    "ValidationException",
                    Visibility.Public,
                    parameters: new[] { new Parameter("errors", "System.Collections.Generic.IReadOnlyCollection<string>") },
                    baseCall: new[] { "GetMessage(errors)" },
                    constructorAssignments: new[] { new AssignmentStatement("Errors", "errors") }));

            var builderProps = record.RecordProperties
                .Select(p => new Property(p.Name, p.Type, true, Visibility.Public))
                .Cast<IWriteableCode>()
                .Concat(new IWriteableCode[] { validateMethod, buildMethod, builderException })
                .ToArray();

            var builderClass = new ClassDec("Builder", visibility: Visibility.Public, contents: builderProps);

            var output = new ClassDec(
                record.Name,
                new Constructor.BlockConstructor(record.Name, Visibility.Public, constructorParams, constructorAssignments),
                Visibility.Public,
                null,
                true,
                builderClass);

            return output;
        }
    }
}