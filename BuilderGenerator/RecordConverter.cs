using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace BuilderGenerator
{
    public class RecordConverter
    {
        public ClassDeclaration RecordToClass(RecordDeclaration record)
        {
            var constructorParams = record.RecordProperties
                .Select(p => new Parameter(p.Name, p.Type))
                .ToArray();

            var constructorAssignments = record.RecordProperties
                .Select(p => new AssignmentStatement($"this.{p.Name}", p.Name))
                .ToArray();

            var validateMethod = new MethodDeclaration(
                "Validate",
                "void",
                Visibility.Public,
                isStatic: false,
                isOverride: false,
                new Parameter[] { },
                new IWriteableCode[] { new Statement("var errors = new System.Collections.Generic.List<string>();") }
                    .Concat(record.RecordProperties
                        .Where(p => p.IsOptional == false)
                        .Select(p => new Statement($"if ({p.Name} is null) errors.Add(\"{p.Name} is null\");")))
                    .Concat(new IWriteableCode[] { new Statement(
                "if (errors.Count > 0) throw new ValidationException(errors);") })
                    .ToArray());

            var buildMethod = new MethodDeclaration(
                "Build",
                record.Name,
                Visibility.Public,
                isStatic: false,
                isOverride: false,
                new Parameter[] { },
                new IWriteableCode[] {
                    new Statement("Validate();"),
                    new ConstructorCall(record.Name, record.RecordProperties.Select(p => p.Name).ToArray()) });

            var builderException = BuildBuilderExceptionDefinition();

            var toStringMethod = BuildToStringMethod(record);

            var builderProps = record.RecordProperties
                .Select(p => new Property(p.Name, p.Type, true, Visibility.Public, p.DefaultValue))
                .Cast<IWriteableCode>()
                .Concat(new IWriteableCode[] { validateMethod, buildMethod, builderException })
                .ToArray();

            var defaultConstructor = new Constructor(
                "Builder",
                Visibility.Public,
                new Parameter[] { },
                new AssignmentStatement[] { });

            var copyConstructor = new Constructor(
                "Builder",
                Visibility.Public,
                new[] { new Parameter("other", record.Name) },
                record.RecordProperties.Select(p => new AssignmentStatement(p.Name, $"other.{p.Name}")).ToArray());

            var builderClass = new ClassDeclaration(
                "Builder",
                new[] { defaultConstructor, copyConstructor }, 
                visibility: Visibility.Public, contents: builderProps);

            var output = new ClassDeclaration(
                record.Name,
                new[] { new Constructor(record.Name, Visibility.Public, constructorParams, constructorAssignments) },
                Visibility.Public,
                null,
                true,
                contents: new IWriteableCode[] { toStringMethod, builderClass });

            return output;
        }

        private MethodDeclaration BuildToStringMethod(RecordDeclaration record)
        {
            var propNames = String.Join($",{Environment.NewLine}", record.RecordProperties.Select(p => $"$\"{p.Name} = {{{p.Name}}}\""));

            var statements = new IWriteableCode[] {
                new Statement($"var elements = new string[] {{ {propNames} }};"),
                new Statement($"var s = System.String.Join(\", \", elements);"),
                new Statement($"return $\"{record.Name} {{{{ {{s}} }}}}\";")
            };

            return new MethodDeclaration(
                "ToString",
                "string",
                Visibility.Public,
                isStatic: false,
                isOverride: true,
                parameters: new Parameter[] { }, 
                contents: statements);
        }

        private static ClassDeclaration BuildBuilderExceptionDefinition()
        {
            return new ClassDeclaration(
                "ValidationException",
                visibility: Visibility.Public,
                contents: new IWriteableCode[] {
                    new Property("Errors", "System.Collections.Generic.IReadOnlyCollection<string>", hasSetter: false, visibility: Visibility.Public, defaultValue: null),
                    new MethodDeclaration(
                        "GetMessage", "string",
                        Visibility.Private,
                        isStatic: true,
                        isOverride: false,
                        parameters: new Parameter[] {new Parameter("errors", "System.Collections.Generic.IReadOnlyCollection<string>") },
                        contents: new IWriteableCode[] { new Statement("return string.Join(System.Environment.NewLine, errors);")}) },
                        baseClass: "System.Exception",
                        constructors: new[] { new Constructor(
                            "ValidationException",
                            Visibility.Public,
                            parameters: new[] { new Parameter("errors", "System.Collections.Generic.IReadOnlyCollection<string>") },
                            baseCall: new[] { "GetMessage(errors)" },
                            constructorAssignments: new[] { new AssignmentStatement("Errors", "errors") })});
        }

        public RecordDeclaration ClassToRecord(ClassDeclarationSyntax classDeclaration)
        {
            var className = classDeclaration.Identifier.Text;

            var props = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(CreateProp)
                .ToList();

            return new RecordDeclaration(className, props);
        }

        private RecordProperty CreateProp(PropertyDeclarationSyntax prop)
        {
            var isOptional = prop.Type is NullableTypeSyntax;
            var defaultValue = prop.Initializer?.ToString();

            return new RecordProperty(
                name: prop.Identifier.ToString(),
                type: prop.Type.ToFullString(),
                isOptional: isOptional,
                defaultValue: defaultValue);
        }
    }
}