namespace Richiban.AutoStar.Model
{
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
}