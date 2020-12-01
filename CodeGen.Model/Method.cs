using System;
using System.Collections.Generic;

namespace Richiban.CodeGen.Model
{
    public class MethodDeclaration : IWriteableCode
    {
        public MethodDeclaration(
            string name,
            string type,
            Visibility visibility,
            bool isStatic,
            bool isOverride,
            IReadOnlyCollection<Parameter> parameters,
            IReadOnlyCollection<IWriteableCode> contents)
        {
            Name = name;
            Type = type;
            Visibility = visibility;
            IsStatic = isStatic;
            IsOverride = isOverride;
            Contents = contents;
            Parameters = parameters;
        }

        public string Name { get; }
        public string Type { get; }
        public Visibility Visibility { get; }
        public bool IsStatic { get; }
        public bool IsOverride { get; }
        public IReadOnlyCollection<IWriteableCode> Contents { get; }
        public IReadOnlyCollection<Parameter> Parameters { get; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            Visibility.WriteTo(codeBuilder);

            if (IsStatic)
            {
                codeBuilder.Append("static ");
            }

            if (IsOverride)
            {
                codeBuilder.Append("override ");
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