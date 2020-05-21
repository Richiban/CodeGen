using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace BuilderGenerator
{
    public class RecordConverter
    {
        public ClassDeclaration RecordToClass(RecordDeclaration record)
        {
            var toStringMethod = BuildToStringMethod(record);
            var equalsMethod = BuildEqualsMethod(record);
            var objectEqualsMethod = BuildObjectEqualsMethod(record);
            var builderClass = GenerateBuilderClass(record);

            var output = GenerateOutputClass(record, toStringMethod, equalsMethod, objectEqualsMethod, builderClass);

            return output;
        }

        private static ClassDeclaration GenerateBuilderClass(RecordDeclaration record)
        {
            var buildMethod = GenerateBuildMethod(record);
            var builderException = BuildBuilderExceptionDefinition();
            var validateMethod = GenerateValidateMethod(record);

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

            return builderClass;
        }

        private static ClassDeclaration GenerateOutputClass(RecordDeclaration record, MethodDeclaration toStringMethod, MethodDeclaration equalsMethod, MethodDeclaration objectEqualsMethod, ClassDeclaration builderClass)
        {
            var constructorParams = record.RecordProperties
                            .Select(p => new Parameter(p.Name, p.Type))
                            .ToArray();

            var constructorAssignments = record.RecordProperties
                .Select(p => new AssignmentStatement($"this.{p.Name}", p.Name))
                .ToArray();

            var output = new ClassDeclaration(
                record.Name,
                new[] { new Constructor(record.Name, Visibility.Public, constructorParams, constructorAssignments) },
                Visibility.Public,
                inheritsImplements: new [] { "" },
                isPartial: true,
                contents: new IWriteableCode[] { toStringMethod, equalsMethod, objectEqualsMethod, builderClass });

            return output;
        }

        private static MethodDeclaration GenerateBuildMethod(RecordDeclaration record) =>
                    new MethodDeclaration(
                        "Build",
                        record.Name,
                        Visibility.Public,
                        isStatic: false,
                        isOverride: false,
                        new Parameter[] { },
                        new IWriteableCode[] {
                    new Statement("Validate();"),
                    new ConstructorCall(record.Name, record.RecordProperties.Select(p => p.Name).ToArray()) });

        private static MethodDeclaration GenerateValidateMethod(RecordDeclaration record) =>
                    new MethodDeclaration(
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

        private static MethodDeclaration BuildToStringMethod(RecordDeclaration record)
        {
            var propNames = String.Join($",{Environment.NewLine}", record.RecordProperties.Select(p => $"$\"{p.Name} = {{{p.Name}}}\""));

            var statements = new IWriteableCode[] {
                new Statement($"var elements = new string[] {{"),
                new Statement($"var elements = new string[] {{ {propNames} }};"),
                new Statement($"}};"),
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

        private static MethodDeclaration BuildEqualsMethod(RecordDeclaration record)
        {
            var propComparisons = record.RecordProperties.Select(p => 
                $"{Environment.NewLine}&& Equals({p.Name}, other.{p.Name}) ");

            var statements = new IWriteableCode[] {
                new Statement($"return !(other is null) {String.Join("", propComparisons)};")
            };

            return new MethodDeclaration(
                "Equals",
                "bool",
                Visibility.Public,
                isStatic: false,
                isOverride: false,
                parameters: new [] { new Parameter("other", record.Name) },
                contents: statements);
        }

        private static MethodDeclaration BuildObjectEqualsMethod(RecordDeclaration record)
        {
            var statements = new IWriteableCode[] {
                new Statement($"return this.Equals(other as {record.Name});")
            };

            return new MethodDeclaration(
                "Equals",
                "bool",
                Visibility.Public,
                isStatic: false,
                isOverride: true,
                parameters: new [] { new Parameter("other", "object") },
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
                        inheritsImplements: new[] { "System.Exception" },
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