using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace BuilderGenerator
{
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
                IReadOnlyCollection<string>? baseCall = null)
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
            public IReadOnlyCollection<string>? BaseCall { get; }

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
}