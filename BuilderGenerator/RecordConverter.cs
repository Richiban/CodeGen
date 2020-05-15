using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace BuilderGenerator
{
    public class RecordConverter
    {
        public ClassDec RecordToClass(RecordDeclaration record)
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
                    .Concat(record.RecordProperties
                        .Where(p => p.IsOptional == false)
                        .Select(p => new Statement($"if ({p.Name} is null) errors.Add(\"{p.Name} is null\");")))
                    .Concat(new IWriteableCode[] { new Statement(
                "if (errors.Count > 0) throw new ValidationException(errors);") })
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
                    new Property("Errors", "System.Collections.Generic.IReadOnlyCollection<string>", hasSetter: false, visibility: Visibility.Public, defaultValue: null),
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
                .Select(p => new Property(p.Name, p.Type, true, Visibility.Public, p.DefaultValue))
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