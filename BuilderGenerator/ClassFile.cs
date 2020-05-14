namespace BuilderGenerator
{
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
}