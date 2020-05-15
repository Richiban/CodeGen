using System;
using System.Collections.Generic;

namespace BuilderGenerator
{
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
}