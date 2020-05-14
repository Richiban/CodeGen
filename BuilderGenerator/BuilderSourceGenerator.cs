using System;
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

    public class ClassFile : IWriteableCode
    {
        private readonly string usings;
        private readonly string namespaceName;
        private readonly ClassDec classDec;

        public ClassFile(string usings, string namespaceName, ClassDec classDec)
        {
            this.usings = usings;
            this.namespaceName = namespaceName;
            this.classDec = classDec;
        }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            codeBuilder.AppendLine(usings);

            codeBuilder.AppendLine($"namespace {namespaceName}");
            codeBuilder.AppendLine("{");

            using (codeBuilder.Indent())
            {
                classDec.WriteTo(codeBuilder);
            }

            codeBuilder.AppendLine("}");
        }
    }

    public class ConstructorCall : IWriteableCode
    {
        public ConstructorCall(string name, IReadOnlyCollection<string> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }
        public IReadOnlyCollection<string> Arguments { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            codeBuilder.Append($"return new {Name}(");

            using (codeBuilder.Indent())
            {
                var isFirst = true;

                foreach (var prop in Arguments)
                {
                    if (!isFirst)
                        codeBuilder.Append(", ");

                    codeBuilder.Append(prop);
                    isFirst = false;
                }
            }

            codeBuilder.AppendLine($");");
        }
    }

    public class Statement : IWriteableCode
    {
        public Statement(string content)
        {
            Content = content;
        }

        public string Content { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            codeBuilder.AppendLine(Content);
        }
    }

    public class Method : IWriteableCode
    {
        public Method(
            string name, string type, Visibility visibility,
            bool isStatic,
            IReadOnlyCollection<Parameter> parameters,
            IReadOnlyCollection<IWriteableCode> contents)
        {
            Name = name;
            Type = type;
            Visibility = visibility;
            IsStatic = isStatic;
            Contents = contents;
            Parameters = parameters;
        }

        public String Name { get; }
        public String Type { get; }
        public Visibility Visibility { get; }
        public bool IsStatic { get; }
        public IReadOnlyCollection<IWriteableCode> Contents { get; }
        public IReadOnlyCollection<Parameter> Parameters { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            Visibility.WriteTo(codeBuilder);

            if (IsStatic)
            {
                codeBuilder.Append("static ");
            }

            codeBuilder.Append($"{Type} {Name}(");

            codeBuilder.WriteAll(Parameters, ", ");

            codeBuilder.AppendLine($")");
            codeBuilder.AppendLine("{");

            using (codeBuilder.Indent())
            {
                foreach (var item in Contents ?? new IWriteableCode[] { })
                {
                    item.WriteTo(codeBuilder);
                }
            }

            codeBuilder.AppendLine("}");
        }
    }

    public class Property : IWriteableCode
    {
        public Property(string name, string type, bool hasSetter, Visibility visibility)
        {
            Name = name;
            Type = type;
            HasSetter = hasSetter;
            Visibility = visibility;
        }

        public string Name { get; }
        public string Type { get; }
        public bool HasSetter { get; }
        public Visibility Visibility { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            Visibility.WriteTo(codeBuilder);
            codeBuilder.Append($"{Type} {Name}");

            if (HasSetter)
                codeBuilder.AppendLine(" { get; set; }");
            else
                codeBuilder.AppendLine(" { get; }");
        }
    }

    public class RecordProperty
    {
        public RecordProperty(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public string Type { get; }
    }

    public class RecordDeclaration
    {
        public RecordDeclaration(string name, IReadOnlyCollection<RecordProperty> recordProperties)
        {
            Name = name;
            RecordProperties = recordProperties;
        }

        public string Name { get; }
        public IReadOnlyCollection<RecordProperty> RecordProperties { get; }
    }

    public class CodeBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private int IndentationLevel = 0;

        public void AppendLine(string s)
        {
            A();
            _sb.AppendLine(s);
            WriteIndentation = true;
        }

        public void Append(string s)
        {
            A();

            _sb.Append(s);
            WriteIndentation = false;
        }

        public override string ToString()
        {
            return _sb.ToString();
        }

        internal void IncreaseIndentation()
        {
            IndentationLevel++;
        }

        internal void DecreaseIndentation()
        {
            IndentationLevel = Math.Max(0, IndentationLevel - 1);
        }

        public IDisposable Indent()
        {
            return new CodeBuilderIndentor(this);
        }

        private void A()
        {
            if (WriteIndentation)
                foreach (var _ in Enumerable.Range(0, IndentationLevel * 4))
                {
                    _sb.Append(' ');
                }

            WriteIndentation = false;
        }

        private bool WriteIndentation = true;

        private class CodeBuilderIndentor : IDisposable
        {
            private readonly CodeBuilder _cb;

            public CodeBuilderIndentor(CodeBuilder cb)
            {
                _cb = cb;
                _cb.IncreaseIndentation();
            }

            public void Dispose() => _cb.DecreaseIndentation();
        }
    }

    public interface IWriteableCode
    {
        void WriteTo(CodeBuilder codeBuilder);
    }

    public class ClassDec : IWriteableCode
    {
        public ClassDec(
            string name,
            Constructor constructor = null,
            Visibility visibility = null,
            string baseClass = null,
            bool isPartial = false,
            params IWriteableCode[] contents)
        {
            Name = name;
            Constructor = constructor ?? new Constructor.None();
            Visibility = visibility ?? Visibility.None;
            BaseClass = baseClass;
            IsPartial = isPartial;
            Contents = contents;
        }

        public string Name { get; }
        public Constructor Constructor { get; }
        public Visibility Visibility { get; }
        public bool IsPartial { get; }
        public IReadOnlyCollection<IWriteableCode> Contents { get; }
        public string BaseClass { get; }

        public void WriteTo(CodeBuilder cb)
        {
            Visibility.WriteTo(cb);

            if (IsPartial)
                cb.Append("partial ");

            cb.Append("class ");
            cb.Append(Name);

            if (BaseClass != null)
            {
                cb.Append($" : {BaseClass}");
            }

            cb.AppendLine("");

            cb.AppendLine("{");

            using (cb.Indent())
            {
                Constructor?.WriteTo(cb);

                foreach (var item in Contents ?? new IWriteableCode[] { })
                {
                    item.WriteTo(cb);
                }
            }

            cb.AppendLine("}");
        }
    }

    public class AssignmentStatement : IWriteableCode
    {
        public AssignmentStatement(string lhs, string rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
        }

        public string Lhs { get; }
        public string Rhs { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            codeBuilder.AppendLine($"{Lhs} = {Rhs};");
        }
    }

    public class Parameter : IWriteableCode
    {
        public Parameter(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public string Type { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            codeBuilder.Append($"{Type} {Name}");
        }
    }

    public abstract class Constructor : IWriteableCode
    {
        private Constructor() { }

        public abstract void WriteTo(CodeBuilder codeBuilder);

        public class BlockConstructor : Constructor
        {
            public BlockConstructor(
                string name,
                Visibility visibility,
                IReadOnlyCollection<Parameter> parameters,
                IReadOnlyCollection<AssignmentStatement> constructorAssignments,
                IReadOnlyCollection<string> baseCall = null)
            {
                Name = name;
                Visibility = visibility;
                Parameters = parameters;
                Statements = constructorAssignments;
                BaseCall = baseCall;
            }

            public string Name { get; }
            public IReadOnlyCollection<Parameter> Parameters { get; }
            public IReadOnlyCollection<AssignmentStatement> Statements;
            public Visibility Visibility { get; }
            public IReadOnlyCollection<string> BaseCall { get; }

            public override void WriteTo(CodeBuilder codeBuilder)
            {
                Visibility.WriteTo(codeBuilder);
                codeBuilder.Append(Name);
                codeBuilder.Append("(");

                var isFirst = true;
                foreach (var p in Parameters)
                {
                    if (isFirst == false)
                    {
                        codeBuilder.Append(", ");
                    }

                    p.WriteTo(codeBuilder);
                    isFirst = false;
                }

                codeBuilder.Append(")");

                if (BaseCall?.Any() == true)
                {
                    codeBuilder.Append(" : base(");

                    codeBuilder.Append(String.Join(", ", BaseCall));

                    codeBuilder.Append(")");
                }

                codeBuilder.AppendLine("");

                codeBuilder.AppendLine("{");

                using (codeBuilder.Indent())
                {
                    foreach (var statement in Statements)
                    {
                        statement.WriteTo(codeBuilder);
                    }
                }

                codeBuilder.AppendLine("}");
            }
        }

        public class None : Constructor { public override void WriteTo(CodeBuilder codeBuilder) { } }
    }

    public class Visibility : IWriteableCode
    {
        private string _value;
        private Visibility(string value) { _value = value; }

        public void WriteTo(CodeBuilder sb) => sb.Append(_value);

        public static Visibility Public { get; } = new Visibility("public ");
        public static Visibility Private { get; } = new Visibility("private ");
        public static Visibility Internal { get; } = new Visibility("internal ");
        public static Visibility None { get; } = new Visibility("");
    }

    public static class Extensions
    {
        public static void WriteAll(this CodeBuilder codeBuilder, IReadOnlyCollection<IWriteableCode> items, string joiner = "")
        {
            var isFirst = true;

            foreach (var p in items)
            {
                if (isFirst == false)
                {
                    codeBuilder.Append(joiner);
                }

                p.WriteTo(codeBuilder);
                isFirst = false;
            }
        }

        public static bool HasAttribute(this SyntaxList<AttributeListSyntax> attributes, string name)
        {
            string fullname, shortname;
            var attrLen = "Attribute".Length;
            if (name.EndsWith("Attribute"))
            {
                fullname = name;
                shortname = name.Remove(name.Length - attrLen, attrLen);
            }
            else
            {
                fullname = name + "Attribute";
                shortname = name;
            }

            return attributes.Any(al => al.Attributes.Any(a => a.Name.ToString() == shortname || a.Name.ToString() == fullname));
        }

        public static T FindParent<T>(this SyntaxNode node) where T : class
        {
            var current = node;
            while (true)
            {
                current = current.Parent;
                if (current == null || current is T)
                    return current as T;
            }
        }
    }
}