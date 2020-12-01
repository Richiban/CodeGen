namespace Richiban.CodeGen.Model
{
    public interface IWriteableCode
    {
        void WriteTo(CodeBuilder codeBuilder);
    }
}