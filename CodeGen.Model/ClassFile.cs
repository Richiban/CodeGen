using System.Diagnostics.CodeAnalysis;

namespace Richiban.CodeGen.Model
{
    public class ClassFile : IWriteableCode
    {
        private readonly string usings;
        private readonly ClassDeclaration classDec;

        public ClassFile(string usings, ClassDeclaration classDec)
        {
            this.usings = usings;
            this.classDec = classDec;
        }

        public string? NamespaceName { get; init; }

        public void WriteTo(CodeBuilder codeBuilder)
        {
            codeBuilder.AppendLine(usings);

            if (NamespaceName is not null)
            {
                codeBuilder.AppendLine($"namespace {NamespaceName}");
                codeBuilder.AppendLine("{");
            }

            using (codeBuilder.Indent())
            {
                classDec.WriteTo(codeBuilder);
            }

            if (NamespaceName is not null)
            {
                codeBuilder.AppendLine("}");
            }
        }
    }
}